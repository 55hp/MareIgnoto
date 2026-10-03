using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Decisions;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Engine;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.Random;
using hp55games.MareIgnoto.Rules.Setup;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Bots
{
    /// <summary>Cosa simulare (08_test-e-simulazione.md §3).</summary>
    public sealed class SimulationOptions
    {
        public IReadOnlyList<int> PlayerCounts { get; set; } = new[] { 2, 4, 8 };
        public int FirstSeed { get; set; } = 1;
        public int LastSeed { get; set; } = 1000;

        /// <summary>La configurazione delle partite; <c>maxRounds</c> &gt; 0 ferma le partite che non finiscono (R-150).</summary>
        public RulesConfig Config { get; set; }

        public MapLayout Map { get; set; }

        /// <summary>Protezione: una partita che chiede più decisioni di così è considerata bloccata.</summary>
        public int MaxDecisionsPerGame { get; set; } = 200000;

        /// <summary>
        /// Il bot di ogni posto (09_bot.md §5): un profilo, oppure null per il bot casuale. Null = tutti casuali (come prima,
        /// con un solo bot casuale per la partita). Con N giocatori si usano i primi N elementi (mancanti = casuali), e la
        /// disposizione ruota con il seed: il posto s ha l'elemento (s + seed) mod N.
        /// </summary>
        public IReadOnlyList<BotProfile> SeatProfiles { get; set; }

        /// <summary>Nome dello scenario, per il report.</summary>
        public string Name { get; set; }

        /// <summary>Il profilo del posto <paramref name="seat"/> in una partita di <paramref name="players"/> giocatori col seed dato; null = casuale.</summary>
        public BotProfile ProfileFor(int seat, int players, int seed)
        {
            if (SeatProfiles == null) return null;
            int index = ((seat + seed) % players + players) % players;
            return index < SeatProfiles.Count ? SeatProfiles[index] : null;
        }
    }

    /// <summary>Le statistiche dei posti con lo stesso bot (09 §5).</summary>
    public sealed class SimulationProfileStats
    {
        public string Name { get; }
        /// <summary>Posti giocati con questo bot (nelle partite arrivate al punteggio).</summary>
        public long Seats { get; internal set; }
        /// <summary>Vittorie, con i pari merito divisi.</summary>
        public double Wins { get; internal set; }
        public long Battle, Mission, Treasure, CoinTokens, Penalty, Poker, Total;

        public SimulationProfileStats(string name)
        {
            Name = name;
        }
    }

    /// <summary>Una partita finita male: eccezione, invariante violato o motore bloccato.</summary>
    public sealed class SimulationFailure
    {
        public int Players { get; }
        public int Seed { get; }
        public string Message { get; }

        public SimulationFailure(int players, int seed, string message)
        {
            Players = players;
            Seed = seed;
            Message = message;
        }

        public override string ToString() => Players + " giocatori, seed " + Seed + ": " + Message;
    }

    /// <summary>Le statistiche di un numero di giocatori.</summary>
    public sealed class SimulationGroup
    {
        public int Players { get; }
        public int Games { get; internal set; }
        /// <summary>Partite finite con l'Isola Sacra (R-140).</summary>
        public int Completed { get; internal set; }
        /// <summary>Seed delle partite interrotte da <c>maxRounds</c> (R-150).</summary>
        public List<int> StoppedSeeds { get; } = new List<int>();
        public List<SimulationFailure> Failures { get; } = new List<SimulationFailure>();

        /// <summary>Round delle partite finite con l'Isola Sacra.</summary>
        public List<int> Rounds { get; } = new List<int>();

        /// <summary>Somme su tutti i giocatori di tutte le partite arrivate al punteggio.</summary>
        public long ScoredPlayers, Battle, Mission, Treasure, CoinTokens, Penalty, Poker, Total;
        public long MissionsCompleted, WinnerTotal, WinnerTookTreasure, ScoredGames, TiedGames;
        public Dictionary<PokerHand, int> PokerHands { get; } = new Dictionary<PokerHand, int>();

        /// <summary>Per bot (solo con posti configurati, 09 §5).</summary>
        public Dictionary<string, SimulationProfileStats> Profiles { get; } = new Dictionary<string, SimulationProfileStats>();

        /// <summary>Battaglie (R-104) e Abbordaggi fortuiti (R-070) in tutte le partite arrivate al punteggio.</summary>
        public long Battles, Boardings;

        /// <summary>Partite in cui un Rush è entrato nell'Isola Sacra, e vittorie (divise) dei Rush entrati.</summary>
        public long RushEntries;
        public double RushWinsAfterEntering;

        public SimulationGroup(int players)
        {
            Players = players;
        }
    }

    /// <summary>L'esito di una simulazione, con il riepilogo testuale per il report.</summary>
    public sealed class SimulationReport
    {
        public SimulationOptions Options { get; }
        public IReadOnlyList<SimulationGroup> Groups { get; }

        public IEnumerable<SimulationFailure> Failures => Groups.SelectMany(g => g.Failures);

        /// <summary>Nessuna eccezione, nessun invariante violato, nessuna partita bloccata.</summary>
        public bool Clean => !Failures.Any();

        public SimulationReport(SimulationOptions options, IReadOnlyList<SimulationGroup> groups)
        {
            Options = options;
            Groups = groups;
        }

        public string ToText()
        {
            var text = new StringBuilder();
            if (!string.IsNullOrEmpty(Options.Name)) text.AppendLine("Scenario " + Options.Name);
            text.AppendLine("Simulazione: seed " + Options.FirstSeed + "-" + Options.LastSeed + ", maxRounds " +
                            Options.Config.maxRounds + ", " + SeatsText());
            foreach (SimulationGroup g in Groups)
            {
                text.AppendLine();
                text.AppendLine(g.Players + " giocatori: " + g.Games + " partite, " + g.Completed + " finite con l'Isola Sacra, " +
                                g.StoppedSeeds.Count + " interrotte da maxRounds, " + g.Failures.Count + " errori");
                if (g.Rounds.Count > 0)
                    text.AppendLine("  round (partite finite): min " + g.Rounds.Min() + ", media " + F(g.Rounds.Average()) +
                                    ", mediana " + Median(g.Rounds) + ", max " + g.Rounds.Max());
                if (g.StoppedSeeds.Count > 0)
                    text.AppendLine("  interrotte da maxRounds, seed: " + string.Join(", ", g.StoppedSeeds));
                if (g.ScoredPlayers == 0) continue;

                double n = g.ScoredPlayers;
                text.AppendLine("  taglia media per giocatore: " + F(g.Total / n) + " = battaglie " + F(g.Battle / n) +
                                " + missioni " + F(g.Mission / n) + " + Tesoro " + F(g.Treasure / n) + " + monete " +
                                F(g.CoinTokens / n) + " - missioni incomplete " + F(g.Penalty / n) + " + poker " + F(g.Poker / n));
                double positive = g.Battle + g.Mission + g.Treasure + g.CoinTokens + g.Poker;
                if (positive > 0)
                    text.AppendLine("  quota delle fonti positive: battaglie " + P(g.Battle / positive) + ", missioni " +
                                    P(g.Mission / positive) + ", Tesoro " + P(g.Treasure / positive) + ", monete " +
                                    P(g.CoinTokens / positive) + ", poker " + P(g.Poker / positive));
                text.AppendLine("  missioni completate per partita: " + F(g.MissionsCompleted / (double)g.ScoredGames) +
                                "; vincitore: taglia media " + F(g.WinnerTotal / (double)g.ScoredGames) +
                                ", ha preso il Tesoro nel " + P(g.WinnerTookTreasure / (double)g.ScoredGames) +
                                " delle partite; parità al primo posto: " + g.TiedGames);
                text.AppendLine("  poker: " + string.Join(", ", g.PokerHands.OrderBy(h => h.Key)
                    .Select(h => h.Key + " " + P(h.Value / n))));
                if (Options.SeatProfiles != null) AppendProfiles(text, g);
            }

            text.AppendLine();
            List<SimulationFailure> failures = Failures.ToList();
            text.AppendLine(failures.Count == 0 ? "Errori: nessuno" : "Errori: " + failures.Count);
            foreach (SimulationFailure failure in failures.Take(50)) text.AppendLine("  " + failure);
            return text.ToString();
        }

        private string SeatsText()
        {
            if (Options.SeatProfiles == null) return "bot casuale (seed del bot = seed della partita)";
            return "posti: " + string.Join(", ", Options.SeatProfiles.Select(p => p?.Name ?? "casuale")) +
                   " (con N giocatori i primi N, ruotati col seed)";
        }

        private static void AppendProfiles(StringBuilder text, SimulationGroup g)
        {
            double games = Math.Max(1, g.ScoredGames);
            text.AppendLine("  battaglie per partita: " + F(g.Battles / games) + "; Abbordaggi per partita: " + F(g.Boardings / games));
            foreach (SimulationProfileStats p in g.Profiles.Values.OrderBy(p => p.Name))
            {
                double seats = Math.Max(1, p.Seats);
                text.AppendLine("  " + p.Name + ": " + p.Seats + " posti, vittorie " + F(p.Wins) + " (" + P(p.Wins / games) +
                                " delle partite); taglia media " + F(p.Total / seats) + " = battaglie " + F(p.Battle / seats) +
                                " + missioni " + F(p.Mission / seats) + " + Tesoro " + F(p.Treasure / seats) + " + monete " +
                                F(p.CoinTokens / seats) + " - incomplete " + F(p.Penalty / seats) + " + poker " + F(p.Poker / seats));
            }

            if (g.Profiles.ContainsKey(BotProfile.Rush.Name))
                text.AppendLine("  Rush nell'Isola Sacra: in " + g.RushEntries + " partite (" + P(g.RushEntries / games) +
                                "); vittorie dopo esserci entrato: " + F(g.RushWinsAfterEntering) +
                                (g.RushEntries > 0 ? " (" + P(g.RushWinsAfterEntering / g.RushEntries) + ")" : ""));
        }

        private static string F(double value) => value.ToString("0.00", CultureInfo.InvariantCulture);

        private static string P(double ratio) => (ratio * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%";

        private static int Median(List<int> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            return sorted[sorted.Count / 2];
        }
    }

    /// <summary>
    /// La simulazione di 08_test-e-simulazione.md §3: partite complete con bot casuali, per ogni numero di giocatori e seed,
    /// con gli invarianti di 04_motore.md §6 verificati dopo ogni risposta. Deterministica: stessi parametri, stesso report.
    /// </summary>
    public static class SimulationRunner
    {
        public static SimulationReport Run(SimulationOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (options.Config == null || options.Map == null) throw new ArgumentException("Servono configurazione e mappa.", nameof(options));

            var groups = new List<SimulationGroup>();
            foreach (int players in options.PlayerCounts)
            {
                var group = new SimulationGroup(players);
                for (int seed = options.FirstSeed; seed <= options.LastSeed; seed++) PlayOne(options, group, seed);
                groups.Add(group);
            }

            return new SimulationReport(options, groups);
        }

        private static void PlayOne(SimulationOptions options, SimulationGroup group, int seed)
        {
            group.Games++;
            GameSession session;
            var sacredEntrants = new HashSet<int>();
            try
            {
                session = GameSession.Start(GameSetup.ForPlayers(group.Players, seed), options.Config, options.Map);
                var randomBot = new RandomBot(new SeededRandom(seed));
                var bots = new IBot[group.Players];
                for (int seat = 0; seat < group.Players; seat++)
                {
                    BotProfile profile = options.ProfileFor(seat, group.Players, seed);
                    bots[seat] = profile == null
                        ? (IBot)randomBot
                        : new StrategicBot(profile, new SeededRandom(StrategicBot.SeedFor(seed, seat)));
                }

                if (!Check(session, group, seed)) return;

                int decisions = 0;
                while (session.Pending != null)
                {
                    if (++decisions > options.MaxDecisionsPerGame)
                    {
                        group.Failures.Add(new SimulationFailure(group.Players, seed, "oltre " + options.MaxDecisionsPerGame +
                                                                                      " decisioni al round " + session.State.Round));
                        return;
                    }

                    PendingDecision pending = session.Pending;
                    IBot bot = bots[pending.Player];
                    PlayerView view = bot is RandomBot ? null : session.State.ViewFor(pending.Player);
                    IReadOnlyList<GameEvent> events = session.Submit(bot.Choose(pending, view));
                    if (options.SeatProfiles != null) Count(group, events, options, seed, sacredEntrants);
                    if (!Check(session, group, seed)) return;
                }
            }
            catch (Exception e)
            {
                group.Failures.Add(new SimulationFailure(group.Players, seed, e.GetType().Name + ": " + e.Message));
                return;
            }

            GameResult result = session.Result;
            if (!session.IsOver || result == null)
            {
                group.Failures.Add(new SimulationFailure(group.Players, seed, "nessuna decisione in attesa ma partita non finita"));
                return;
            }

            if (result.EndedByRoundLimit) group.StoppedSeeds.Add(seed);
            else
            {
                group.Completed++;
                group.Rounds.Add(result.RoundsPlayed);
            }

            Collect(group, result, session);
            if (options.SeatProfiles != null) CollectProfiles(group, result, options, seed, sacredEntrants);
        }

        private static string LabelOf(SimulationOptions options, int seat, int players, int seed) =>
            options.ProfileFor(seat, players, seed)?.Name ?? "casuale";

        /// <summary>Battaglie, Abbordaggi e ingressi nell'Isola Sacra, dagli eventi.</summary>
        private static void Count(SimulationGroup g, IReadOnlyList<GameEvent> events, SimulationOptions options, int seed,
            HashSet<int> sacredEntrants)
        {
            foreach (GameEvent e in events)
            {
                if (e is BattleWonEvent) g.Battles++;
                else if (e is BoardingStartedEvent) g.Boardings++;
                else if (e is SacredIslandEnteredEvent entered) sacredEntrants.Add(entered.Player);
            }
        }

        private static void CollectProfiles(SimulationGroup g, GameResult result, SimulationOptions options, int seed,
            HashSet<int> sacredEntrants)
        {
            int players = result.Ranking.Count;
            foreach (PlayerScore score in result.Ranking)
            {
                string label = LabelOf(options, score.Player, players, seed);
                if (!g.Profiles.TryGetValue(label, out SimulationProfileStats p)) g.Profiles[label] = p = new SimulationProfileStats(label);
                p.Seats++;
                if (result.Winners.Contains(score.Player)) p.Wins += 1.0 / result.Winners.Count;
                p.Battle += score.BattleTokens;
                p.Mission += score.MissionTokens;
                p.Treasure += score.TreasureTokens;
                p.CoinTokens += score.CoinTokens;
                p.Penalty += score.MissionPenalty;
                p.Poker += score.Poker.Score;
                p.Total += score.Total;
            }

            var rushEntrants = sacredEntrants.Where(seat => LabelOf(options, seat, players, seed) == BotProfile.Rush.Name).ToList();
            if (rushEntrants.Count == 0) return;
            g.RushEntries++;
            g.RushWinsAfterEntering += rushEntrants.Count(seat => result.Winners.Contains(seat)) / (double)result.Winners.Count;
        }

        private static bool Check(GameSession session, SimulationGroup group, int seed)
        {
            IReadOnlyList<string> errors = session.CheckInvariants();
            if (errors.Count == 0) return true;
            group.Failures.Add(new SimulationFailure(group.Players, seed, "round " + session.State.Round + ": " + string.Join(" | ", errors)));
            return false;
        }

        private static void Collect(SimulationGroup g, GameResult result, GameSession session)
        {
            g.ScoredGames++;
            g.MissionsCompleted += session.State.Players.Sum(p => p.CompletedMissions.Count);
            if (result.Winners.Count > 1) g.TiedGames++;
            foreach (PlayerScore score in result.Ranking)
            {
                g.ScoredPlayers++;
                g.Battle += score.BattleTokens;
                g.Mission += score.MissionTokens;
                g.Treasure += score.TreasureTokens;
                g.CoinTokens += score.CoinTokens;
                g.Penalty += score.MissionPenalty;
                g.Poker += score.Poker.Score;
                g.Total += score.Total;
                g.PokerHands[score.Poker.Hand] = (g.PokerHands.TryGetValue(score.Poker.Hand, out int n) ? n : 0) + 1;
            }

            PlayerScore winner = result.ScoreOf(result.Winners[0]);
            g.WinnerTotal += winner.Total;
            if (winner.TookTreasure) g.WinnerTookTreasure++;
        }
    }
}
