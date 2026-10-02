using System;
using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Decisions;
using hp55games.MareIgnoto.Rules.Engine;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.Random;
using hp55games.MareIgnoto.Rules.Setup;
using hp55games.MareIgnoto.Rules.State;
using NUnit.Framework;

namespace hp55games.MareIgnoto.Rules.Tests
{
    public class SetupTests
    {
        private static readonly RulesConfig Config = new RulesConfig();

        [TestCase(2)]
        [TestCase(4)]
        [TestCase(8)]
        public void AfterSetupTheGameAsksForTheFirstHeading(int players)
        {
            GameSession session = TestSupport.Start(players, 5);
            var bot = new hp55games.MareIgnoto.Rules.Bots.RandomBot(new SeededRandom(5));
            while (session.Pending.Kind != DecisionKind.ChooseHeading)
                session.Submit(bot.Choose(session.Pending));

            IReadOnlyGameState state = session.State;
            Assert.AreEqual(1, state.Round);
            Assert.AreEqual(GamePhase.Preparation, state.Phase);
            Assert.AreEqual(players, state.PlayerCount);
            Assert.AreEqual(state.TurnOrder[0], session.Pending.Player, "la prima rotta la sceglie il primo di turno");
            Assert.IsFalse(session.IsOver);
            Assert.IsNull(session.Result);
            TestSupport.AssertInvariants(session);
        }

        [TestCase(2)]
        [TestCase(4)]
        [TestCase(8)]
        public void EveryPlayerStartsAsTheRulesSay_R031_R032_R033_R034_R035(int players)
        {
            GameSession session = TestSupport.Start(players, 11);
            GameMap map = GameMap.Create(TestSupport.StandardMap(), Config);
            var offers = Enumerable.Range(0, players).Select(i => i % 4).ToArray();
            TestSupport.Play(session, TestSupport.WithOffers(offers));

            int totalOffers = 0;
            for (int id = 0; id < players; id++)
            {
                PlayerView view = session.State.ViewFor(id);
                IReadOnlyPlayerState player = session.State.Player(id);

                Assert.AreEqual(map.SpawnPointsFor(players)[id], player.Position, "R-031 p" + id);
                Assert.AreEqual(Config.slotsAbove, player.CrewAbove.Count);
                Assert.IsTrue(player.CrewAbove.All(c => c == null), "R-032: niente sopra coperta");
                Assert.AreEqual(Config.startingCrewCards, view.CrewBelow.Count(c => c != null), "R-032 p" + id);
                Assert.AreEqual(Config.startingCrewCards, player.CrewBelowOccupied.Count(o => o));
                Assert.AreEqual(Config.startingPirateCards, view.Hand.Count, "R-033 p" + id);
                Assert.AreEqual(Config.startingPirateCards, player.HandCount);
                Assert.That(view.Missions.Count, Is.InRange(Config.startingMissionsMinKept, Config.startingMissionsDrawn), "R-034");
                Assert.AreEqual(Config.startingCoins - offers[id], player.Coins, "R-035/R-036 p" + id);
                Assert.AreEqual(0, player.BountyTokens);
                totalOffers += offers[id];
            }

            Assert.AreEqual(totalOffers, session.State.Treasure, "R-036: le offerte vanno nel Tesoro");
        }

