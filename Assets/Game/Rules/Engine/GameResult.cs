namespace hp55games.MareIgnoto.Rules.Engine
{
    /// <summary>
    /// L'esito della partita. Per ora dice solo come e quando è finita; classifica, vincitori e dettaglio della taglia
    /// per giocatore (04_motore.md, spec 0004) si aggiungono quando esiste la fine partita vera (R-140–R-147).
    /// </summary>
    public sealed class GameResult
    {
        /// <summary>Vero se la partita è stata interrotta da <c>maxRounds</c> (R-150), cioè in una simulazione.</summary>
        public bool EndedByRoundLimit { get; }

        /// <summary>Round completati.</summary>
        public int RoundsPlayed { get; }

        public GameResult(bool endedByRoundLimit, int roundsPlayed)
        {
            EndedByRoundLimit = endedByRoundLimit;
            RoundsPlayed = roundsPlayed;
        }
    }
}
