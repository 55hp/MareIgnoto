using System;
using System.Collections.Generic;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Map;

namespace hp55games.MareIgnoto.Rules.State
{
    /// <summary>
    /// Lo stato mutabile della partita. Lo modificano solo i flussi del motore (cartella Engine);
    /// fuori dall'assembly si vede solo come <see cref="IReadOnlyGameState"/>.
    /// </summary>
    internal sealed class GameState : IReadOnlyGameState
    {
        private int nextUid = 1;

        public RulesConfig Config { get; }
        public GameMap Map { get; }
        public List<PlayerState> Players { get; } = new List<PlayerState>();
        public DeckState<CrewCard> Crew { get; } = new DeckState<CrewCard>();
        public DeckState<PirateCard> Pirate { get; } = new DeckState<PirateCard>();
        public DeckState<MissionCard> Corsair { get; } = new DeckState<MissionCard>();
        public WeatherState[] ZoneStates { get; }
        public List<int> TurnOrderList { get; } = new List<int>();

        /// <summary>Vero dal movimento alla fine degli Abbordaggi: intanto le navi possono condividere celle di mare.</summary>
        public bool MovementInProgress { get; set; }

        /// <summary>
        /// Celle di mare che possono ospitare più navi fino al prossimo movimento: esito di un Abbordaggio a 3+ navi
        /// (R-071) o del limite della catena (R-073a).
        /// </summary>
        public HashSet<Coord> SharedSeaCellsAllowed { get; } = new HashSet<Coord>();

        /// <summary>Chi ha già giocato il turno nella Fase 2 del round, in ordine; si azzera all'inizio di ogni Fase 2.</summary>
        public List<int> TurnsPlayed { get; } = new List<int>();

        /// <summary>
        /// Uid dei Nostromi saliti sopra coperta: non possono più tornare sotto (R-016), salvo il Medico (R-023), che li
        /// sposta in <see cref="NostromiFreedByMedico"/>.
        /// </summary>
        public HashSet<int> LockedNostromi { get; } = new HashSet<int>();

        /// <summary>
        /// Uid dei Nostromi riportati sotto coperta dal Medico (R-023), l'unica via d'uscita dal blocco di R-016. Quando
        /// risalgono sopra coperta tornano in <see cref="LockedNostromi"/>.
        /// </summary>
        public HashSet<int> NostromiFreedByMedico { get; } = new HashSet<int>();

        /// <summary>Avanzamento delle missioni in mano, per Uid della carta (R-131: nasce quando la missione si tiene).</summary>
        public Dictionary<int, Engine.MissionProgress> MissionProgress { get; } = new Dictionary<int, Engine.MissionProgress>();

        /// <summary>
        /// Navi entrate nell'Isola Sacra nel movimento di questo round, col passo (R-140, R-141). Non vuoto: la partita
        /// finisce alla fine del round (R-142).
        /// </summary>
        public Dictionary<int, int> SacredIslandArrivals { get; } = new Dictionary<int, int>();

        /// <summary>Chi ha preso il Tesoro (R-140, R-141); vuoto finché nessuno è entrato nell'Isola Sacra.</summary>
        public List<int> TreasureTakers { get; } = new List<int>();

        /// <summary>L'esito, quando la partita finisce.</summary>
        public Engine.GameResult Result { get; set; }

        public int Round { get; set; }
        public GamePhase Phase { get; set; } = GamePhase.Setup;
        public int ActivePlayer { get; set; } = -1;
        public Heading Wind { get; set; }
        public int Treasure { get; set; }

        /// <summary>Carte di ogni mazzo all'inizio della partita: base della verifica di conservazione.</summary>
        public int InitialCrewCards { get; private set; }
        public int InitialPirateCards { get; private set; }
        public int InitialCorsairCards { get; private set; }

        public GameState(RulesConfig config, GameMap map)
        {
            Config = config;
            Map = map;
            ZoneStates = new WeatherState[map.ZoneCount];
        }

        public int NextUid() => nextUid++;

        /// <summary>Crea i tre mazzi dalla configurazione (R-003, R-004, R-005), nell'ordine di creazione, non mescolati.</summary>
        public void BuildDecks()
        {
            var crew = new List<CrewCard>();
            for (int rank = CrewCatalog.MinRank; rank <= CrewCatalog.MaxRank; rank++)
                foreach (CrewSuit suit in CrewCatalog.SuitsInDeck)
                    crew.Add(new CrewCard(NextUid(), CrewCatalog.KindForRank(rank), rank, suit));
            for (int i = 0; i < Config.jokerCount; i++)
                crew.Add(new CrewCard(NextUid(), CrewCardId.Jolly, 0, CrewSuit.None));
            Crew.Fill(crew);
            InitialCrewCards = crew.Count;

            var pirate = new List<PirateCard>();
            foreach (PirateCardEntry entry in Config.pirateCards)
                for (int i = 0; i < entry.copies; i++)
                    pirate.Add(new PirateCard(NextUid(), entry.id));
            Pirate.Fill(pirate);
            InitialPirateCards = pirate.Count;

            var corsair = new List<MissionCard>();
            foreach (MissionEntry entry in Config.missions)
                for (int i = 0; i < entry.copies; i++)
                    corsair.Add(new MissionCard(NextUid(), entry.id));
            Corsair.Fill(corsair);
            InitialCorsairCards = corsair.Count;
        }

        public PlayerState PlayerById(int id)
        {
            if (id < 0 || id >= Players.Count)
                throw new ArgumentOutOfRangeException(nameof(id), "Giocatore inesistente: " + id);
            return Players[id];
        }

        // ---- IReadOnlyGameState ----

        public int PlayerCount => Players.Count;
        IReadOnlyList<IReadOnlyPlayerState> IReadOnlyGameState.Players => Players;
        public IReadOnlyPlayerState Player(int id) => PlayerById(id);
        GameMap IReadOnlyGameState.Map => Map;
        IReadOnlyList<int> IReadOnlyGameState.TurnOrder => TurnOrderList;
        IReadOnlyList<WeatherState> IReadOnlyGameState.Zones => ZoneStates;
        public DeckInfo CrewDeck => Crew.Info(DeckKind.Crew);
        public DeckInfo PirateDeck => Pirate.Info(DeckKind.Pirate);
        public DeckInfo CorsairDeck => Corsair.Info(DeckKind.Corsair);

        public PlayerView ViewFor(int playerId) => new PlayerView(this, PlayerById(playerId));
    }
}