        [TestCase(2)]
        [TestCase(4)]
        [TestCase(8)]
        public void CardsAreConservedAfterSetup_Invariant(int players)
        {
            GameSession session = TestSupport.Start(players, 21);
            TestSupport.PlayWithBot(session);
            IReadOnlyGameState state = session.State;

            int crewInHands = Enumerable.Range(0, players).Sum(id => state.ViewFor(id).CrewBelow.Count(c => c != null));
            int crewTotal = CrewCatalog.MaxRank * CrewCatalog.SuitsInDeck.Count + Config.jokerCount;
            Assert.AreEqual(crewTotal, state.CrewDeck.DrawCount + state.CrewDeck.DiscardCount + crewInHands);

            int pirateInHands = Enumerable.Range(0, players).Sum(id => state.Player(id).HandCount);
            Assert.AreEqual(Config.pirateCards.Sum(e => e.copies), state.PirateDeck.DrawCount + state.PirateDeck.DiscardCount + pirateInHands);

            int missionsKept = Enumerable.Range(0, players).Sum(id => state.Player(id).MissionsInHandCount);
            Assert.AreEqual(Config.missions.Sum(e => e.copies), state.CorsairDeck.DrawCount + state.CorsairDeck.DiscardCount + missionsKept);
            Assert.AreEqual(players * Config.startingMissionsDrawn - missionsKept, state.CorsairDeck.DiscardCount,
                "le missioni non tenute vanno negli scarti Corsaro");
        }

        [Test]
        public void DecksAreShuffledAtSetup_R030()
        {
            GameSession session = TestSupport.Start(4, 31);
            var shuffled = session.InitialEvents.OfType<DeckShuffledEvent>().ToList();
            CollectionAssert.AreEquivalent(
                new[] { DeckKind.Crew, DeckKind.Pirate, DeckKind.Corsair },
                shuffled.Select(e => e.Deck).ToArray());
            Assert.IsTrue(shuffled.All(e => !e.FromDiscard));

            GameSession other = TestSupport.Start(4, 32);
            var firstCrew = session.InitialEvents.OfType<CardsDrawnEvent>().First(e => e.Deck == DeckKind.Crew && e.Player == 0);
            var otherCrew = other.InitialEvents.OfType<CardsDrawnEvent>().First(e => e.Deck == DeckKind.Crew && e.Player == 0);
            CollectionAssert.AreNotEqual(firstCrew.Cards.Select(c => c.Uid), otherCrew.Cards.Select(c => c.Uid),
                "seed diversi → mazzi diversi (con probabilità praticamente 1)");
        }

        [Test]
        public void SetupDecisionsAreSecretAndOfTheRightKind_R032_R034_R036_R040()
        {
            GameSession session = TestSupport.Start(2, 41);
            var seen = new List<(DecisionKind kind, int player)>();
            var bot = new hp55games.MareIgnoto.Rules.Bots.RandomBot(new SeededRandom(1));
            while (session.Pending != null)
            {
                Assert.IsTrue(session.Pending.IsSecret, session.Pending.ToString());
                Assert.IsNotEmpty(session.Pending.Options);
                seen.Add((session.Pending.Kind, session.Pending.Player));
                session.Submit(bot.Choose(session.Pending));
            }

            CollectionAssert.AreEqual(new[]
            {
                (DecisionKind.PlaceStartingCrew, 0), (DecisionKind.KeepMissions, 0), (DecisionKind.GartyaOffer, 0),
                (DecisionKind.PlaceStartingCrew, 1), (DecisionKind.KeepMissions, 1), (DecisionKind.GartyaOffer, 1),
            }, seen.Take(6).ToArray());
            Assert.IsTrue(seen.Skip(6).All(s => s.kind == DecisionKind.ChooseHeading));
            Assert.AreEqual(2, seen.Skip(6).Count());
        }

