using System;
using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Decisions;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Map;
using NUnit.Framework;
using static hp55games.MareIgnoto.Rules.Tests.RoundScenario;

namespace hp55games.MareIgnoto.Rules.Tests
{
    /// <summary>
    /// Turno di Fase 2 (02_regole.md §4, §7.1) ed effetti crew del turno. In porto: p0 parte da (3,4) verso E e attracca
    /// sull'isola 0 (4,4); p1 resta ferma in (16,16). Monete fissate a 10.
    /// </summary>
    public class PortAndTurnTests
    {
        private static readonly Heading[] ToPort = { Heading.E, Heading.S };

        private static RoundScenario InPort()
        {
            RoundScenario s = Create(2).At(0, 3, 4).At(1, 16, 16).Order(0, 1);
            s.P(0).Coins = 10;
            s.P(1).Coins = 7;
            return s;
        }

        private static RoundScenario AtSea()
        {
            RoundScenario s = Create(2).At(0, 5, 7).At(1, 16, 16).Order(0, 1);
            s.P(0).Coins = 10;
            s.P(1).Coins = 7;
            return s;
        }

        private static Func<PendingDecision, DecisionOption, bool> Port(PortAction action) =>
            Opt<PortActionOption>(0, o => o.Action == action);

        private static List<PortActionOption> PortOptions(RoundScenario s) =>
            s.DecisionsOf(DecisionKind.PortAction, 0)[0].Options.Cast<PortActionOption>().ToList();

        // ---- Porto ----

        [Test]
        public void InPortThePlayerChoosesOnePortActionAndNothingElse_R054()
        {
            RoundScenario s = InPort();
            s.Crew(0, s.AboveSlot(0), CrewCardId.Mozzo);
            s.Hand(0, PirateCardId.PescaFortunata, PirateCardId.Bordata);
            s.Random.Enqueue(5);
            List<GameEvent> events = s.PlayRound(ToPort);

            Assert.AreEqual(new Coord(4, 4), s.P(0).Position);
            Assert.AreEqual(TurnKind.Port, events.OfType<TurnKindEvent>().Single(e => e.Player == 0).Kind);
            Assert.AreEqual(1, s.DecisionsOf(DecisionKind.PortAction, 0).Count);
            Assert.IsTrue(s.DecisionsOf(DecisionKind.PortAction, 0)[0].IsSecret);
            Assert.IsFalse(s.DecisionsOf(DecisionKind.SeaAction, 0).Any(), "in porto niente swap, attacchi né carte");
            Assert.AreEqual(2, s.P(0).Hand.Count);
        }

        [Test]
        public void PlunderGivesD8Coins_R090()
        {
            RoundScenario s = InPort();
            s.Random.Enqueue(6);
            List<GameEvent> events = s.PlayRound(ToPort, Pick(Port(PortAction.Plunder)));

            Assert.AreEqual(16, s.P(0).Coins);
            Assert.AreEqual(DiceReason.Plunder, events.OfType<DieRolledEvent>().Single(e => e.Player == 0).Reason);
            Assert.AreEqual(PortAction.Plunder, events.OfType<PortActionEvent>().Single().Action);
        }

        [Test]
        public void LeisureKeepsTheShipInPortForTheNextRound_R091_R045()
        {
            RoundScenario s = InPort();
            s.PlayRound(ToPort, Pick(Port(PortAction.Leisure)));
            Assert.AreEqual(10 - s.Config.leisureCost, s.P(0).Coins);
            Assert.AreEqual(2, s.P(0).LeisureRound);

            List<GameEvent> round2 = s.PlayRound(new[] { Heading.N, Heading.S }, Pick(Port(PortAction.Plunder)));
            Assert.IsFalse(s.Decisions.Any(d => d.Kind == DecisionKind.ChooseHeading && d.Player == 0 && s.Decisions.IndexOf(d) > 0
                                               && round2.OfType<RoundStartedEvent>().Any()), "nel round 2 p0 non sceglie la rotta");
            Assert.IsFalse(round2.OfType<ShipMovedEvent>().Any(e => e.Player == 0));
            Assert.AreEqual(new Coord(4, 4), s.P(0).Position);
            Assert.AreEqual(TurnKind.Port, round2.OfType<TurnKindEvent>().Single(e => e.Player == 0).Kind, "gioca di nuovo un turno di porto");
        }

