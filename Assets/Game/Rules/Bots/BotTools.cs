using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Decisions;
using hp55games.MareIgnoto.Rules.Engine;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.Random;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Bots
{
    /// <summary>
    /// Gli strumenti comuni dei bot (09_bot.md §3): funzioni pure che leggono solo ciò che un giocatore vede
    /// (<see cref="PlayerView"/> e lo stato pubblico) e, dove c'è una parità, la casualità del bot.
    /// </summary>
    public static class BotTools
    {
        /// <summary>Distanza "irraggiungibile" nei campi di distanza.</summary>
        public const int Unreachable = int.MaxValue / 4;

        // ---- Velocità e movimento ----

        /// <summary>Velocità stimata con la rotta data (R-060–R-064): la stessa formula del motore, con dati pubblici.</summary>
        public static int EstimatedSpeed(PlayerView view, Heading heading)
        {
            IReadOnlyGameState state = view.State;
            return MovementFlow.Speed(view.Self.Position, heading, view.Self.CrewAbove, state.Wind, state.Map, state.Config);
        }

        /// <summary>La cella d'arrivo muovendo di <paramref name="speed"/> celle: ci si ferma su terra (isola, Isola Sacra, cornice) o al bordo.</summary>
        public static Coord SimulateMove(GameMap map, Coord from, Heading heading, int speed)
        {
            Coord position = from;
            for (int step = 0; step < speed; step++)
            {
                Coord next = position.Step(heading);
                if (!map.IsInBounds(next)) break;
                position = next;
                if (map.IsLand(position)) break;
            }

            return position;
        }

        /// <summary>
        /// Cammino minimo (8 vicini) dalle celle <paramref name="goals"/> verso ogni cella, passando solo per celle di mare
        /// (isole e cornice si evitano). Le mete valgono 0; <paramref name="blocked"/> sono celle di mare da non attraversare.
        /// </summary>
        public static int[,] DistanceField(GameMap map, IEnumerable<Coord> goals, ICollection<Coord> blocked = null)
        {
            var distance = new int[map.Width, map.Height];
            for (int x = 0; x < map.Width; x++)
                for (int y = 0; y < map.Height; y++)
                    distance[x, y] = Unreachable;

            var queue = new Queue<Coord>();
            foreach (Coord goal in goals)
            {
                if (distance[goal.X, goal.Y] == 0) continue;
                distance[goal.X, goal.Y] = 0;
                queue.Enqueue(goal);
            }

            while (queue.Count > 0)
            {
                Coord cell = queue.Dequeue();
                for (int h = 0; h < HeadingExtensions.Count; h++)
                {
                    Coord next = cell.Step((Heading)h);
                    if (!map.IsInBounds(next) || map.KindAt(next) != CellKind.Sea) continue;
                    if (blocked != null && blocked.Contains(next)) continue;
                    if (distance[next.X, next.Y] != Unreachable) continue;
                    distance[next.X, next.Y] = distance[cell.X, cell.Y] + 1;
                    queue.Enqueue(next);
                }
            }

            return distance;
        }

        /// <summary>La distanza dalla meta di una cella qualsiasi: per una terra che non è meta, uno più il vicino di mare migliore.</summary>
        public static int DistanceAt(int[,] field, GameMap map, Coord cell)
        {
            int own = field[cell.X, cell.Y];
            if (own != Unreachable || map.KindAt(cell) == CellKind.Sea) return own;
            int best = Unreachable;
            for (int h = 0; h < HeadingExtensions.Count; h++)
            {
                Coord next = cell.Step((Heading)h);
                if (map.IsInBounds(next) && field[next.X, next.Y] < best) best = field[next.X, next.Y];
            }

            return best == Unreachable ? Unreachable : best + 1;
        }

        /// <summary>
        /// Penalità meteo della cella d'arrivo (09 §3): Tempesta <see cref="BotProfile.StormPenalty"/>, Mare Mosso
        /// <see cref="BotProfile.RoughSeaPenalty"/>, altrimenti 0. Mare libero, isole e cornice non hanno zona.
        /// </summary>
        public static int WeatherPenalty(IReadOnlyGameState state, Coord cell, BotProfile profile)
        {
            int zone = state.Map.ZoneOf(cell);
            if (zone < 0) return 0;
            switch (state.Config.WeatherAt(state.ZoneLevels[zone]))
            {
                case WeatherState.Storm: return profile.StormPenalty;
                case WeatherState.RoughSea: return profile.RoughSeaPenalty;
                default: return 0;
            }
        }

        /// <summary>
        /// Costo di ogni rotta (09 §3): distanza residua della cella d'arrivo dalla meta più la penalità meteo, più
        /// <paramref name="forbiddenCost"/> se la cella d'arrivo è vietata (per esempio l'Isola Sacra in modalità cauta).
        /// </summary>
        public static int[] HeadingCosts(PlayerView view, IReadOnlyCollection<Coord> goals, BotProfile profile,
            ICollection<Coord> forbidden = null, int forbiddenCost = 1000)
        {
            IReadOnlyGameState state = view.State;
            GameMap map = state.Map;
            int[,] field = DistanceField(map, goals, forbidden);
            var costs = new int[HeadingExtensions.Count];
            for (int h = 0; h < costs.Length; h++)
            {
                var heading = (Heading)h;
                Coord arrival = SimulateMove(map, view.Self.Position, heading, EstimatedSpeed(view, heading));
                int cost = DistanceAt(field, map, arrival);
                if (cost >= Unreachable) cost = Unreachable / 2;
                cost += WeatherPenalty(state, arrival, profile);
                if (forbidden != null && forbidden.Contains(arrival)) cost += forbiddenCost;
                costs[h] = cost;
            }

            return costs;
        }

        /// <summary>La rotta di costo minimo (09 §3), a caso tra le pari.</summary>
        public static Heading ChooseHeading(PlayerView view, IReadOnlyCollection<Coord> goals, BotProfile profile, IRandomSource random,
            ICollection<Coord> forbidden = null)
        {
            int[] costs = HeadingCosts(view, goals, profile, forbidden);
            int best = costs.Min();
            var ties = Enumerable.Range(0, costs.Length).Where(h => costs[h] == best).Select(h => (Heading)h).ToList();
            return Pick(ties, random);
        }

        // ---- Celle notevoli ----

        public static List<Coord> SacredIslandCells(GameMap map) => CellsOf(map, CellKind.SacredIsland);

        /// <summary>Le celle porto, escluse quelle dell'isola con il segnalino <paramref name="excludedIsland"/> (R-097); tutte se così non ne resta nessuna.</summary>
        public static List<Coord> PortCells(GameMap map, int excludedIsland)
        {
            List<Coord> ports = CellsOf(map, CellKind.Island);
            List<Coord> allowed = ports.Where(c => map.IslandIdAt(c) != excludedIsland).ToList();
            return allowed.Count > 0 ? allowed : ports;
        }

        private static List<Coord> CellsOf(GameMap map, CellKind kind)
        {
            var cells = new List<Coord>();
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                    if (map.KindAt(new Coord(x, y)) == kind) cells.Add(new Coord(x, y));
            return cells;
        }

        // ---- Stima di punteggio (R-143) ----

        /// <summary>
        /// "Proprio, se prendo il Tesoro ora" (09 §3): segnalini + quelli del Tesoro + (monete + Tesoro) per difetto
        /// − missioni in mano + poker della propria ciurma, tutta nota.
        /// </summary>
        public static int OwnScoreIfTakingTreasure(PlayerView view)
        {
            RulesConfig cfg = view.State.Config;
            IReadOnlyPlayerState self = view.Self;
            var crew = self.CrewAbove.Concat(view.CrewBelow).Where(c => c != null).ToList();
            return self.BountyTokens + cfg.treasureTokens + (self.Coins + view.State.Treasure) / cfg.coinsPerToken
                   - view.Missions.Count * cfg.incompleteMissionPenalty
                   + Scoring.Poker(crew, self.CrewAbove, cfg).Score;
        }

        /// <summary>
        /// "Altrui, minimo noto" (09 §3): segnalini + monete per difetto − missioni in mano + poker delle sole carte sopra
        /// coperta. Le carte sotto coperta non si vedono: è un limite inferiore.
        /// </summary>
        public static int MinKnownScore(IReadOnlyGameState state, int player)
        {
            RulesConfig cfg = state.Config;
            IReadOnlyPlayerState other = state.Player(player);
            var visible = other.CrewAbove.Where(c => c != null).ToList();
            return other.BountyTokens + other.Coins / cfg.coinsPerToken - other.MissionsInHandCount * cfg.incompleteMissionPenalty
                   + Scoring.Poker(visible, other.CrewAbove, cfg).Score;
        }

        /// <summary>
        /// Vero se prendendo il Tesoro ora si perderebbe comunque contro qualcuno: "proprio" strettamente minore del
        /// minimo noto di almeno un avversario (a parità vince chi ha raggiunto l'Isola Sacra, R-147).
        /// </summary>
        public static bool WouldLoseTakingTreasure(PlayerView view)
        {
            int own = OwnScoreIfTakingTreasure(view);
            for (int player = 0; player < view.State.PlayerCount; player++)
                if (player != view.Viewer && own < MinKnownScore(view.State, player)) return true;
            return false;
        }

        // ---- Bersagli (R-100, R-101) ----

        /// <summary>Gittata della propria nave dalle carte sopra coperta (la stessa formula del motore).</summary>
        public static int Range(PlayerView view) => CombatFlow.Range(view.Self.CrewAbove, view.State.Config);

        /// <summary>Le navi avversarie in mare entro la gittata, se la propria nave è in mare.</summary>
        public static List<int> TargetsInRange(PlayerView view)
        {
            IReadOnlyGameState state = view.State;
            GameMap map = state.Map;
            Coord own = view.Self.Position;
            if (map.KindAt(own) != CellKind.Sea) return new List<int>();
            int range = Range(view);
            return Enumerable.Range(0, state.PlayerCount)
                .Where(p => p != view.Viewer && map.KindAt(state.Player(p).Position) == CellKind.Sea &&
                            Coord.Distance(state.Player(p).Position, own) <= range)
                .ToList();
        }

        /// <summary>Le navi avversarie in mare, a qualsiasi distanza.</summary>
        public static List<int> ShipsAtSea(PlayerView view)
        {
            IReadOnlyGameState state = view.State;
            return Enumerable.Range(0, state.PlayerCount)
                .Where(p => p != view.Viewer && state.Map.KindAt(state.Player(p).Position) == CellKind.Sea).ToList();
        }

        /// <summary>Il bersaglio (09 §3): tra i candidati, il più vicino all'Isola Sacra; a parità a caso. -1 se non ce ne sono.</summary>
        public static int ChooseTarget(PlayerView view, IEnumerable<int> candidates, IRandomSource random)
        {
            List<int> list = candidates.ToList();
            if (list.Count == 0) return -1;
            List<Coord> sacred = SacredIslandCells(view.State.Map);
            int DistanceToSacred(int p) => sacred.Count == 0 ? 0 : sacred.Min(c => Coord.Distance(c, view.State.Player(p).Position));
            int best = list.Min(DistanceToSacred);
            return Pick(list.Where(p => DistanceToSacred(p) == best).ToList(), random);
        }

        // ---- Ciurma (R-057, R-020) ----

        /// <summary>Rango di una carta nella priorità del profilo: 0 la più alta; le non elencate e gli slot vuoti valgono quanto la lista.</summary>
        public static int CrewRank(CrewCard card, BotProfile profile)
        {
            if (card == null) return profile.CrewPriority.Count;
            int index = -1;
            for (int i = 0; i < profile.CrewPriority.Count; i++)
                if (profile.CrewPriority[i] == card.Kind) index = i;
            return index < 0 ? profile.CrewPriority.Count : index;
        }

        /// <summary>La carta nello slot dato della propria nave (sopra coperta pubblica, sotto coperta dal proprio punto di vista).</summary>
        public static CrewCard OwnCrewAt(PlayerView view, int slot)
        {
            int above = view.Self.CrewAbove.Count;
            return slot < above ? view.Self.CrewAbove[slot] : view.CrewBelow[slot - above];
        }

        /// <summary>
        /// Quanto migliora la ciurma sopra coperta uno swap (09 §3): positivo solo se porta sopra una carta di priorità più
        /// alta di quella che scende. Gli swap dalla stessa parte non cambiano niente (0).
        /// </summary>
        public static int SwapGain(PlayerView view, SwapOption swap, BotProfile profile)
        {
            int above = view.Self.CrewAbove.Count;
            bool fromAbove = swap.From < above, toAbove = swap.To < above;
            if (fromAbove == toAbove) return 0;
            int upSlot = fromAbove ? swap.To : swap.From, downSlot = fromAbove ? swap.From : swap.To;
            return CrewRank(OwnCrewAt(view, downSlot), profile) - CrewRank(OwnCrewAt(view, upSlot), profile);
        }

        /// <summary>Lo swap più utile tra le opzioni (a caso tra i pari), oppure null se nessuno migliora la ciurma.</summary>
        public static SwapOption BestSwap(PlayerView view, IEnumerable<SwapOption> swaps, BotProfile profile, IRandomSource random)
        {
            var useful = swaps.Select(s => new { Swap = s, Gain = SwapGain(view, s, profile) }).Where(s => s.Gain > 0).ToList();
            if (useful.Count == 0) return null;
            int best = useful.Max(s => s.Gain);
            return Pick(useful.Where(s => s.Gain == best).Select(s => s.Swap).ToList(), random);
        }

        /// <summary>Rango di una carta Pirateria da tenere: 0 la più utile; le non elencate valgono quanto la lista (si perdono prima).</summary>
        public static int PirateRank(PirateCard card, BotProfile profile)
        {
            for (int i = 0; i < profile.PirateKeepPriority.Count; i++)
                if (profile.PirateKeepPriority[i] == card.Id) return i;
            return profile.PirateKeepPriority.Count;
        }

        public static T Pick<T>(IReadOnlyList<T> items, IRandomSource random) => items[random.Range(0, items.Count)];
    }
}