        [Test]
        public void SetupDecisionsExposeAllTheirLegalOptions()
        {
            GameSession session = TestSupport.Start(2, 51);

            PendingDecision place = session.Pending;
            Assert.AreEqual(DecisionKind.PlaceStartingCrew, place.Kind);
            Assert.AreEqual(Config.startingCrewCards, place.Cards.Count);
            // 2 carte in 3 slot sotto coperta: 3 × 2 modi.
            Assert.AreEqual(6, place.Options.Count);
            Assert.IsTrue(place.Options.Cast<PlaceCrewOption>().All(o => o.Placements.Select(p => p.Slot).Distinct().Count() == 2));
            Assert.IsTrue(place.Options.Cast<PlaceCrewOption>().All(o => o.Placements.All(p => IsBelowSlot(p.Slot))));

            session.Submit(place.Choose(0));
            PendingDecision keep = session.Pending;
            Assert.AreEqual(DecisionKind.KeepMissions, keep.Kind);
            Assert.AreEqual(Config.startingMissionsDrawn, keep.Cards.Count);
            // Sottoinsiemi non vuoti di 3 missioni: 7.
            Assert.AreEqual(7, keep.Options.Count);
            Assert.IsTrue(keep.Options.Cast<KeepMissionsOption>().All(o => o.Kept.Count >= Config.startingMissionsMinKept));

            session.Submit(keep.Choose(0));
            PendingDecision offer = session.Pending;
            Assert.AreEqual(DecisionKind.GartyaOffer, offer.Kind);
            Assert.AreEqual(Config.startingCoins + 1, offer.Options.Count, "da 0 a tutte le monete");
            CollectionAssert.AreEqual(Enumerable.Range(0, Config.startingCoins + 1),
                offer.Options.Cast<OfferOption>().Select(o => o.Amount));
        }

        private static bool IsBelowSlot(int slot) => slot >= Config.slotsAbove && slot < Config.slotsAbove + Config.slotsBelow;

        [Test]
        public void HeadingDecisionOffersTheEightDirectionsAndMustBeConfirmed_R040()
        {
            GameSession session = TestSupport.Start(2, 61);
            var bot = new hp55games.MareIgnoto.Rules.Bots.RandomBot(new SeededRandom(1));
            while (session.Pending.Kind != DecisionKind.ChooseHeading) session.Submit(bot.Choose(session.Pending));

            PendingDecision pending = session.Pending;
            Assert.IsTrue(pending.RequiresConfirmation);
            Assert.IsTrue(pending.IsSecret);
            CollectionAssert.AreEqual(Enum.GetValues(typeof(Heading)).Cast<Heading>(),
                pending.Options.Cast<HeadingOption>().Select(o => o.Heading));
        }

        [Test]
        public void GartyaOfferIsDeductedAndRevealedTogether_R036()
        {
            GameSession session = TestSupport.Start(3, 71);
            List<GameEvent> events = TestSupport.Play(session, TestSupport.WithOffers(0, 4, 10));

            var revealed = events.OfType<OffersRevealedEvent>().Single();
            CollectionAssert.AreEqual(new[] { 0, 4, 10 }, revealed.Amounts);
            Assert.AreEqual(Config.startingCoins, session.State.Player(0).Coins);
            Assert.AreEqual(Config.startingCoins - 4, session.State.Player(1).Coins);
            Assert.AreEqual(0, session.State.Player(2).Coins);
            Assert.AreEqual(14, session.State.Treasure);

            int revealIndex = events.FindIndex(e => e is OffersRevealedEvent);
            int lastMade = events.FindLastIndex(e => e is OfferMadeEvent);
            Assert.Less(lastMade, revealIndex, "tutte le offerte sono fatte prima di rivelarle");
            Assert.IsTrue(events.OfType<CoinsChangedEvent>().Where(e => e.Reason == CoinReason.GartyaOffer)
                .All(e => events.IndexOf(e) > revealIndex), "le monete si tolgono solo alla rivelazione");
        }

        [Test]
        public void ZeroOfferLeavesTheTreasureEmpty()
        {
            GameSession session = TestSupport.Start(2, 72);
            List<GameEvent> events = TestSupport.Play(session, TestSupport.WithOffers(0, 0));
            Assert.AreEqual(0, session.State.Treasure);
            Assert.IsFalse(events.OfType<TreasureChangedEvent>().Any());
        }