        [TestCase(PortAction.RecruitOne)]
        [TestCase(PortAction.RecruitTwo)]
        public void RecruitmentDrawsFourAndKeepsOneOrTwo_R092_R020(PortAction action)
        {
            RoundScenario s = InPort();
            s.P(0).Coins = 20;
            int discardBefore = s.State.Crew.DiscardCount;
            List<GameEvent> events = s.PlayRound(ToPort, Pick(Port(action)));

            int keep = action == PortAction.RecruitOne ? s.Config.recruitKeepOne : s.Config.recruitKeepTwo;
            int cost = action == PortAction.RecruitOne ? s.Config.recruitCostKeepOne : s.Config.recruitCostKeepTwo;
            Assert.AreEqual(20 - cost, s.P(0).Coins, "il costo esce dal gioco");
            PendingDecision choose = s.DecisionsOf(DecisionKind.RecruitKeep, 0).Single();
            Assert.AreEqual(s.Config.recruitDrawCount, choose.Cards.Count);
            Assert.IsTrue(choose.Options.Cast<KeepCrewOption>().All(o => o.Kept.Count == keep));
            Assert.AreEqual(keep, s.P(0).Crew.All().Count());
            Assert.AreEqual(keep, events.OfType<CrewReceivedEvent>().Count(e => e.Outcome == ReceiveOutcome.Placed));
            Assert.AreEqual(discardBefore + s.Config.recruitDrawCount - keep, s.State.Crew.DiscardCount);
            Assert.IsEmpty(s.P(0).InTransit);
        }

        [Test]
        public void ReceivingWithAFullShipReplacesACardOrDiscardsTheNewOne_R020_R021()
        {
            RoundScenario s = InPort();
            CrewCard[] old =
            {
                s.Crew(0, 0, CrewCardId.Mozzo), s.Crew(0, 1, CrewCardId.Cuoco), s.Crew(0, 2, CrewCardId.Vedetta),
                s.Crew(0, 3, CrewCardId.Medico), s.Crew(0, 4, CrewCardId.Navigatore),
            };
            List<GameEvent> events = s.PlayRound(ToPort, Pick(Port(PortAction.RecruitOne),
                Opt<ReceiveCrewOption>(0, o => o.Outcome == ReceiveOutcome.Replaced && o.Slot == 0)));

            PendingDecision receive = s.DecisionsOf(DecisionKind.ReceiveCrew, 0).Single();
            Assert.AreEqual(s.Config.ShipSlotCount + 1, receive.Options.Count, "sostituisci uno dei 5 slot, oppure scarta");
            Assert.IsTrue(receive.Options.Cast<ReceiveCrewOption>().Any(o => o.Outcome == ReceiveOutcome.Discarded));
            Assert.AreNotSame(old[0], s.P(0).Crew[0]);
            var lost = events.OfType<CrewLostEvent>().Single();
            Assert.AreEqual(CrewLossCause.Replaced, lost.Cause, "la sostituzione è una perdita (R-021)");
            Assert.AreSame(old[0], lost.Card);
        }

        [Test]
        public void TheReceivedCardCanBeDiscardedDirectly_R020()
        {
            RoundScenario s = InPort();
            var kinds = new[] { CrewCardId.Mozzo, CrewCardId.Cuoco, CrewCardId.Vedetta, CrewCardId.Medico, CrewCardId.Navigatore };
            CrewCard[] before = kinds.Select((kind, slot) => s.Crew(0, slot, kind)).ToArray();
            List<GameEvent> events = s.PlayRound(ToPort, Pick(Port(PortAction.RecruitOne),
                Opt<ReceiveCrewOption>(0, o => o.Outcome == ReceiveOutcome.Discarded)));

            Assert.AreEqual(ReceiveOutcome.Discarded, events.OfType<CrewReceivedEvent>().Single().Outcome);
            CollectionAssert.AreEqual(before, Enumerable.Range(0, s.Config.ShipSlotCount).Select(slot => s.P(0).Crew[slot]));
            Assert.IsFalse(events.OfType<CrewLostEvent>().Any());
        }

