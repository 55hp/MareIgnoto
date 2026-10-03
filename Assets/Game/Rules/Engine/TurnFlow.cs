using System;
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
    /// <summary>
    /// Il turno di un giocatore in Fase 2 dopo cornice (R-053) e Mozzo (R-052): Isola Sacra (R-055), porto (R-054,
    /// R-090–R-096, Svago R-091), Vedetta (R-058) o mare (R-056, <see cref="SeaTurnFlow"/>).
    /// </summary>
    internal static class TurnFlow
    {
        public static IEnumerable<FlowStep> Play(GameContext ctx, PlayerState player)
        {
            GameState state = ctx.State;
            CellKind kind = state.Map.KindAt(player.Position);

            if (kind == CellKind.SacredIsland)
            {
                ctx.Emit(new TurnSkippedEvent(player.Id, TurnSkipReason.SacredIsland)); // R-055
                yield break;
            }

            // R-091: in Svago la nave resta in porto e nella Fase 2 di quel round gioca di nuovo un turno di porto.
            bool port = kind == CellKind.Island || player.LeisureRound == state.Round;
            bool byLookout = false;
            if (!port && kind == CellKind.Sea && LookoutReachesIsland(state.Map, player, ctx.Config))
            {
                var options = new List<DecisionOption> { new TurnKindOption(TurnKind.Port), new TurnKindOption(TurnKind.Sea) };
                AskStep ask = ctx.Ask(DecisionKind.LookoutChoice, player.Id, true, options);
                yield return ask;
                port = byLookout = ask.Choice<TurnKindOption>().Kind == TurnKind.Port;
            }

            ctx.Emit(new TurnKindEvent(player.Id, port ? TurnKind.Port : TurnKind.Sea, byLookout));
            if (port) yield return Flow.Call(Port(ctx, player));
            else yield return Flow.Call(SeaTurnFlow.Run(ctx, player));
        }

        /// <summary>
        /// R-058: la Vedetta sopra coperta fa attraccare a un'isola entro <c>lookoutRange</c> celle; ogni copia (Jolly
        /// compreso, R-013/R-015) allunga la distanza di <c>lookoutRange</c>. L'Isola Sacra non è un porto (R-055).
        /// </summary>
        public static bool LookoutReachesIsland(GameMap map, PlayerState player, RulesConfig config)
        {
            int range = CrewEffects.EffectiveCount(player.CrewAbove, CrewCardId.Vedetta) * config.lookoutRange;
            for (int dx = -range; dx <= range; dx++)
                for (int dy = -range; dy <= range; dy++)
                {
                    var cell = new Coord(player.Position.X + dx, player.Position.Y + dy);
                    if (map.IsInBounds(cell) && map.KindAt(cell) == CellKind.Island) return true;
                }

            return false;
        }

        /// <summary>R-095: il Cuoco abbassa ogni costo di porto di <c>cookCostDiscount</c> per copia, fino a 0.</summary>
        public static int PortCost(PlayerState player, int baseCost, RulesConfig config)
        {
            int discount = CrewEffects.EffectiveCount(player.CrewAbove, CrewCardId.Cuoco) * config.cookCostDiscount;
            return Math.Max(0, baseCost - discount);
        }

        /// <summary>Turno in porto (R-054): una sola azione, poi il turno termina. In porto niente swap, attacchi né carte.</summary>
        public static IEnumerable<FlowStep> Port(GameContext ctx, PlayerState player)
        {
            RulesConfig cfg = ctx.Config;
            int leisure = PortCost(player, cfg.leisureCost, cfg);
            int recruitOne = PortCost(player, cfg.recruitCostKeepOne, cfg);
            int recruitTwo = PortCost(player, cfg.recruitCostKeepTwo, cfg);

            // R-096: le azioni a pagamento solo con le monete per il costo effettivo; R-009: Missione solo col Corsaro non vuoto.
            var options = new List<DecisionOption> { new PortActionOption(PortAction.Plunder, 0) };
            if (player.Coins >= leisure) options.Add(new PortActionOption(PortAction.Leisure, leisure));
            if (ctx.CanDraw(DeckKind.Crew))
            {
                if (player.Coins >= recruitOne) options.Add(new PortActionOption(PortAction.RecruitOne, recruitOne));
                if (player.Coins >= recruitTwo) options.Add(new PortActionOption(PortAction.RecruitTwo, recruitTwo));
            }

            if (ctx.CanDraw(DeckKind.Corsair)) options.Add(new PortActionOption(PortAction.Mission, 0));
            foreach (int slot in Losses.OccupiedSlots(player))
                options.Add(new PortActionOption(PortAction.Commerce, 0, slot, CrewEffects.CommerceValue(player.Crew[slot], cfg)));

            AskStep ask = ctx.Ask(DecisionKind.PortAction, player.Id, true, options);
            yield return ask;
            PortActionOption choice = ask.Choice<PortActionOption>();

            ctx.Emit(new PortActionEvent(player.Id, choice.Action, choice.Cost));
            if (choice.Cost > 0) ctx.ChangeCoins(player, -choice.Cost, CoinReason.PortCost); // escono dal gioco

            switch (choice.Action)
            {
                case PortAction.Plunder: // R-090
                    ctx.ChangeCoins(player, ctx.RollD8(player.Id, DiceReason.Plunder), CoinReason.Plunder);
                    break;
                case PortAction.Leisure: // R-091, R-045
                    player.LeisureRound = ctx.State.Round + 1;
                    break;
                case PortAction.RecruitOne:
                    yield return Flow.Call(Recruit(ctx, player, cfg.recruitKeepOne));
                    break;
                case PortAction.RecruitTwo:
                    yield return Flow.Call(Recruit(ctx, player, cfg.recruitKeepTwo));
                    break;
                case PortAction.Mission: // R-093
                    ctx.DrawToTransit(player, DeckKind.Corsair, cfg.portMissionDrawCount);
                    yield return Flow.Call(CrewFlow.KeepMissions(ctx, player, cfg.portMissionMinKept));
                    break;
                case PortAction.Commerce: // R-094: non è una perdita (R-021)
                    bool above = player.Crew.IsAbove(choice.Slot);
                    CrewCard sold = ctx.TakeCrew(player, choice.Slot);
                    ctx.State.Crew.Discard(sold);
                    ctx.Emit(new CrewSoldEvent(player.Id, choice.Slot, above, choice.Value, sold));
                    ctx.Emit(new CrewSlotChangedEvent(player.Id, choice.Slot, above, null));
                    if (choice.Value > 0) ctx.ChangeCoins(player, choice.Value, CoinReason.Commerce);
                    break;
            }
        }

        /// <summary>R-092: pesca le crew, ne tiene <paramref name="keep"/> (ricevute secondo R-020), le altre agli scarti.</summary>
        private static IEnumerable<FlowStep> Recruit(GameContext ctx, PlayerState player, int keep)
        {
            List<CrewCard> drawn = ctx.DrawToTransit(player, DeckKind.Crew, ctx.Config.recruitDrawCount).Cast<CrewCard>().ToList();
            if (drawn.Count == 0) yield break;

            var options = Losses.Combinations(drawn, Math.Min(keep, drawn.Count))
                .Select(kept => (DecisionOption)new KeepCrewOption(kept)).ToList();
            AskStep ask = ctx.Ask(DecisionKind.RecruitKeep, player.Id, true, options, drawn.Cast<Card>().ToList());
            yield return ask;
            IReadOnlyList<CrewCard> chosen = ask.Choice<KeepCrewOption>().Kept;

            var discarded = drawn.Where(card => !chosen.Contains(card)).ToList();
            foreach (CrewCard card in discarded)
            {
                player.InTransit.Remove(card);
                ctx.State.Crew.Discard(card);
            }

            if (discarded.Count > 0) ctx.Emit(new CardsDiscardedEvent(player.Id, DeckKind.Crew, discarded.Cast<Card>().ToList()));
            foreach (CrewCard card in chosen) yield return Flow.Call(CrewFlow.Receive(ctx, player, card));
        }
    }
}