        [Test]
        public void WindAndZonesStartAsDefault_R038()
        {
            GameSession session = TestSupport.Start(2, 81);
            List<GameEvent> events = TestSupport.PlayWithBot(session);

            DieRolledEvent roll = events.OfType<DieRolledEvent>().Single(e => e.Reason == DiceReason.InitialWind);
            Assert.That(roll.Value, Is.InRange(1, 8));
            Assert.AreEqual(Heading.N.Rotate(roll.Value), session.State.Wind);
            Assert.AreEqual(session.State.Wind, events.OfType<WindChangedEvent>().Single().Wind);
            Assert.AreEqual(36, session.State.Zones.Count);
            Assert.IsTrue(session.State.Zones.All(z => z == WeatherState.Normal));
        }

        [TestCase(1, Heading.NE)]
        [TestCase(2, Heading.E)]
        [TestCase(4, Heading.S)]
        [TestCase(7, Heading.NO)]
        [TestCase(8, Heading.N)]
        public void InitialWindIsTheD8InClockwiseStepsFromNorth_R038(int roll, Heading expected)
        {
            // Un solo tiro nel setup a 2 senza pareggi d'offerta: quello del vento.
            var random = new ScriptedRandomSource(new[] { roll }, new SeededRandom(82));
            GameSession session = TestSupport.Start(2, 82, null, random);
            TestSupport.Play(session, TestSupport.WithOffers(3, 1));
            Assert.AreEqual(expected, session.State.Wind);
            Assert.AreEqual(0, random.RemainingScripted);
        }

        [Test]
        public void WindIsRandomAcrossSeeds_R038()
        {
            var winds = new HashSet<Heading>();
            for (int seed = 1; seed <= 60; seed++)
            {
                GameSession session = TestSupport.Start(2, seed);
                TestSupport.PlayWithBot(session);
                winds.Add(session.State.Wind);
            }

            Assert.Greater(winds.Count, 3);
        }

        [Test]
        public void Phase1HeadingsAreSecretUntilRevealed_R040_R041()
        {
            GameSession session = TestSupport.Start(2, 91);
            var bot = new hp55games.MareIgnoto.Rules.Bots.RandomBot(new SeededRandom(1));
            while (session.Pending.Kind != DecisionKind.ChooseHeading) session.Submit(bot.Choose(session.Pending));

            int first = session.Pending.Player;
            session.Submit(session.Pending.Choose((int)Heading.E));
            Assert.IsNull(session.State.Player(first).RevealedHeading, "R-041: non rivelata finché non hanno scelto tutti");
            Assert.AreEqual(Heading.E, session.State.ViewFor(first).ChosenHeading, "chi l'ha scelta la vede");

            int second = session.Pending.Player;
            Assert.AreNotEqual(first, second);
            Assert.IsNull(session.State.ViewFor(second).ChosenHeading);
            IReadOnlyList<GameEvent> revealEvents = session.Submit(session.Pending.Choose((int)Heading.SO));

            Assert.IsNull(session.Pending);
            Assert.AreEqual(Heading.E, session.State.Player(first).RevealedHeading);
            Assert.AreEqual(Heading.SO, session.State.Player(second).RevealedHeading);
            var revealed = revealEvents.OfType<HeadingsRevealedEvent>().Single();
            Assert.AreEqual(Heading.E, revealed.Headings[first]);
            Assert.AreEqual(Heading.SO, revealed.Headings[second]);
        }

        [Test]
        public void ADecisionWithASingleLegalOptionIsAppliedByTheEngine()
        {
            // Senza monete l'unica offerta possibile è 0; con 1 sola missione pescata non c'è nulla da scegliere.
            var config = new RulesConfig { startingCoins = 0, startingMissionsDrawn = 1 };
            GameSession session = TestSupport.Start(2, 101, config);

            var kinds = new List<DecisionKind>();
            TestSupport.Play(session, decision =>
            {
                kinds.Add(decision.Kind);
                return decision.Choose(0);
            });

            CollectionAssert.DoesNotContain(kinds, DecisionKind.GartyaOffer);
            CollectionAssert.DoesNotContain(kinds, DecisionKind.KeepMissions);
            Assert.AreEqual(1, session.State.Player(0).MissionsInHandCount);
            Assert.AreEqual(0, session.State.Player(0).Coins);
        }

