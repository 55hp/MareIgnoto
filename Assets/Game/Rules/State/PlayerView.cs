using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Map;

namespace hp55games.MareIgnoto.Rules.State
{
    /// <summary>
    /// Il punto di vista di un giocatore (04_motore.md §5): lo stato pubblico più le informazioni che solo
    /// lui può vedere. È un'istantanea: le liste sono copie, non cambiano con la partita.
    /// Le informazioni degli altri giocatori si leggono da <see cref="State"/>, che è pubblico.
    /// </summary>
    public sealed class PlayerView
    {
        public int Viewer { get; }
        public IReadOnlyGameState State { get; }

        /// <summary>La mano Pirateria.</summary>
        public IReadOnlyList<PirateCard> Hand { get; }
        /// <summary>Le missioni Corsaro in mano (R-130).</summary>
        public IReadOnlyList<MissionCard> Missions { get; }
        /// <summary>Le carte sotto coperta, una voce per slot (null se vuoto).</summary>
        public IReadOnlyList<CrewCard> CrewBelow { get; }
        /// <summary>La rotta scelta, anche prima della rivelazione; null se non ancora scelta.</summary>
        public Heading? ChosenHeading { get; }

        internal PlayerView(IReadOnlyGameState state, PlayerState player)
        {
            Viewer = player.Id;
            State = state;
            Hand = player.Hand.ToArray();
            Missions = player.Missions.ToArray();
            CrewBelow = player.Crew.BelowSlots().Select(slot => player.Crew[slot]).ToArray();
            ChosenHeading = player.ChosenHeading;
        }

        public IReadOnlyPlayerState Self => State.Player(Viewer);
    }
}