        [Test]
        public void MissionDrawsThreeAndKeepsAtLeastOne_R093()
        {
            RoundScenario s = InPort();
            int before = s.P(0).Missions.Count;
            s.PlayRound(ToPort, Pick(Port(PortAction.Mission)));

            PendingDecision keep = s.DecisionsOf(DecisionKind.KeepMissions, 0).Single();
            Assert.AreEqual(s.Config.portMissionDrawCount, keep.Cards.Count);
            Assert.IsTrue(keep.Options.Cast<KeepMissionsOption>().All(o => o.Kept.Count >= s.Config.portMissionMinKept));
            Assert.Greater(s.P(0).Missions.Count, before);
        }

        [Test]
        public void MissionIsNotSelectableWithAnEmptyCorsairDeck_R009_R093()
        {
            RoundScenario s = InPort();
            while (s.State.Corsair.DrawCount > 0) s.State.Corsair.Discard(s.State.Corsair.TakeTop());
            s.PlayRound(ToPort);
            Assert.IsFalse(PortOptions(s).Any(o => o.Action == PortAction.Mission));
        }

        [Test]
        public void CommerceSellsAnyCrewForItsCommerceValue_R094_R014_R021()
        {
            RoundScenario s = InPort();
            s.Crew(0, s.AboveSlot(0), CrewCardId.Culverin);
            s.Crew(0, s.AboveSlot(1), CrewCardId.Jolly);
            s.Crew(0, s.BelowSlot(0), CrewCardId.Cuoco);
            List<GameEvent> events = s.PlayRound(ToPort, Pick(Opt<PortActionOption>(0, o => o.Action == PortAction.Commerce && o.Slot == 0)));

            var values = PortOptions(s).Where(o => o.Action == PortAction.Commerce).ToDictionary(o => o.Slot, o => o.Value);
            Assert.AreEqual(s.Config.courtCommerceValue, values[s.AboveSlot(0)]);
            Assert.AreEqual(s.Config.jokerCommerceValue, values[s.AboveSlot(1)]);
            Assert.AreEqual(4, values[s.BelowSlot(0)]);

            Assert.AreEqual(10 + s.Config.courtCommerceValue, s.P(0).Coins);
            Assert.IsNull(s.P(0).Crew[s.AboveSlot(0)]);
            Assert.AreEqual(1, events.OfType<CrewSoldEvent>().Count());
            Assert.IsFalse(events.OfType<CrewLostEvent>().Any(), "la vendita non è una perdita");
        }

        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        public void TheCookLowersEveryPaidPortActionDownToZero_R095_R013_R015(int cooks, int discount)
        {
            RoundScenario s = InPort();
            s.P(0).Coins = 20;
            if (cooks >= 1) s.Crew(0, s.AboveSlot(0), CrewCardId.Cuoco);
            if (cooks >= 2) s.Crew(0, s.AboveSlot(1), CrewCardId.Jolly);
            s.PlayRound(ToPort);

            List<PortActionOption> options = PortOptions(s);
            Assert.AreEqual(Math.Max(0, s.Config.leisureCost - discount), options.Single(o => o.Action == PortAction.Leisure).Cost);
            Assert.AreEqual(Math.Max(0, s.Config.recruitCostKeepOne - discount), options.Single(o => o.Action == PortAction.RecruitOne).Cost);
            Assert.AreEqual(Math.Max(0, s.Config.recruitCostKeepTwo - discount), options.Single(o => o.Action == PortAction.RecruitTwo).Cost);
            Assert.AreEqual(0, options.Single(o => o.Action == PortAction.Plunder).Cost);
        }

        [Test]
        public void TwoCooksAreCumulative_R013()
        {
            RoundScenario s = InPort();
            s.Crew(0, s.AboveSlot(0), CrewCardId.Cuoco);
            s.Crew(0, s.AboveSlot(1), CrewCardId.Cuoco);
            s.PlayRound(ToPort);
            Assert.AreEqual(s.Config.leisureCost - 2 * s.Config.cookCostDiscount, PortOptions(s).Single(o => o.Action == PortAction.Leisure).Cost);
        }

