using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Map;

namespace hp55games.MareIgnoto.Rules.State
{
    /// <summary>Stato mutabile di un giocatore. Visibile fuori dal motore solo come <see cref="IReadOnlyPlayerState"/>.</summary>
    internal sealed class PlayerState : IReadOnlyPlayerState
    {
        public int Id { get; }
        public string Name { get; }
        public Coord Position { get; set; }
        public int Coins { get; set; }
        public int BountyTokens { get; set; }
        public CrewSlots Crew { get; }
        public List<PirateCard> Hand { get; } = new List<PirateCard>();
        public List<MissionCard> Missions { get; } = new List<MissionCard>();
        public List<MissionCard> Completed { get; } = new List<MissionCard>();

        /// <summary>
        /// Carte pescate in attesa di una scelta (crew da piazzare, missioni da tenere, crew di Reclutamento...).
        /// Contano nella conservazione delle carte.
        /// </summary>
        public List<Card> InTransit { get; } = new List<Card>();

        /// <summary>La rotta scelta in Fase 1; segreta finché non viene rivelata.</summary>
        public Heading? ChosenHeading { get; set; }
        public bool HeadingRevealed { get; set; }

        /// <summary>Round in cui la nave resta in porto per Svago (R-045, R-091); 0 se nessuno.</summary>
        public int LeisureRound { get; set; }

        /// <summary>Round nella cui Fase 1 la nave è immune al meteo per Vento in Poppa (R-087); 0 se nessuno.</summary>
        public int TailwindRound { get; set; }

        public PlayerState(int id, string name, int slotsAbove, int slotsBelow)
        {
            Id = id;
            Name = name;
            Crew = new CrewSlots(slotsAbove, slotsBelow);
        }

        public Heading? RevealedHeading => HeadingRevealed ? ChosenHeading : null;

        public IReadOnlyList<CrewCard> CrewAbove
        {
            get
            {
                var above = new CrewCard[Crew.AboveCount];
                for (int i = 0; i < above.Length; i++) above[i] = Crew[i];
                return above;
            }
        }

        public IReadOnlyList<bool> CrewBelowOccupied => Crew.BelowSlots().Select(slot => Crew[slot] != null).ToArray();

        public int HandCount => Hand.Count;
        public int MissionsInHandCount => Missions.Count;
        IReadOnlyList<MissionCard> IReadOnlyPlayerState.CompletedMissions => Completed;
    }
}