        [Test]
        public void SingleSlotCrewPlacementIsAppliedByTheEngine()
        {
            // Con 1 crew e 1 solo slot sotto coperta il posizionamento è obbligato; la rotta resta da confermare.
            var config = new RulesConfig { startingCrewCards = 1, slotsBelow = 1 };
            GameSession session = TestSupport.Start(2, 102, config);
            Assert.AreEqual(DecisionKind.KeepMissions, session.Pending.Kind);
            Assert.AreEqual(1, session.State.Player(0).CrewBelowOccupied.Count(o => o));
        }

        [Test]
        public void ADeckThatRunsOutDrawsOnlyWhatIsAvailable_R009()
        {
            var config = new RulesConfig();
            foreach (PirateCardEntry entry in config.pirateCards) entry.copies = 0;
            config.pirateCards.First(e => e.id == PirateCardId.Bordata).copies = 4;
            GameSession session = TestSupport.Start(2, 111, config);
            TestSupport.PlayWithBot(session);

            Assert.AreEqual(Config.startingPirateCards, session.State.Player(0).HandCount);
            Assert.AreEqual(1, session.State.Player(1).HandCount);
            Assert.AreEqual(0, session.State.PirateDeck.DrawCount);
            TestSupport.AssertInvariants(session);
        }

        [Test]
        public void ExhaustedDeckRecyclesItsDiscardPile_R009()
        {
            var config = new RulesConfig();
            var map = GameMap.Create(TestSupport.StandardMap(), config);
            var state = new GameState(config, map);
            state.BuildDecks();
            state.Players.Add(new PlayerState(0, "P1", config.slotsAbove, config.slotsBelow));
            var context = new GameContext(state, config, new SeededRandom(1), GameSetup.ForPlayers(2, 1));

            // Si svuota il mazzo Pirateria mettendo tutte le carte negli scarti.
            while (state.Pirate.DrawCount > 0) state.Pirate.Discard(state.Pirate.TakeTop());
            int discarded = state.PirateDeck.DiscardCount;

            IReadOnlyList<PirateCard> drawn = context.DrawToHand(state.Players[0], 3);

            Assert.AreEqual(3, drawn.Count);
            Assert.AreEqual(discarded - 3, state.PirateDeck.DrawCount);
            Assert.AreEqual(0, state.PirateDeck.DiscardCount);
            var events = context.TakeEvents();
            var shuffle = events.OfType<DeckShuffledEvent>().Single();
            Assert.IsTrue(shuffle.FromDiscard);
            Assert.AreEqual(discarded, shuffle.CardCount);
        }

        [Test]
        public void TheCorsairDeckNeverRecyclesItsDiscardPile_R009()
        {
            GameContext context = BareContext(out GameState state);

            // Restano 2 missioni, il resto è negli scarti: se ne pescano solo 2, senza rimescolare.
            while (state.Corsair.DrawCount > 2) state.Corsair.Discard(state.Corsair.TakeTop());
            int discarded = state.CorsairDeck.DiscardCount;

            IReadOnlyList<Card> drawn = context.DrawToTransit(state.Players[0], DeckKind.Corsair, Config.startingMissionsDrawn);
            Assert.AreEqual(2, drawn.Count);
            Assert.AreEqual(0, state.CorsairDeck.DrawCount);
            Assert.AreEqual(discarded, state.CorsairDeck.DiscardCount, "gli scarti Corsaro restano fuori dal gioco");
            Assert.IsFalse(context.TakeEvents().OfType<DeckShuffledEvent>().Any());

            // Mazzo vuoto: niente da pescare, e l'azione "Missione" non è selezionabile.
            Assert.IsFalse(context.CanDraw(DeckKind.Corsair));
            Assert.IsEmpty(context.DrawToTransit(state.Players[0], DeckKind.Corsair, 1));
            Assert.IsEmpty(context.TakeEvents());
        }