        [Test]
        public void PaidActionsNeedTheCoinsForTheEffectiveCost_R096()
        {
            RoundScenario s = InPort();
            s.P(0).Coins = s.Config.recruitCostKeepOne - 1;
            s.PlayRound(ToPort);

            List<PortAction> actions = PortOptions(s).Select(o => o.Action).ToList();
            CollectionAssert.DoesNotContain(actions, PortAction.RecruitOne);
            CollectionAssert.DoesNotContain(actions, PortAction.RecruitTwo);
            CollectionAssert.Contains(actions, PortAction.Leisure);
            CollectionAssert.Contains(actions, PortAction.Plunder);
        }

        [Test]
        public void OnTheSacredIslandThereIsNoPortAction_R055()
        {
            RoundScenario s = Create(2).At(0, 8, 9).At(1, 16, 16).Order(0, 1);
            List<GameEvent> events = s.PlayRound(new[] { Heading.E, Heading.S });

            Assert.AreEqual(TurnSkipReason.SacredIsland, events.OfType<TurnSkippedEvent>().Single().Reason);
            Assert.IsFalse(s.DecisionsOf(DecisionKind.PortAction, 0).Any());
        }

        // ---- Mare ----

        [Test]
        public void DrawingTwoCardsEndsTheTurn_R056()
        {
            RoundScenario s = AtSea();
            s.Hand(0, PirateCardId.PescaFortunata);
            List<GameEvent> events = s.PlayTurns(Pick(Opt<DrawCardsOption>(0)));

            Assert.AreEqual(1 + s.Config.seaDrawCount, s.P(0).Hand.Count);
            Assert.AreEqual(1, s.DecisionsOf(DecisionKind.SeaAction, 0).Count, "il turno termina");
            Assert.IsTrue(s.DecisionsOf(DecisionKind.SeaAction, 0)[0].IsSecret);
        }

        [Test]
        public void DrawingIsOnlyAnAlternativeToTheOtherActions_R056()
        {
            RoundScenario s = AtSea();
            s.Crew(0, s.AboveSlot(0), CrewCardId.Mozzo);
            s.Hand(0, PirateCardId.PescaFortunata); // così dopo lo swap resta una scelta vera
            s.PlayTurns(Pick(Opt<SwapOption>(0, o => o.From == 0 && o.To == s.BelowSlot(0))));

            List<PendingDecision> turn = s.DecisionsOf(DecisionKind.SeaAction, 0);
            Assert.IsTrue(turn[0].Options.OfType<DrawCardsOption>().Any());
            Assert.IsFalse(turn[1].Options.OfType<DrawCardsOption>().Any(), "dopo uno swap non si pesca");
            Assert.IsFalse(turn[1].Options.OfType<SwapOption>().Any(), "uno swap per turno");
        }

        [Test]
        public void SwapMovesToAnyEmptySlotOrExchangesTwoCards_R057()
        {
            RoundScenario s = AtSea();
            CrewCard mozzo = s.Crew(0, s.AboveSlot(0), CrewCardId.Mozzo);
            CrewCard cook = s.Crew(0, s.BelowSlot(0), CrewCardId.Cuoco);
            List<GameEvent> events = s.PlayTurns(Pick(Opt<SwapOption>(0, o => o.From == 0 && o.To == s.BelowSlot(0))));

            var swaps = s.DecisionsOf(DecisionKind.SeaAction, 0)[0].Options.OfType<SwapOption>().ToList();
            // Mozzo: verso 4 slot; Cuoco: verso i 3 vuoti (lo scambio con il Mozzo è già contato).
            Assert.AreEqual(4 + 3, swaps.Count);
            Assert.AreSame(cook, s.P(0).Crew[s.AboveSlot(0)]);
            Assert.AreSame(mozzo, s.P(0).Crew[s.BelowSlot(0)]);
            Assert.IsFalse(events.OfType<CrewLostEvent>().Any(), "uno swap non è una perdita (R-021)");
            Assert.AreEqual(1, events.OfType<CrewSwappedEvent>().Count());
        }

