using System;
using System.Collections.Generic;
using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Decisions;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.Random;
using hp55games.MareIgnoto.Rules.Setup;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Engine
{
    /// <summary>
    /// Una partita (04_motore.md §1). UI, tutorial, bot e test la guidano allo stesso modo: leggono
    /// <see cref="Pending"/>, rispondono con <see cref="Submit"/>. Deterministica: stesso setup, stessa
    /// sorgente casuale e stesse risposte producono la stessa partita, con eventi identici.
    /// </summary>
    public sealed class GameSession
    {
        private readonly GameState state;
        private readonly GameContext context;
        private readonly FlowRunner runner;

        public IReadOnlyGameState State => state;

        /// <summary>
        /// La decisione in attesa. Provvisorio fino alla spec 0002: dopo la rivelazione delle rotte del primo
        /// round il motore non ha ancora altro da fare e <c>Pending</c> è null anche se la partita non è finita.
        /// </summary>
        public PendingDecision Pending => runner.Pending;

        public bool IsOver => state.Phase == GamePhase.Ended;

        /// <summary>Null finché <see cref="IsOver"/> è falso.</summary>
        public GameResult Result { get; private set; }

        /// <summary>Gli eventi prodotti da <see cref="Start(GameSetup, RulesConfig, MapLayout, IRandomSource)"/> (preparazione e pescate).</summary>
        public IReadOnlyList<GameEvent> InitialEvents { get; }

        private GameSession(GameState state, GameContext context, FlowRunner runner)
        {
            this.state = state;
            this.context = context;
            this.runner = runner;
            runner.Run();
            InitialEvents = context.TakeEvents();
        }

        /// <summary>
        /// Prepara la partita (R-030–R-038) e si ferma alla prima decisione. Lancia ArgumentException se
        /// setup, configurazione o mappa non sono validi.
        /// </summary>
        public static GameSession Start(GameSetup setup, RulesConfig config, MapLayout map, IRandomSource random)
        {
            if (setup == null) throw new ArgumentNullException(nameof(setup));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (random == null) throw new ArgumentNullException(nameof(random));

            IReadOnlyList<string> configErrors = config.Validate();
            if (configErrors.Count > 0)
                throw new ArgumentException("RulesConfig non valida:\n" + string.Join("\n", configErrors), nameof(config));

            GameMap gameMap = GameMap.Create(map, config);
            ValidateSetup(setup, config, gameMap);

            var state = new GameState(config, gameMap);
            state.BuildDecks();
            foreach (PlayerSetup player in OrderedBySeat(setup))
                state.Players.Add(new PlayerState(player.Seat, player.Name, config.slotsAbove, config.slotsBelow));

            var context = new GameContext(state, config, random, setup);
            return new GameSession(state, context, new FlowRunner(GameFlow.Run(context)));
        }

        /// <summary>Come sopra, con la sorgente standard seedata da <see cref="GameSetup.Seed"/>.</summary>
        public static GameSession Start(GameSetup setup, RulesConfig config, MapLayout map)
        {
            if (setup == null) throw new ArgumentNullException(nameof(setup));
            return Start(setup, config, map, new SeededRandom(setup.Seed));
        }

        /// <summary>
        /// Valida la risposta contro <see cref="Pending"/>; se non è valida lancia <see cref="InvalidDecisionException"/>
        /// e lo stato non cambia. Se è valida avanza il gioco fino alla prossima decisione che richiede un giocatore
        /// e restituisce gli eventi prodotti, in ordine.
        /// </summary>
        public IReadOnlyList<GameEvent> Submit(DecisionAnswer answer)
        {
            PendingDecision pending = runner.Pending;
            if (answer == null) throw new InvalidDecisionException("Risposta nulla.");
            if (pending == null) throw new InvalidDecisionException("Nessuna decisione in attesa.");
            if (answer.DecisionId != pending.Id)
                throw new InvalidDecisionException("La risposta è per la decisione " + answer.DecisionId +
                                                   ", ma quella in attesa è la " + pending.Id + ".");
            if (answer.OptionIndex < 0 || answer.OptionIndex >= pending.Options.Count)
                throw new InvalidDecisionException("Opzione " + answer.OptionIndex + " inesistente: la decisione ne ha " +
                                                   pending.Options.Count + ".");

            runner.Answer(pending.Options[answer.OptionIndex]);
            return context.TakeEvents();
        }

        /// <summary>Verifica gli invarianti di 04_motore.md §6 sullo stato attuale; elenco vuoto se tutti rispettati.</summary>
        public IReadOnlyList<string> CheckInvariants() => Invariants.Check(state);

        private static IEnumerable<PlayerSetup> OrderedBySeat(GameSetup setup)
        {
            var players = new List<PlayerSetup>(setup.Players);
            players.Sort((a, b) => a.Seat.CompareTo(b.Seat));
            return players;
        }

        private static void ValidateSetup(GameSetup setup, RulesConfig config, GameMap map)
        {
            int count = setup.Players.Count;
            if (count < config.minPlayers || count > config.maxPlayers)
                throw new ArgumentException("I giocatori devono essere da " + config.minPlayers + " a " + config.maxPlayers +
                                            " (trovati " + count + ").", nameof(setup));
            if (map.SpawnPointsFor(count).Count != count)
                throw new ArgumentException("La mappa non ha un preset di punti di partenza per " + count + " giocatori.", nameof(setup));

            var seats = new HashSet<int>();
            foreach (PlayerSetup player in setup.Players)
            {
                if (player.Seat < 0 || player.Seat >= count || !seats.Add(player.Seat))
                    throw new ArgumentException("I posti devono essere i numeri da 0 a " + (count - 1) + ", una volta ciascuno.", nameof(setup));
            }

            TutorialOptions tutorial = setup.Tutorial;
            if (tutorial?.InitialZones == null) return;
            foreach (ZoneSetup zone in tutorial.InitialZones)
                if (zone.Zone < 0 || zone.Zone >= map.ZoneCount)
                    throw new ArgumentException("Zona meteo inesistente nel setup del tutorial: " + zone.Zone + ".", nameof(setup));
        }
    }
}