        [Test]
        public void CrewAndPirateCanDrawWhileTheirDiscardIsNotEmpty_R009()
        {
            GameContext context = BareContext(out GameState state);
            while (state.Pirate.DrawCount > 0) state.Pirate.Discard(state.Pirate.TakeTop());
            while (state.Crew.DrawCount > 0) state.Crew.Discard(state.Crew.TakeTop());
            Assert.IsTrue(context.CanDraw(DeckKind.Pirate));
            Assert.IsTrue(context.CanDraw(DeckKind.Crew));
            Assert.IsTrue(context.CanDraw(DeckKind.Corsair));
        }

        private static GameContext BareContext(out GameState state)
        {
            var map = GameMap.Create(TestSupport.StandardMap(), Config);
            state = new GameState(Config, map);
            state.BuildDecks();
            state.Players.Add(new PlayerState(0, "P1", Config.slotsAbove, Config.slotsBelow));
            return new GameContext(state, Config, new SeededRandom(1), GameSetup.ForPlayers(2, 1));
        }

        [Test]
        public void StartRejectsInvalidInput()
        {
            Assert.Throws<ArgumentException>(() => TestSupport.Start(1, 1), "meno di 2 giocatori");
            Assert.Throws<ArgumentException>(() => GameSession.Start(GameSetup.ForPlayers(9, 1), Config, TestSupport.StandardMap(), new SeededRandom(1)),
                "più di 8 giocatori");

            var duplicateSeats = new GameSetup(new[] { new PlayerSetup(0, "A"), new PlayerSetup(0, "B") }, 1);
            Assert.Throws<ArgumentException>(() => GameSession.Start(duplicateSeats, Config, TestSupport.StandardMap(), new SeededRandom(1)));

            var gappedSeats = new GameSetup(new[] { new PlayerSetup(0, "A"), new PlayerSetup(2, "B") }, 1);
            Assert.Throws<ArgumentException>(() => GameSession.Start(gappedSeats, Config, TestSupport.StandardMap(), new SeededRandom(1)));

            MapLayout brokenMap = TestSupport.StandardMap();
            brokenMap.sacredIslandCells.Clear();
            Assert.Throws<ArgumentException>(() => GameSession.Start(GameSetup.ForPlayers(2, 1), Config, brokenMap, new SeededRandom(1)));

            // Senza preset per 9 la mappa non è valida con maxPlayers = 9.
            var ninePlayers = new RulesConfig { maxPlayers = 9 };
            Assert.Throws<ArgumentException>(() => GameSession.Start(GameSetup.ForPlayers(9, 1), ninePlayers, TestSupport.StandardMap(), new SeededRandom(1)));

            var brokenConfig = new RulesConfig { startingCoins = -1 };
            Assert.Throws<ArgumentException>(() => TestSupport.Start(2, 1, brokenConfig));
        }

        [Test]
        public void SeatsDecidePlayerIdsAndStartingPoints_R031()
        {
            var players = new[] { new PlayerSetup(1, "Seconda"), new PlayerSetup(0, "Prima") };
            GameSession session = GameSession.Start(new GameSetup(players, 5), Config, TestSupport.StandardMap());
            Assert.AreEqual("Prima", session.State.Player(0).Name);
            Assert.AreEqual("Seconda", session.State.Player(1).Name);
            Assert.AreEqual(new Coord(0, 0), session.State.Player(0).Position);
            Assert.AreEqual(new Coord(19, 19), session.State.Player(1).Position);
        }

        [Test]
        public void StartingPointsComeFromThePresetForThePlayerCount_R031()
        {
            GameSession session = TestSupport.Start(3, 6);
            Assert.AreEqual(new Coord(0, 0), session.State.Player(0).Position);
            Assert.AreEqual(new Coord(19, 0), session.State.Player(1).Position);
            Assert.AreEqual(new Coord(9, 19), session.State.Player(2).Position);
        }

