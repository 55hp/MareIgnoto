using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Decisions;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Engine
{
    /// <summary>
    /// Gli Abbordaggi fortuiti (R-070–R-073d), risolti dopo il movimento per ogni cella di mare con 2+ navi, nell'ordine
    /// di turno del primo giocatore coinvolto. Le catene di R-073a si accodano e si risolvono subito dopo.
    /// </summary>
    internal sealed class BoardingFlow
    {
        private readonly GameContext ctx;
        private readonly GameState state;
        private readonly Queue<KeyValuePair<Coord, int>> queue = new Queue<KeyValuePair<Coord, int>>();
        /// <summary>Navi riposizionate da un Abbordaggio a 3+ (R-071): niente nuovo Abbordaggio in questo round.</summary>
        private readonly HashSet<int> settled = new HashSet<int>();
        /// <summary>Cicli di Abbordaggio risolti in questo round (catene e ripetizioni comprese), per il limite di R-073a.</summary>
        private int cycles;

        private BoardingFlow(GameContext ctx)
        {
            this.ctx = ctx;
            state = ctx.State;
        }

        public static IEnumerable<FlowStep> Run(GameContext ctx) => new BoardingFlow(ctx).Resolve();

        private IEnumerable<FlowStep> Resolve()
        {
            foreach (int id in state.TurnOrderList)
            {
                Coord cell = state.PlayerById(id).Position;
                if (state.Map.KindAt(cell) == CellKind.Sea && ShipsAt(cell).Count >= 2 && !queue.Any(q => q.Key == cell))
                    queue.Enqueue(new KeyValuePair<Coord, int>(cell, 0));
            }

            while (queue.Count > 0)
            {
                KeyValuePair<Coord, int> next = queue.Dequeue();
                Coord cell = next.Key;
                List<PlayerState> involved = ShipsAt(cell).Where(p => !settled.Contains(p.Id)).ToList();
                if (involved.Count < 2)
                {
                    // Restano solo navi già sistemate da R-071: condividono la cella senza nuovo Abbordaggio.
                    if (ShipsAt(cell).Count >= 2) state.SharedSeaCellsAllowed.Add(cell);
                    continue;
                }

                if (involved.Count == 2) yield return Flow.Call(TwoShips(cell, involved, next.Value));
                else yield return Flow.Call(ManyShips(cell, involved, next.Value));
            }
        }

        /// <summary>Le navi sulla cella, nell'ordine di turno del round.</summary>
        private List<PlayerState> ShipsAt(Coord cell) =>
            state.TurnOrderList.Select(state.PlayerById).Where(p => p.Position == cell).ToList();

        /// <summary>R-073a: oltre il limite le navi restano dove sono e il motore lo segnala.</summary>
        private bool LimitReached(Coord cell, List<PlayerState> involved)
        {
            if (cycles < ctx.Config.maxAbbordaggioChain) return false;
            ctx.Emit(new BoardingChainLimitEvent(cell, involved.Select(p => p.Id).ToArray(), ctx.Config.maxAbbordaggioChain));
            state.SharedSeaCellsAllowed.Add(cell);
            return true;
        }

        /// <summary>R-070: perdita a scelta e tiro; con tiri uguali si ripete il ciclo. Poi R-073a sulle destinazioni.</summary>
        private IEnumerable<FlowStep> TwoShips(Coord cell, List<PlayerState> involved, int depth)
        {
            for (int repeat = 0; ; repeat++)
            {
                if (LimitReached(cell, involved)) yield break;
                cycles++;
                ctx.Emit(new BoardingStartedEvent(cell, involved.Select(p => p.Id).ToArray(), depth, repeat));

                foreach (PlayerState player in involved)
                    yield return Flow.Call(LoseTwoShips(player));

                var values = new Dictionary<int, int>();
                yield return Flow.Call(Reposition(involved, values));
                if (values[involved[0].Id] == values[involved[1].Id]) continue;

                foreach (PlayerState player in involved)
                {
                    Coord destination = Move(player, values[player.Id]);
                    if (state.Map.KindAt(destination) != CellKind.Sea) continue;

                    // R-073a: cella di mare occupata da una nave non coinvolta → nuovo Abbordaggio subito, a catena.
                    List<PlayerState> others = ShipsAt(destination).Where(p => p != player).ToList();
                    if (others.Count == 0) continue;
                    if (others.Any(p => !settled.Contains(p.Id)))
                        queue.Enqueue(new KeyValuePair<Coord, int>(destination, depth + 1));
                    else
                        state.SharedSeaCellsAllowed.Add(destination);
                }

                yield break;
            }
        }

        /// <summary>R-071: ognuno perde 1 crew e 1 Pirateria, tira una volta; chi finisce su un'altra nave resta lì.</summary>
        private IEnumerable<FlowStep> ManyShips(Coord cell, List<PlayerState> involved, int depth)
        {
            if (LimitReached(cell, involved)) yield break;
            cycles++;
            ctx.Emit(new BoardingStartedEvent(cell, involved.Select(p => p.Id).ToArray(), depth, 0));

            foreach (PlayerState player in involved)
                yield return Flow.Call(LoseManyShips(player));

            var values = new Dictionary<int, int>();
            yield return Flow.Call(Reposition(involved, values));

            foreach (PlayerState player in involved)
            {
                Move(player, values[player.Id]);
                settled.Add(player.Id);
            }

            foreach (PlayerState player in involved)
                if (state.Map.KindAt(player.Position) == CellKind.Sea && ShipsAt(player.Position).Count >= 2)
                    state.SharedSeaCellsAllowed.Add(player.Position);
        }

        /// <summary>R-072, R-073b: prima tirano tutti quelli senza Timoniere, poi chi ce l'ha sceglie, nell'ordine di turno.</summary>
        private IEnumerable<FlowStep> Reposition(List<PlayerState> involved, Dictionary<int, int> values)
        {
            foreach (PlayerState player in involved.Where(p => !WeatherFlow.HasHelmsman(p)))
                values[player.Id] = ctx.RollD8(player.Id, DiceReason.BoardingReposition);

            foreach (PlayerState player in involved.Where(WeatherFlow.HasHelmsman))
            {
                var result = new int[1];
                yield return Flow.Call(WeatherFlow.RollOrChoose(ctx, player, DiceReason.BoardingReposition,
                    DecisionKind.BoardingReposition, result));
                values[player.Id] = result[0];
            }
        }

        /// <summary>Sposta la nave nella cella adiacente del risultato (R-072). Sulla cornice è arenata (R-073a, R-069).</summary>
        private Coord Move(PlayerState player, int value)
        {
            Coord from = player.Position;
            Coord to = from.Step(HeadingExtensions.FromD8(value));
            player.Position = to;
            ctx.Emit(new ShipRepositionedEvent(player.Id, value, from, to));
            if (state.Map.KindAt(to) == CellKind.Border) ctx.Emit(new ShipStrandedEvent(player.Id, to));
            if (state.Map.KindAt(to) == CellKind.SacredIsland && !state.SacredIslandArrivals.ContainsKey(player.Id))
            {
                // R-073a, R-140: arriva sull'Isola Sacra come in movimento, ma dopo tutti i passi (R-141).
                state.SacredIslandArrivals[player.Id] = GameState.BoardingArrivalStep;
                ctx.Emit(new SacredIslandEnteredEvent(player.Id, 0, to, true));
            }
            return to;
        }

        /// <summary>R-070, R-073c: 1 crew (qualsiasi slot) oppure 2 Pirateria, tra i tipi di carta che il giocatore ha.</summary>
        private IEnumerable<FlowStep> LoseTwoShips(PlayerState player)
        {
            var options = new List<DecisionOption>();
            List<int> occupied = Losses.OccupiedSlots(player).ToList();
            if (occupied.Count > 0)
                foreach (IReadOnlyList<int> slots in Combinations(occupied, ctx.Config.boardingTwoShipsCrewLoss))
                    options.Add(new BoardingLossOption(slots, new PirateCard[0]));
            if (player.Hand.Count > 0)
                foreach (IReadOnlyList<PirateCard> cards in Losses.DistinctHandChoices(player.Hand, System.Math.Min(ctx.Config.boardingTwoShipsPirateLoss, player.Hand.Count)))
                    options.Add(new BoardingLossOption(new int[0], cards));

            return Apply(player, options);
        }

        /// <summary>R-071, R-073c: 1 crew e 1 Pirateria insieme (meno, se non ne ha).</summary>
        private IEnumerable<FlowStep> LoseManyShips(PlayerState player)
        {
            List<int> occupied = Losses.OccupiedSlots(player).ToList();
            List<IReadOnlyList<int>> crewChoices = Combinations(occupied, ctx.Config.boardingManyShipsCrewLoss);
            List<IReadOnlyList<PirateCard>> pirateChoices = Losses.DistinctHandChoices(player.Hand,
                System.Math.Min(ctx.Config.boardingManyShipsPirateLoss, player.Hand.Count));

            var options = new List<DecisionOption>();
            foreach (IReadOnlyList<int> slots in crewChoices)
                foreach (IReadOnlyList<PirateCard> cards in pirateChoices)
                    if (slots.Count > 0 || cards.Count > 0) options.Add(new BoardingLossOption(slots, cards));

            return Apply(player, options);
        }

        private IEnumerable<FlowStep> Apply(PlayerState player, List<DecisionOption> options)
        {
            if (options.Count == 0) yield break; // R-073c: nessuna carta, nessuna perdita.

            AskStep ask = ctx.Ask(DecisionKind.BoardingLoss, player.Id, true, options);
            yield return ask;
            BoardingLossOption choice = ask.Choice<BoardingLossOption>();

            // Per carta, non per slot: se il Medico sposta una carta minacciata, le altre scelte restano valide.
            List<CrewCard> cards = choice.CrewSlots.Select(slot => player.Crew[slot]).ToList();
            foreach (CrewCard card in cards)
            {
                int slot = Losses.OccupiedSlots(player).FirstOrDefault(s => player.Crew[s] == card);
                if (player.Crew[slot] == card)
                    yield return Flow.Call(Losses.LoseCrew(ctx, player, slot, CrewLossCause.Boarding));
            }

            Losses.LosePirateCards(ctx, player, choice.PirateCards);
        }

        /// <summary>Tutti i sottoinsiemi di <paramref name="count"/> elementi (meno se non bastano), in ordine.</summary>
        private static List<IReadOnlyList<int>> Combinations(List<int> items, int count)
        {
            int k = System.Math.Min(count, items.Count);
            var result = new List<IReadOnlyList<int>>();
            Collect(items, 0, k, new List<int>(), result);
            return result;
        }

        private static void Collect(List<int> items, int start, int left, List<int> current, List<IReadOnlyList<int>> result)
        {
            if (left == 0)
            {
                result.Add(current.ToArray());
                return;
            }

            for (int i = start; i <= items.Count - left; i++)
            {
                current.Add(items[i]);
                Collect(items, i + 1, left - 1, current, result);
                current.RemoveAt(current.Count - 1);
            }
        }
    }
}
