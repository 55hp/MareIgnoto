using System;
using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Bots;
using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Decisions;
using hp55games.MareIgnoto.Rules.Engine;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.Random;
using hp55games.MareIgnoto.Rules.Setup;
using NUnit.Framework;

namespace hp55games.MareIgnoto.Rules.Tests
{
    /// <summary>Costruzione di scenari e pilotaggio delle partite, condivisi dai test.</summary>
    internal static class TestSupport
    {
        /// <summary>La mappa del gioco: il layout v5 approvato (05_mappa.md §6), da <see cref="LayoutV5"/>.</summary>
        public static MapLayout StandardMap() => LayoutV5.Create();

        public static GameSession Start(int players, int seed, RulesConfig config = null, IRandomSource random = null,
            Action<GameSetup> customize = null)
        {
            GameSetup setup = GameSetup.ForPlayers(players, seed);
            customize?.Invoke(setup);
            return GameSession.Start(setup, config ?? new RulesConfig(), StandardMap(), random ?? new SeededRandom(seed));
        }

        /// <summary>
        /// Risponde con il bot casuale fino alla fine della preparazione (prima scelta della rotta), verificando gli
        /// invarianti dopo ogni risposta. Restituisce tutti gli eventi (iniziali compresi).
        /// </summary>
        public static List<GameEvent> PlayWithBot(GameSession session, int botSeed = 99)
        {
            var bot = new RandomBot(new SeededRandom(botSeed));
            return Play(session, bot.Choose);
        }

        /// <summary>Il bot gioca fino all'inizio del round <paramref name="rounds"/> + 1 (o alla fine della partita).</summary>
        public static List<GameEvent> PlayRounds(GameSession session, int rounds, int botSeed = 99)
        {
            var bot = new RandomBot(new SeededRandom(botSeed));
            return Play(session, bot.Choose, s => s.State.Round > rounds);
        }

        /// <summary>Il bot gioca finché la partita non finisce (serve maxRounds &gt; 0).</summary>
        public static List<GameEvent> PlayToEnd(GameSession session, int botSeed = 99)
        {
            var bot = new RandomBot(new SeededRandom(botSeed));
            return Play(session, bot.Choose, s => false);
        }

        /// <summary>Vero alla prima scelta della rotta del round 1: la preparazione della partita è finita.</summary>
        public static bool SetupDone(GameSession session) =>
            session.Pending.Kind == DecisionKind.ChooseHeading && session.State.Round == 1;

        /// <summary>
        /// Risponde con <paramref name="chooser"/> finché <paramref name="stop"/> (default: fine della preparazione)
        /// non è vero o la partita non finisce, verificando gli invarianti dopo ogni risposta.
        /// Restituisce gli eventi dall'inizio della partita se <paramref name="includeInitial"/>.
        /// </summary>
        public static List<GameEvent> Play(GameSession session, Func<PendingDecision, DecisionAnswer> chooser,
            Func<GameSession, bool> stop = null, bool includeInitial = true)
        {
            stop = stop ?? SetupDone;
            var events = includeInitial ? new List<GameEvent>(session.InitialEvents) : new List<GameEvent>();
            AssertInvariants(session);
            int guard = 0;
            while (session.Pending != null && !stop(session))
            {
                Assert.Less(guard++, 10000, "Il gioco non si ferma.");
                events.AddRange(session.Submit(chooser(session.Pending)));
                AssertInvariants(session);
            }

            return events;
        }

        /// <summary>Risponde alle offerte a Gartya con gli importi dati (per id giocatore) e al resto con la prima opzione.</summary>
        public static Func<PendingDecision, DecisionAnswer> WithOffers(params int[] offers)
        {
            return decision =>
            {
                if (decision.Kind != DecisionKind.GartyaOffer) return decision.Choose(0);
                int amount = offers[decision.Player];
                for (int i = 0; i < decision.Options.Count; i++)
                    if (((OfferOption)decision.Options[i]).Amount == amount) return decision.Choose(i);
                throw new InvalidOperationException("Offerta non disponibile: " + amount);
            };
        }

        public static void AssertInvariants(GameSession session)
        {
            IReadOnlyList<string> errors = session.CheckInvariants();
            Assert.IsEmpty(errors, string.Join("\n", errors));
        }

        public static string Log(IEnumerable<GameEvent> events) => string.Join("\n", events.Select(e => e.Describe()));
    }
}
