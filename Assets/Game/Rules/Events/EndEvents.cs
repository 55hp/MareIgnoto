using System.Collections.Generic;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Engine;

namespace hp55games.MareIgnoto.Rules.Events
{
    /// <summary>
    /// Missione completata (R-132): la carta si rivela e la ricompensa in segnalini arriva subito (segue un
    /// <see cref="BountyChangedEvent"/>). <see cref="AtGameEnd"/> per le missioni valutate a fine partita (Olandese Volante).
    /// </summary>
    public sealed class MissionCompletedEvent : GameEvent
    {
        public int Player { get; }
        public MissionCard Card { get; }
        public int Reward { get; }
        public bool AtGameEnd { get; }

        public MissionCompletedEvent(int player, MissionCard card, int reward, bool atGameEnd)
        {
            Player = player;
            Card = card;
            Reward = reward;
            AtGameEnd = atGameEnd;
        }

        public override string Describe() =>
            "MissionCompleted p" + Player + " " + Card + " +" + Reward + (AtGameEnd ? " atGameEnd" : "");
    }

    /// <summary>
    /// Il Tesoro dell'Isola Sacra (R-140, R-141): a chi è entrato nel passo più basso; gli arrivi per Abbordaggio
    /// (<see cref="ByBoarding"/>, <see cref="Step"/> 0) vengono dopo ogni arrivo in movimento. A parità il segnalino va a
    /// ciascuno e le monete si dividono per difetto; il resto (<see cref="CoinsLost"/>) esce dal gioco.
    /// </summary>
    public sealed class TreasureTakenEvent : GameEvent
    {
        public IReadOnlyList<int> Players { get; }
        public int Step { get; }
        public bool ByBoarding { get; }
        public int TokensEach { get; }
        public int CoinsEach { get; }
        public int CoinsLost { get; }

        public TreasureTakenEvent(IReadOnlyList<int> players, int step, bool byBoarding, int tokensEach, int coinsEach, int coinsLost)
        {
            Players = players;
            Step = step;
            ByBoarding = byBoarding;
            TokensEach = tokensEach;
            CoinsEach = coinsEach;
            CoinsLost = coinsLost;
        }

        public override string Describe() =>
            "TreasureTaken [" + Join(Players) + "]" + (ByBoarding ? " byBoarding" : " s" + Step) + " tokens=" + TokensEach +
            " coins=" + CoinsEach + " lost=" + CoinsLost;
    }

    /// <summary>La taglia finale di un giocatore col dettaglio delle fonti (R-143–R-146). Pubblico: a fine partita si rivela tutto.</summary>
    public sealed class FinalScoreEvent : GameEvent
    {
        public PlayerScore Score { get; }

        public FinalScoreEvent(PlayerScore score)
        {
            Score = score;
        }

        public override string Describe() => "FinalScore " + Score;
    }
}
