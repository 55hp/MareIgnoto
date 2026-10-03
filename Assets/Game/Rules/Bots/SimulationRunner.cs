using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Engine;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.Random;
using hp55games.MareIgnoto.Rules.Setup;

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
            text.AppendLine("Simulazione: seed " + Options.FirstSeed + "-" + Options.LastSeed + ", maxRounds " +
                            Options.Config.maxRounds + ", bot casuale (seed del bot = seed della partita)");
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
            }

            text.AppendLine();
            List<SimulationFailure> failures = Failures.ToList();
            text.AppendLine(failures.Count == 0 ? "Errori: nessuno" : "Errori: " + failures.Count);
            foreach (SimulationFailure failure in failures.Take(50)) text.AppendLine("  " + failure);
            return text.ToString();
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
            try
            {
                session = GameSession.Start(GameSetup.ForPlayers(group.Players, seed), options.Config, options.Map);
                var bot = new RandomBot(new SeededRandom(seed));
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

                    session.Submit(bot.Choose(session.Pending));
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