        [Test]
        public void ALockedNostromoNeverGoesBackBelow_R016_R057()
        {
            RoundScenario s = AtSea();
            s.Crew(0, s.AboveSlot(0), CrewCardId.Nostromo);
            s.Crew(0, s.AboveSlot(1), CrewCardId.Mozzo);
            s.PlayTurns();

            var swaps = s.DecisionsOf(DecisionKind.SeaAction, 0)[0].Options.OfType<SwapOption>().ToList();
            Assert.IsFalse(swaps.Any(o => o.From == 0 && o.To >= s.Config.slotsAbove), "il Nostromo non scende");
            Assert.IsTrue(swaps.Any(o => o.From == 0 && o.To == 1), "tra slot sopra coperta si può");
        }

        [Test]
        public void ANostromoThatNeverWentAboveCanMoveFreely_R016()
        {
            RoundScenario s = AtSea();
            s.Crew(0, s.BelowSlot(0), CrewCardId.Nostromo);
            s.PlayTurns(Pick(Opt<SwapOption>(0, o => o.From == s.BelowSlot(0) && o.To == 0)));
            Assert.AreEqual(CrewCardId.Nostromo, s.P(0).Crew[0].Kind);
            Assert.IsTrue(s.State.LockedNostromi.Contains(s.P(0).Crew[0].Uid), "salito sopra coperta, ora è bloccato");
        }

        [Test]
        public void AStolenNostromoCanBeReceivedBelowDeck_R020_R016()
        {
            RoundScenario s = Create(2).At(0, 5, 7).At(1, 6, 7).Order(0, 1);
            s.P(1).Coins = 0;
            CrewCard nostromo = s.Crew(1, s.AboveSlot(0), CrewCardId.Nostromo);
            s.Hand(0, PirateCardId.Arrembaggio);
            s.PlayTurns(Pick(Opt<AttackOption>(0, o => o.Opening == AttackOpening.Arrembaggio),
                Opt<ReceiveCrewOption>(0, o => o.Slot == s.BelowSlot(0))));

            PendingDecision receive = s.DecisionsOf(DecisionKind.ReceiveCrew, 0).Single();
            Assert.IsTrue(receive.Options.Cast<ReceiveCrewOption>().Any(o => o.Slot >= s.Config.slotsAbove), "anche sotto coperta");
            Assert.AreSame(nostromo, s.P(0).Crew[s.BelowSlot(0)]);
            Assert.IsFalse(s.State.LockedNostromi.Contains(nostromo.Uid), "il blocco vale solo da quando è sopra coperta");
            TestSupport.AssertInvariants(s.Session);
        }

        [Test]
        public void AReceivedNostromoIsLockedOnceItGoesAbove_R020_R016()
        {
            RoundScenario s = Create(2).At(0, 5, 7).At(1, 6, 7).Order(0, 1);
            s.P(1).Coins = 0;
            CrewCard nostromo = s.Crew(1, s.AboveSlot(0), CrewCardId.Nostromo);
            s.Hand(0, PirateCardId.Arrembaggio);
            s.PlayTurns(Pick(Opt<AttackOption>(0, o => o.Opening == AttackOpening.Arrembaggio),
                Opt<ReceiveCrewOption>(0, o => o.Slot == s.AboveSlot(0))));

            Assert.AreSame(nostromo, s.P(0).Crew[s.AboveSlot(0)]);
            Assert.IsTrue(s.State.LockedNostromi.Contains(nostromo.Uid));
            s.PlayTurns();
            var swaps = s.DecisionsOf(DecisionKind.SeaAction, 0).Last().Options.OfType<SwapOption>().ToList();
            Assert.IsFalse(swaps.Any(o => o.From == s.AboveSlot(0) && o.To >= s.Config.slotsAbove), "ora non scende più");
        }

        [Test]
        public void TheCabinBoyStillPaysOnASeaTurn_R052()
        {
            RoundScenario s = AtSea();
            s.Crew(0, s.AboveSlot(0), CrewCardId.Mozzo);
            s.PlayTurns();
            Assert.AreEqual(10 + s.Config.cabinBoyCoins, s.P(0).Coins);
        }

        [Test]
        public void HandSizeHasNoLimit_R012()
        {
            RoundScenario s = AtSea();
            for (int i = 0; i < 20; i++) s.Hand(0, PirateCardId.Parle);
            s.PlayTurns(Pick(Opt<DrawCardsOption>(0)));
            Assert.AreEqual(20 + s.Config.seaDrawCount, s.P(0).Hand.Count);
        }

