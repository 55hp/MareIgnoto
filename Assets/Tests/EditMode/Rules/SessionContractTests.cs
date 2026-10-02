using System;
using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Bots;
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
    public class SessionContractTests
    {
        [Test]
        public void InvalidAnswersAreRejectedAndTheStateDoesNotChange()
        {
            GameSession session = TestSupport.Start(2, 1);
            PendingDecision pending = session.Pending;
            string before = StateFingerprint(session);

            Assert.Throws<InvalidDecisionException>(() => session.Submit(null));
            Assert.Throws<InvalidDecisionException>(() => session.Submit(new DecisionAnswer(pending.Id, -1)));
            Assert.Throws<InvalidDecisionException>(() => session.Submit(new DecisionAnswer(pending.Id, pending.Options.Count)));
            Assert.Throws<InvalidDecisionException>(() => session.Submit(new DecisionAnswer(pending.Id + 1, 0)));

            Assert.AreSame(pending, session.Pending);
            Assert.AreEqual(before, StateFingerprint(session));
            Assert.DoesNotThrow(() => session.Submit(pending.Choose(0)));
        }

        [Test]
        public void ARepeatedAnswerToAnAlreadyClosedDecisionIsRejected()
        {
            GameSession session = TestSupport.Start(2, 2);
            DecisionAnswer answer = session.Pending.Choose(0);
            session.Submit(answer);
            Assert.Throws<InvalidDecisionException>(() => session.Submit(answer));
        }

        [Test]
        public void SubmittingWithNoPendingDecisionIsRejected()
        {
            GameSession session = TestSupport.Start(2, 3);
            TestSupport.PlayWithBot(session);
            Assert.IsNull(session.Pending);
            Assert.Throws<InvalidDecisionException>(() => session.Submit(new DecisionAnswer(1, 0)));
        }

        [Test]
        public void ChooseByOptionAndByIndexAreEquivalent()
        {
            GameSession session = TestSupport.Start(2, 4);
            PendingDecision pending = session.Pending;
            DecisionAnswer byOption = pending.Choose(pending.Options[2]);
            Assert.AreEqual(2, byOption.OptionIndex);
            Assert.AreEqual(pending.Id, byOption.DecisionId);
            Assert.Throws<ArgumentException>(() => pending.Choose(new OfferOption(1)));
        }

        [Test]
        public void DecisionIdsIncrease()
        {
            GameSession session = TestSupport.Start(2, 5);
            var bot = new RandomBot(new SeededRandom(5));
            int last = 0;
            while (session.Pending != null)
            {
                Assert.Greater(session.Pending.Id, last);
                last = session.Pending.Id;
                session.Submit(bot.Choose(session.Pending));
            }
        }

        [Test]
        public void SameSeedAndAnswersGiveIdenticalEvents_Determinism()
        {
            foreach (int players in new[] { 2, 4, 8 })
            {
                string first = TestSupport.Log(TestSupport.PlayWithBot(TestSupport.Start(players, 77), 5));
                string second = TestSupport.Log(TestSupport.PlayWithBot(TestSupport.Start(players, 77), 5));
                Assert.AreEqual(first, second, players + " giocatori");
                Assert.IsNotEmpty(first);
            }
        }

        [Test]
        public void DifferentSeedsGiveDifferentGames_Determinism()
        {
            string a = TestSupport.Log(TestSupport.PlayWithBot(TestSupport.Start(4, 1), 5));
            string b = TestSupport.Log(TestSupport.PlayWithBot(TestSupport.Start(4, 2), 5));
            Assert.AreNotEqual(a, b);
        }

        [Test]
        public void DifferentAnswersGiveDifferentGamesFromTheSameSeed()
        {
            string a = TestSupport.Log(TestSupport.PlayWithBot(TestSupport.Start(4, 1), 5));
            string b = TestSupport.Log(TestSupport.PlayWithBot(TestSupport.Start(4, 1), 6));
            Assert.AreNotEqual(a, b);
        }

        [Test]
        public void StartWithoutExplicitSourceUsesTheSetupSeed()
        {
            GameSetup setup = GameSetup.ForPlayers(3, 123);
            var viaDefault = GameSession.Start(setup, new RulesConfig(), TestSupport.StandardMap());
            var viaExplicit = GameSession.Start(setup, new RulesConfig(), TestSupport.StandardMap(), new SeededRandom(123));
            Assert.AreEqual(TestSupport.Log(viaDefault.InitialEvents), TestSupport.Log(viaExplicit.InitialEvents));
        }

        [Test]
        public void RandomBotCanAnswerEverySetupDecisionForAnyPlayerCountAndSeed()
        {
            for (int players = 2; players <= 8; players++)
            {
                for (int seed = 1; seed <= 15; seed++)
                {
                    GameSession session = TestSupport.Start(players, seed);
                    List<GameEvent> events = TestSupport.PlayWithBot(session, seed);

                    Assert.IsNull(session.Pending);
                    Assert.AreEqual(1, events.OfType<HeadingsRevealedEvent>().Count());
                    Assert.AreEqual(players, session.State.Players.Count(p => p.RevealedHeading.HasValue));
                }
            }
        }

        [Test]
        public void RandomBotRejectsNullDecision()
        {
            Assert.Throws<ArgumentNullException>(() => new RandomBot(new SeededRandom(1)).Choose(null));
        }

        // ---- Visibilità (04_motore.md §3, §5) ----

        [Test]
        public void PrivateEventsReachOnlyTheirOwnerAndRedactForTheOthers()
        {
            GameSession session = TestSupport.Start(3, 6);
            var drawn = session.InitialEvents.OfType<CardsDrawnEvent>().First(e => e.Deck == DeckKind.Crew && e.Player == 1);

            Assert.AreEqual(EventVisibility.Private, drawn.Visibility);
            Assert.AreEqual(1, drawn.VisibleTo);
            Assert.IsTrue(drawn.IsVisibleTo(1));
            Assert.IsFalse(drawn.IsVisibleTo(0));

            GameEvent forOwner = drawn.ViewFor(1);
            Assert.AreSame(drawn, forOwner);

            var forOther = (CardsDrawnEvent)drawn.ViewFor(0);
            Assert.AreNotSame(drawn, forOther);
            Assert.AreEqual(EventVisibility.Public, forOther.Visibility);
            Assert.IsTrue(forOther.IsRedacted);
            Assert.AreEqual(drawn.Count, forOther.Count, "gli altri sanno quante carte");
            Assert.IsEmpty(forOther.Cards, "ma non quali");
            Assert.AreEqual(drawn.Player, forOther.Player);
        }

        [Test]
        public void EventsAreRedactedAndNeverLeakSecretsToOtherViewers()
        {
            GameSession session = TestSupport.Start(3, 7);
            List<GameEvent> events = TestSupport.PlayWithBot(session, 7);

            for (int viewer = 0; viewer < 3; viewer++)
            {
                foreach (GameEvent gameEvent in events)
                {
                    GameEvent seen = gameEvent.ViewFor(viewer);
                    Assert.IsTrue(seen.IsVisibleTo(viewer));

                    switch (seen)
                    {
                        case CardsDrawnEvent d when d.Player != viewer:
                            Assert.IsEmpty(d.Cards);
                            break;
                        case CardsDiscardedEvent d when d.Player != viewer:
                            Assert.IsEmpty(d.Cards);
                            break;
                        case CrewSlotChangedEvent s when !s.Above && s.Player != viewer:
                            Assert.IsNull(s.Card);
                            break;
                        case OfferMadeEvent o when o.Player != viewer:
                            Assert.AreEqual(0, o.Amount);
                            Assert.IsTrue(o.IsRedacted);
                            break;
                        case HeadingChosenEvent h when h.Player != viewer:
                            Assert.IsFalse(h.Heading.HasValue);
                            break;
                    }
                }
            }
        }

        [Test]
        public void BelowDeckCrewPlacementIsPrivateToTheOwner_R010()
        {
            GameSession session = TestSupport.Start(2, 8);
            List<GameEvent> events = TestSupport.PlayWithBot(session, 8);
            var placements = events.OfType<CrewSlotChangedEvent>().Where(e => e.Player == 0).ToList();

            Assert.AreEqual(new RulesConfig().startingCrewCards, placements.Count);
            Assert.IsTrue(placements.All(e => !e.Above && e.Visibility == EventVisibility.Private));

            var asSeenByOther = (CrewSlotChangedEvent)placements[0].ViewFor(1);
            Assert.IsTrue(asSeenByOther.Occupied);
            Assert.IsNull(asSeenByOther.Card);
        }

        [Test]
        public void PublicEventsAreVisibleToEveryone()
        {
            GameSession session = TestSupport.Start(2, 9);
            var shuffled = session.InitialEvents.OfType<DeckShuffledEvent>().First();
            Assert.AreEqual(EventVisibility.Public, shuffled.Visibility);
            Assert.AreEqual(-1, shuffled.VisibleTo);
            Assert.IsTrue(shuffled.IsVisibleTo(0));
            Assert.AreSame(shuffled, shuffled.ViewFor(1));
        }

        [Test]
        public void ViewForExposesOnlyTheViewersSecrets()
        {
            GameSession session = TestSupport.Start(2, 10);
            TestSupport.PlayWithBot(session, 10);

            PlayerView view0 = session.State.ViewFor(0);
            PlayerView view1 = session.State.ViewFor(1);

            Assert.AreEqual(0, view0.Viewer);
            Assert.AreEqual(new RulesConfig().startingPirateCards, view0.Hand.Count);
            Assert.IsTrue(view0.Hand.All(c => c != null));
            Assert.IsTrue(view0.CrewBelow.Any(c => c != null));
            Assert.AreNotEqual(view0.Hand[0].Uid, view1.Hand[0].Uid);

            // Dello stato pubblico non si vedono carte altrui: solo conteggi.
            IReadOnlyPlayerState other = view0.State.Player(1);
            Assert.AreEqual(view1.Hand.Count, other.HandCount);
            Assert.AreEqual(view1.Missions.Count, other.MissionsInHandCount);
            Assert.AreEqual(view1.CrewBelow.Count(c => c != null), other.CrewBelowOccupied.Count(o => o));
            Assert.IsEmpty(other.CompletedMissions);
        }

        [Test]
        public void ViewIsASnapshot()
        {
            GameSession session = TestSupport.Start(2, 12);
            PlayerView before = session.State.ViewFor(0);
            int handBefore = before.Hand.Count;
            TestSupport.PlayWithBot(session, 12);
            Assert.AreEqual(handBefore, before.Hand.Count);
        }

        [Test]
        public void ViewForUnknownPlayerThrows()
        {
            GameSession session = TestSupport.Start(2, 13);
            Assert.Throws<ArgumentOutOfRangeException>(() => session.State.ViewFor(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => session.State.ViewFor(-1));
        }

        // ---- Ordine del round 1 (R-037) ----

        [Test]
        public void Round1OrderIsByDescendingOffer_R037()
        {
            GameSession session = TestSupport.Start(4, 14);
            TestSupport.Play(session, TestSupport.WithOffers(2, 9, 0, 5));
            CollectionAssert.AreEqual(new[] { 1, 3, 0, 2 }, session.State.TurnOrder);
        }

        [Test]
        public void TiedOffersAreBrokenByTheHigherDieRoll_R037()
        {
            // p0 e p1 offrono 5, p2 offre 7: p2 primo; tra p0 (3) e p1 (6) precede p1.
            var random = new ScriptedRandomSource(new[] { 3, 6 }, new SeededRandom(1));
            GameSession session = TestSupport.Start(3, 15, null, random);
            List<GameEvent> events = TestSupport.Play(session, TestSupport.WithOffers(5, 5, 7));

            CollectionAssert.AreEqual(new[] { 2, 1, 0 }, session.State.TurnOrder);
            var rolls = TiebreakRolls(events);
            Assert.AreEqual(2, rolls.Count);
            CollectionAssert.AreEqual(new[] { 0, 1 }, rolls.Select(r => r.Player));
            CollectionAssert.AreEqual(new[] { 3, 6 }, rolls.Select(r => r.Value));
        }

        [Test]
        public void TiedDieRollsAreRerolledAmongTheTied_R037()
        {
            // p0 e p1 pareggiano (4, 4) e ritirano (2, 7): precede p1.
            var random = new ScriptedRandomSource(new[] { 4, 4, 2, 7 }, new SeededRandom(1));
            GameSession session = TestSupport.Start(3, 16, null, random);
            List<GameEvent> events = TestSupport.Play(session, TestSupport.WithOffers(5, 5, 7));

            CollectionAssert.AreEqual(new[] { 2, 1, 0 }, session.State.TurnOrder);
            CollectionAssert.AreEqual(new[] { 4, 4, 2, 7 }, TiebreakRolls(events).Select(r => r.Value));
            CollectionAssert.AreEqual(new[] { 0, 1, 0, 1 }, TiebreakRolls(events).Select(r => r.Player));
        }

        [Test]
        public void AllPlayersTiedWithPartialTiesInTheRolls_R037()
        {
            // 4 giocatori a 0: tiri 5,5,3,5 → {p0,p1,p3} sopra p2; poi 1,8,8 → {p1,p3} sopra p0; poi 2,6 → p3 sopra p1.
            var random = new ScriptedRandomSource(new[] { 5, 5, 3, 5, 1, 8, 8, 2, 6 }, new SeededRandom(1));
            GameSession session = TestSupport.Start(4, 17, null, random);
            List<GameEvent> events = TestSupport.Play(session, TestSupport.WithOffers(0, 0, 0, 0));

            CollectionAssert.AreEqual(new[] { 3, 1, 0, 2 }, session.State.TurnOrder);
            Assert.AreEqual(9, TiebreakRolls(events).Count);
            Assert.AreEqual(0, ((ScriptedRandomSource)random).RemainingScripted);
        }

        [Test]
        public void NoTiesMeansNoTiebreakDiceAtSetup_R037()
        {
            // Senza pareggi l'unico tiro del setup è quello del vento (R-038).
            GameSession session = TestSupport.Start(3, 18);
            List<GameEvent> events = TestSupport.Play(session, TestSupport.WithOffers(1, 2, 3));
            Assert.IsEmpty(TiebreakRolls(events));
            Assert.AreEqual(DiceReason.InitialWind, events.OfType<DieRolledEvent>().Single().Reason);
        }

        private static List<DieRolledEvent> TiebreakRolls(IEnumerable<GameEvent> events) =>
            events.OfType<DieRolledEvent>().Where(r => r.Reason == DiceReason.TurnOrderTiebreak).ToList();

        [Test]
        public void TurnOrderIsAPermutationForAnyOffers()
        {
            for (int seed = 1; seed <= 40; seed++)
            {
                GameSession session = TestSupport.Start(8, seed);
                TestSupport.PlayWithBot(session, seed);
                CollectionAssert.AreEquivalent(Enumerable.Range(0, 8), session.State.TurnOrder);
            }
        }

        [Test]
        public void TurnOrderEventMatchesTheState()
        {
            GameSession session = TestSupport.Start(4, 19);
            List<GameEvent> events = TestSupport.PlayWithBot(session, 19);
            var set = events.OfType<TurnOrderSetEvent>().Single();
            CollectionAssert.AreEqual(session.State.TurnOrder, set.Order);
            Assert.AreEqual(1, set.Round);
        }

        private static string StateFingerprint(GameSession session)
        {
            IReadOnlyGameState s = session.State;
            return string.Join("|", s.Round, s.Phase, s.Treasure, s.Wind, s.CrewDeck.DrawCount, s.PirateDeck.DrawCount,
                s.CorsairDeck.DrawCount, string.Join(",", s.Players.Select(p => p.Coins + ":" + p.HandCount)),
                session.Pending.Id);
        }
    }
}
