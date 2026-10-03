using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Decisions;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Engine
{
    /// <summary>Il movimento simultaneo cella per cella (R-060–R-069).</summary>
    internal static class MovementFlow
    {
        public static IEnumerable<FlowStep> Run(GameContext ctx)
        {
            GameState state = ctx.State;
            GameMap map = state.Map;
            int count = state.PlayerCount;

            var speeds = new int[count];
            foreach (PlayerState player in state.Players)
                speeds[player.Id] = player.LeisureRound == state.Round ? 0 : Speed(player, state.Wind, map, ctx.Config); // R-045
            ctx.Emit(new MovementStartedEvent(speeds));

            var remaining = (int[])speeds.Clone();
            var moved = new bool[count];

            for (int step = 1; remaining.Any(r => r > 0); step++)
            {
                // R-065: ogni nave con passi residui avanza di una cella, tutte insieme.
                var from = new Dictionary<int, Coord>();
                foreach (PlayerState player in state.Players)
                {
                    if (remaining[player.Id] == 0) continue;
                    Coord target = player.Position.Step(player.ChosenHeading.Value);
                    if (!map.IsInBounds(target))
                    {
                        // La rotta esce dalla mappa (nave sulla cornice che punta fuori): la nave resta dov'è.
                        // TODO R-066: 02 non copre il caso; vedi le domande della spec 0002.
                        remaining[player.Id] = 0;
                        ctx.Emit(new ShipStoppedEvent(player.Id, step, player.Position, StopReason.MapEdge));
                        continue;
                    }

                    from[player.Id] = player.Position;
                    player.Position = target;
                    remaining[player.Id]--;
                    moved[player.Id] = true;
                    ctx.Emit(new ShipMovedEvent(player.Id, step, from[player.Id], target));
                }

                // R-067: attraversamenti tra celle di mare (sulla terra più navi convivono, R-068).
                var crossed = new HashSet<int>();
                foreach (int a in from.Keys.OrderBy(id => id).ToList())
                {
                    foreach (int b in from.Keys.Where(id => id > a).OrderBy(id => id))
                    {
                        if (crossed.Contains(a) || crossed.Contains(b)) continue;
                        PlayerState pa = state.PlayerById(a);
                        PlayerState pb = state.PlayerById(b);
                        if (pa.Position != from[b] || pb.Position != from[a]) continue;
                        if (map.KindAt(from[a]) != CellKind.Sea || map.KindAt(from[b]) != CellKind.Sea) continue;

                        crossed.Add(a);
                        crossed.Add(b);
                        yield return Flow.Call(ResolveCrossing(ctx, step, pa, pb));
                        remaining[a] = 0;
                        remaining[b] = 0;
                    }
                }

                // R-066: terra o nave sulla cella appena raggiunta.
                foreach (int id in from.Keys.OrderBy(id => id))
                {
                    if (crossed.Contains(id)) continue;
                    PlayerState player = state.PlayerById(id);
                    CellKind kind = map.KindAt(player.Position);
                    if (kind != CellKind.Sea)
                    {
                        if (kind == CellKind.SacredIsland)
                        {
                            // R-140, R-141: la partita finirà a fine round; conta il passo d'ingresso.
                            if (!state.SacredIslandArrivals.ContainsKey(id)) state.SacredIslandArrivals[id] = step;
                            ctx.Emit(new SacredIslandEnteredEvent(id, step, player.Position));
                        }
                        if (remaining[id] > 0) ctx.Emit(new ShipStoppedEvent(id, step, player.Position, StopReason.Land));
                        remaining[id] = 0;
                    }
                    else if (state.Players.Any(other => other.Id != id && other.Position == player.Position))
                    {
                        ctx.Emit(new ShipStoppedEvent(id, step, player.Position, StopReason.Collision));
                        remaining[id] = 0;
                    }
                }
            }

            // R-069, R-097: arenata sulla cornice o arrivata sull'isola col proprio segnalino.
            foreach (PlayerState player in state.Players)
                if (moved[player.Id]) CheckStranded(ctx, player);

            ctx.Emit(new MovementEndedEvent(state.Players.Select(p => p.Position).ToArray()));
        }

        /// <summary>
        /// Dopo un arrivo (movimento o riposizionamento da Abbordaggio): la cornice arena (R-069); l'isola con il proprio
        /// segnalino vale per la nave come la cornice: arenata, e in Fase 2 il turno salta (R-097, R-053).
        /// </summary>
        public static void CheckStranded(GameContext ctx, PlayerState player)
        {
            GameMap map = ctx.State.Map;
            CellKind kind = map.KindAt(player.Position);
            bool ownIsland = kind == CellKind.Island && player.IslandMarker >= 0 && map.IslandIdAt(player.Position) == player.IslandMarker;
            if (ownIsland) player.ArrivedOnOwnIsland = true;
            if (kind == CellKind.Border || ownIsland) ctx.Emit(new ShipStrandedEvent(player.Id, player.Position));
        }

        /// <summary>
        /// R-060–R-064: base, vento (non se si parte da terra, R-062), Timoniere (Jolly compreso), mai sotto 0.
        /// </summary>
        public static int Speed(PlayerState player, Heading wind, GameMap map, RulesConfig config)
        {
            Heading heading = player.ChosenHeading.Value;
            int speed = config.baseSpeed;
            if (!map.IsLand(player.Position))
            {
                if (heading == wind) speed += config.windSpeedBonus;
                else if (heading == wind.Opposite()) speed -= config.windSpeedBonus;
            }

            speed += CrewEffects.EffectiveCount(player.CrewAbove, CrewCardId.Timoniere) * config.helmsmanSpeedBonus;
            return speed < 0 ? 0 : speed;
        }

        /// <summary>
        /// R-067: le due navi si fermano sulla stessa cella, una delle due coinvolte, scelta da chi viene prima
        /// nell'ordine di turno del round.
        /// </summary>
        private static IEnumerable<FlowStep> ResolveCrossing(GameContext ctx, int step, PlayerState a, PlayerState b)
        {
            List<int> order = ctx.State.TurnOrderList;
            PlayerState chooser = order.IndexOf(a.Id) <= order.IndexOf(b.Id) ? a : b;

            // Ora a e b si sono scambiate: le celle coinvolte sono le loro posizioni attuali.
            var cells = new[] { a.Position, b.Position }.OrderBy(c => c.Y).ThenBy(c => c.X).ToList();
            var options = cells.Select(c => (DecisionOption)new CellOption(c)).ToList();
            AskStep ask = ctx.Ask(DecisionKind.CrossingCell, chooser.Id, false, options);
            yield return ask;

            Coord cell = ask.Choice<CellOption>().Cell;
            a.Position = cell;
            b.Position = cell;
            ctx.Emit(new CrossingResolvedEvent(step, a.Id, b.Id, chooser.Id, cell));
            ctx.Emit(new ShipStoppedEvent(a.Id, step, cell, StopReason.Crossing));
            ctx.Emit(new ShipStoppedEvent(b.Id, step, cell, StopReason.Crossing));
        }
    }
}
