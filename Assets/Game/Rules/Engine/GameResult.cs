using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Events;

namespace hp55games.MareIgnoto.Rules.Engine
{
    /// <summary>
    /// L'esito della partita (R-140–R-147): come è finita, la classifica con il dettaglio della taglia di ogni giocatore
    /// e i vincitori. Serve alla schermata finale e al tutorial.
    /// </summary>
    public sealed class GameResult
    {
        public GameEndReason Reason { get; }

        /// <summary>Vero se la partita è stata interrotta da <c>maxRounds</c> (R-150), cioè in una simulazione.</summary>
        public bool EndedByRoundLimit => Reason == GameEndReason.RoundLimit;

        /// <summary>Round completati (il round di cortesia compreso, R-142).</summary>
        public int RoundsPlayed { get; }

        /// <summary>La classifica: taglia decrescente, a parità prima chi ha preso il Tesoro (R-147), poi per id.</summary>
        public IReadOnlyList<PlayerScore> Ranking { get; }

        /// <summary>Chi ha <see cref="PlayerScore.Rank"/> 1: più di uno solo se la parità resta (R-147).</summary>
        public IReadOnlyList<int> Winners { get; }

        /// <summary>Chi ha preso il Tesoro (R-140, R-141); vuoto se la partita è finita per <c>maxRounds</c>.</summary>
        public IReadOnlyList<int> TreasureTakers { get; }

        public GameResult(GameEndReason reason, int roundsPlayed, IReadOnlyList<PlayerScore> ranking, IReadOnlyList<int> treasureTakers)
        {
            Reason = reason;
            RoundsPlayed = roundsPlayed;
            Ranking = ranking;
            TreasureTakers = treasureTakers;
            Winners = ranking.Where(s => s.Rank == 1).Select(s => s.Player).ToArray();
        }

        public PlayerScore ScoreOf(int player) => Ranking.First(s => s.Player == player);
    }

    /// <summary>
    /// La taglia finale di un giocatore (R-143): segnalini (battaglie, missioni, Tesoro) + 1 ogni <c>coinsPerToken</c>
    /// monete − missioni non completate + poker.
    /// </summary>
    public sealed class PlayerScore
    {
        public int Player { get; }
        /// <summary>Posizione in classifica, da 1; i pari merito hanno la stessa (R-147).</summary>
        public int Rank { get; internal set; }

        public int BattleTokens { get; }
        public int MissionTokens { get; }
        public int TreasureTokens { get; }
        /// <summary>Tutti i segnalini taglia (R-007): battaglie + missioni + Tesoro.</summary>
        public int Tokens => BattleTokens + MissionTokens + TreasureTokens;

        public int Coins { get; }
        /// <summary>Segnalini delle monete, per difetto.</summary>
        public int CoinTokens { get; }

        /// <summary>Missioni ancora in mano a fine partita (R-133).</summary>
        public int IncompleteMissions { get; }
        /// <summary>Segnalini tolti per le missioni non completate (valore positivo, si sottrae).</summary>
        public int MissionPenalty { get; }

        public PokerScore Poker { get; }

        /// <summary>Ha preso il Tesoro: vince le parità (R-147).</summary>
        public bool TookTreasure { get; }

        public int Total => Tokens + CoinTokens - MissionPenalty + Poker.Score;

        public PlayerScore(int player, int battleTokens, int missionTokens, int treasureTokens, int coins, int coinTokens,
            int incompleteMissions, int missionPenalty, PokerScore poker, bool tookTreasure)
        {
            Player = player;
            BattleTokens = battleTokens;
            MissionTokens = missionTokens;
            TreasureTokens = treasureTokens;
            Coins = coins;
            CoinTokens = coinTokens;
            IncompleteMissions = incompleteMissions;
            MissionPenalty = missionPenalty;
            Poker = poker;
            TookTreasure = tookTreasure;
        }

        public override string ToString() =>
            "p" + Player + " #" + Rank + " total=" + Total + " battles=" + BattleTokens + " missions=" + MissionTokens +
            " treasure=" + TreasureTokens + " coins=" + Coins + "(" + CoinTokens + ") incomplete=" + IncompleteMissions +
            "(-" + MissionPenalty + ") poker=" + Poker + (TookTreasure ? " tookTreasure" : "");
    }

    /// <summary>
    /// Il punteggio poker della ciurma (R-144–R-146): miglior combinazione dei 5 slot (03 §4), poi Jolly (due: punteggio
    /// fisso; uno: carta jolly e punteggio dimezzato per difetto), poi Nostromo sopra coperta (moltiplica, per copia).
    /// </summary>
    public sealed class PokerScore
    {
        /// <summary>Le carte della ciurma, sopra e sotto coperta.</summary>
        public IReadOnlyList<CrewCard> Cards { get; }

        /// <summary>La combinazione riconosciuta (con un Jolly: quella che il Jolly completa; con due: quella delle altre carte).</summary>
        public PokerHand Hand { get; }

        /// <summary>Segnalini della combinazione secondo la tabella (03 §4).</summary>
        public int TableScore { get; }

        public int Jokers { get; }

        /// <summary>Due Jolly: punteggio fisso, la tabella non conta (R-145).</summary>
        public bool JokerPair { get; }

        /// <summary>Un Jolly: il punteggio della tabella si dimezza per difetto (R-145).</summary>
        public bool HalvedByJoker { get; }

        /// <summary>Dopo R-145, prima del Nostromo.</summary>
        public int AfterJokers { get; }

        /// <summary>Moltiplicatore dei Nostromi sopra coperta (1 se nessuno, R-146).</summary>
        public int NostromoMultiplier { get; }

        public int Score => AfterJokers * NostromoMultiplier;

        public PokerScore(IReadOnlyList<CrewCard> cards, PokerHand hand, int tableScore, int jokers, bool jokerPair,
            bool halvedByJoker, int afterJokers, int nostromoMultiplier)
        {
            Cards = cards;
            Hand = hand;
            TableScore = tableScore;
            Jokers = jokers;
            JokerPair = jokerPair;
            HalvedByJoker = halvedByJoker;
            AfterJokers = afterJokers;
            NostromoMultiplier = nostromoMultiplier;
        }

        public override string ToString() =>
            Hand + "(" + TableScore + ")" + (JokerPair ? " jokerPair" : HalvedByJoker ? " halved" : "") +
            (NostromoMultiplier != 1 ? " x" + NostromoMultiplier : "") + " =" + Score;
    }
}