        // ---- Vedetta (R-058) ----

        [Test]
        public void TheLookoutMayTreatASeaTurnNextToAnIslandAsAPortTurn_R058()
        {
            RoundScenario s = Create(2).At(0, 6, 5).At(1, 16, 16).Order(0, 1);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Vedetta);
            s.Random.Enqueue(3);
            List<GameEvent> events = s.PlayTurns(Pick(Opt<TurnKindOption>(0, o => o.Kind == TurnKind.Port), Opt<PortActionOption>(0, o => o.Action == PortAction.Plunder)));

            PendingDecision choice = s.DecisionsOf(DecisionKind.LookoutChoice, 0).Single();
            Assert.IsTrue(choice.IsSecret);
            Assert.AreEqual(2, choice.Options.Count);
            TurnKindEvent kind = events.OfType<TurnKindEvent>().Single(e => e.Player == 0);
            Assert.AreEqual(TurnKind.Port, kind.Kind);
            Assert.IsTrue(kind.ByLookout);
            Assert.AreEqual(new Coord(6, 5), s.P(0).Position, "la nave resta dov'è");
            Assert.IsTrue(s.DecisionsOf(DecisionKind.PortAction, 0).Any());
        }

        [Test]
        public void TheLookoutCanStillPlayAtSea_R058()
        {
            RoundScenario s = Create(2).At(0, 6, 5).At(1, 16, 16).Order(0, 1);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Vedetta);
            s.PlayTurns(Pick(Opt<TurnKindOption>(0, o => o.Kind == TurnKind.Sea)));
            Assert.IsTrue(s.DecisionsOf(DecisionKind.SeaAction, 0).Any());
            Assert.IsFalse(s.DecisionsOf(DecisionKind.PortAction, 0).Any());
        }

        [Test]
        public void WithoutLookoutOrWithItBelowThereIsNoChoice_R058_R011()
        {
            RoundScenario none = Create(2).At(0, 6, 5).At(1, 16, 16).Order(0, 1);
            none.PlayTurns();
            Assert.IsFalse(none.DecisionsOf(DecisionKind.LookoutChoice, 0).Any());

            RoundScenario below = Create(2).At(0, 6, 5).At(1, 16, 16).Order(0, 1);
            below.Crew(0, below.BelowSlot(0), CrewCardId.Vedetta);
            below.PlayTurns();
            Assert.IsFalse(below.DecisionsOf(DecisionKind.LookoutChoice, 0).Any());
        }

        [Test]
        public void JokerAndLookoutReachAnIslandTwoCellsAway_R058_R015()
        {
            // (7,6) dista 2 dall'isola 0 in (5,4).
            RoundScenario plain = Create(2).At(0, 7, 6).At(1, 16, 16).Order(0, 1);
            plain.Crew(0, plain.AboveSlot(0), CrewCardId.Vedetta);
            plain.PlayTurns();
            Assert.IsFalse(plain.DecisionsOf(DecisionKind.LookoutChoice, 0).Any());

            RoundScenario joker = Create(2).At(0, 7, 6).At(1, 16, 16).Order(0, 1);
            joker.Crew(0, joker.AboveSlot(0), CrewCardId.Vedetta);
            joker.Crew(0, joker.AboveSlot(1), CrewCardId.Jolly);
            joker.PlayTurns();
            Assert.IsTrue(joker.DecisionsOf(DecisionKind.LookoutChoice, 0).Any());
        }

        [Test]
        public void JokersAloneOrTogetherDoNothing_R015()
        {
            RoundScenario s = InPort();
            s.Crew(0, s.AboveSlot(0), CrewCardId.Jolly);
            s.Crew(0, s.AboveSlot(1), CrewCardId.Jolly);
            s.P(0).Coins = 20;
            s.PlayRound(ToPort);
            Assert.AreEqual(s.Config.leisureCost, PortOptions(s).Single(o => o.Action == PortAction.Leisure).Cost);
            Assert.AreEqual(0, CrewEffects.EffectiveCount(s.P(0).CrewAbove, CrewCardId.Cuoco));
        }
    }
}