        [Test]
        public void TutorialOptionsPutCardsOnTopAndFixWindAndZones()
        {
            GameSession session = TestSupport.Start(2, 121, null, null, setup =>
            {
                setup.Tutorial = new TutorialOptions
                {
                    CrewDeckTop = new[]
                    {
                        new CrewCardSpec(CrewCardId.Timoniere, CrewSuit.Spades),
                        new CrewCardSpec(CrewCardId.Navigatore),
                        new CrewCardSpec(CrewCardId.Jolly),
                    },
                    PirateDeckTop = new[] { PirateCardId.Bordata, PirateCardId.VentoInPoppa },
                    CorsairDeckTop = new[] { MissionId.Avido, MissionId.Barbanera },
                    InitialWind = Heading.SE,
                    InitialZones = new[] { new ZoneSetup(7, WeatherState.Storm), new ZoneSetup(8, WeatherState.RoughSea) },
                };
            });

            // Le crew si pescano nell'ordine dei posti: p0 prende le prime due, p1 la terza.
            var crewDraws = session.InitialEvents.OfType<CardsDrawnEvent>().Where(e => e.Deck == DeckKind.Crew).ToList();
            var p0 = crewDraws.First(e => e.Player == 0).Cards.Cast<CrewCard>().ToList();
            Assert.AreEqual(CrewCardId.Timoniere, p0[0].Kind);
            Assert.AreEqual(CrewSuit.Spades, p0[0].Suit);
            Assert.AreEqual(CrewCardId.Navigatore, p0[1].Kind);
            Assert.AreEqual(CrewCardId.Jolly, ((CrewCard)crewDraws.First(e => e.Player == 1).Cards[0]).Kind);

            var pirateP0 = session.InitialEvents.OfType<CardsDrawnEvent>().First(e => e.Deck == DeckKind.Pirate && e.Player == 0);
            Assert.AreEqual(PirateCardId.Bordata, ((PirateCard)pirateP0.Cards[0]).Id);
            Assert.AreEqual(PirateCardId.VentoInPoppa, ((PirateCard)pirateP0.Cards[1]).Id);

            var missionsP0 = session.InitialEvents.OfType<CardsDrawnEvent>().First(e => e.Deck == DeckKind.Corsair && e.Player == 0);
            Assert.AreEqual(MissionId.Avido, ((MissionCard)missionsP0.Cards[0]).Id);
            Assert.AreEqual(MissionId.Barbanera, ((MissionCard)missionsP0.Cards[1]).Id);

            TestSupport.PlayWithBot(session);
            Assert.AreEqual(Heading.SE, session.State.Wind);
            Assert.AreEqual(WeatherState.Storm, session.State.Zones[7]);
            Assert.AreEqual(WeatherState.RoughSea, session.State.Zones[8]);
            Assert.AreEqual(WeatherState.Normal, session.State.Zones[0]);
        }

        [Test]
        public void TutorialDeckTopBeyondAvailableCopiesIsRejected()
        {
            Assert.Throws<ArgumentException>(() => TestSupport.Start(2, 1, null, null, setup =>
                setup.Tutorial = new TutorialOptions
                {
                    CrewDeckTop = new[]
                    {
                        new CrewCardSpec(CrewCardId.Jolly), new CrewCardSpec(CrewCardId.Jolly), new CrewCardSpec(CrewCardId.Jolly),
                    },
                }));
        }

        [Test]
        public void TutorialZoneOutOfRangeIsRejected()
        {
            Assert.Throws<ArgumentException>(() => TestSupport.Start(2, 1, null, null, setup =>
                setup.Tutorial = new TutorialOptions { InitialZones = new[] { new ZoneSetup(99, WeatherState.Storm) } }));
        }
    }
}
