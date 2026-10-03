using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Decisions;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.State;
using NUnit.Framework;

namespace hp55games.MareIgnoto.Rules.Tests
{
    /// <summary>
    /// Meteo della Fase 1 (02_regole.md §6) sul layout v5. p0 in D7, dentro la nuvola N5; p1 lontano in Q16, mare libero.
    /// Vento da Nord, rotte verso E: senza meteo p0 arriva in E7 (sempre N5).
    /// </summary>
    public class WeatherTests
    {
        private static readonly Heading[] East = { Heading.E, Heading.E };
        private static readonly Coord Start = Coord.Parse("D7");

        private static Coord C(string name) => Coord.Parse(name);

        private static RoundScenario InZone(WeatherState weather)
        {
            return RoundScenario.Create(2).At(0, Start.X, Start.Y).At(1, C("Q16").X, C("Q16").Y).Order(0, 1)
                .ZoneAt(Start.X, Start.Y, weather);
        }

        [Test]
        public void RoughSeaRotatesByTheD8AndCostsOnePirateCard_R083()
        {
            RoundScenario s = InZone(WeatherState.RoughSea);
            s.Hand(0, PirateCardId.Bordata, PirateCardId.Parle);
            s.Random.Enqueue(1);
            List<GameEvent> events = s.PlayRound(East);

            var rotated = events.OfType<HeadingRotatedEvent>().Single();
            Assert.AreEqual(Heading.E, rotated.From);
            Assert.AreEqual(Heading.SE, rotated.To, "1 scatto in senso orario");
            Assert.AreEqual(Start.Step(Heading.SE), s.P(0).Position, "si muove con la rotta ruotata");

            PendingDecision loss = s.Decisions.Single(d => d.Kind == DecisionKind.WeatherPirateLoss);
            Assert.IsTrue(loss.IsSecret);
            Assert.AreEqual(2, loss.Options.Count);
            Assert.AreEqual(2 - s.Config.roughSeaPirateLoss, s.P(0).Hand.Count);
            Assert.AreEqual(DiceReason.WeatherRotation, events.OfType<DieRolledEvent>().Single().Reason);
        }

        [Test]
        public void ARotationOf8LeavesTheHeadingUnchanged_R083()
        {
            RoundScenario s = InZone(WeatherState.RoughSea);
            s.Random.Enqueue(8);
            List<GameEvent> events = s.PlayRound(East);

            Assert.AreEqual(events.OfType<HeadingRotatedEvent>().Single().From, events.OfType<HeadingRotatedEvent>().Single().To);
            Assert.AreEqual(Start.Step(Heading.E), s.P(0).Position);
        }

        [Test]
        public void StormRotatesAndCostsABelowDeckCrewButNoPirateCard_R084()
        {
            RoundScenario s = InZone(WeatherState.Storm);
            s.Crew(0, s.BelowSlot(0), CrewCardId.Mozzo);
            s.Crew(0, s.BelowSlot(1), CrewCardId.Cuoco);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Bucaniere);
            s.Hand(0, PirateCardId.Bordata);
            s.Random.Enqueue(8);
            List<GameEvent> events = s.PlayRound(East);

            PendingDecision loss = s.Decisions.Single(d => d.Kind == DecisionKind.StormCrewLoss);
            Assert.IsTrue(loss.IsSecret);
            CollectionAssert.AreEqual(new[] { s.BelowSlot(0), s.BelowSlot(1) }, loss.Options.Cast<CrewSlotOption>().Select(o => o.Slot),
                "solo sotto coperta");

            var lost = events.OfType<CrewLostEvent>().Single();
            Assert.AreEqual(CrewLossCause.Storm, lost.Cause);
            Assert.IsFalse(lost.Above);
            Assert.AreEqual(1, s.P(0).Hand.Count, "la Tempesta non toglie carte Pirateria");
            Assert.IsNotNull(s.P(0).Crew[s.AboveSlot(0)]);
            Assert.AreEqual(1, events.OfType<HeadingRotatedEvent>().Count());
        }

        [Test]
        public void StormWithoutBelowDeckCrewCostsNothing_R084()
        {
            RoundScenario s = InZone(WeatherState.Storm);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Bucaniere);
            s.Random.Enqueue(8);
            List<GameEvent> events = s.PlayRound(East);

            Assert.IsFalse(events.OfType<CrewLostEvent>().Any());
            Assert.IsFalse(s.Decisions.Any(d => d.Kind == DecisionKind.StormCrewLoss));
        }

        [TestCase(0, WeatherState.Normal)]
        [TestCase(1, WeatherState.RoughSea)]
        [TestCase(2, WeatherState.Storm)]
        [TestCase(5, WeatherState.Storm)]
        public void TheLevelOfTheZoneGivesTheEffect_R081(int level, WeatherState effect)
        {
            RoundScenario s = InZone(WeatherState.Normal);
            s.ZoneAt(Start.X, Start.Y, level);
            s.Random.Enqueue(8);
            List<GameEvent> events = s.PlayRound(East);

            WeatherAppliedEvent applied = events.OfType<WeatherAppliedEvent>().Single(e => e.Player == 0);
            Assert.AreEqual(level, applied.ZoneLevel);
            Assert.AreEqual(effect, applied.Perceived);
        }

        [Test]
        public void ARingSliceStartsAtLevelFiveAndIsAStorm_R038_R081()
        {
            // Senza azzerare le zone (come fa RoundScenario): lo spicchio R5 è al livello iniziale.
            RoundScenario s = InZone(WeatherState.Normal).At(0, C("H9").X, C("H9").Y);
            int r5 = s.State.Map.ZoneIndex("R5");
            s.State.ZoneLevels[r5] = s.State.Map.ZoneInitialLevel(r5);
            s.Crew(0, s.BelowSlot(0), CrewCardId.Mozzo);
            s.Random.Enqueue(8);
            List<GameEvent> events = s.PlayRound(new[] { Heading.O, Heading.E });

            WeatherAppliedEvent applied = events.OfType<WeatherAppliedEvent>().Single(e => e.Player == 0);
            Assert.AreEqual(s.State.Map.ZoneInitialLevel(r5), applied.ZoneLevel);
            Assert.AreEqual(WeatherState.Storm, applied.Perceived);
            Assert.AreEqual(CrewLossCause.Storm, events.OfType<CrewLostEvent>().Single().Cause);
        }

        [Test]
        public void TheNavigatorLowersTheLevelByOne_R085()
        {
            // Livello 2 → percepito 1: Mare Mosso (perde la carta Pirateria, non la crew).
            RoundScenario s = InZone(WeatherState.Normal);
            s.ZoneAt(Start.X, Start.Y, s.Config.stormLevel);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Navigatore);
            s.Crew(0, s.BelowSlot(0), CrewCardId.Mozzo);
            s.Hand(0, PirateCardId.Bordata);
            s.Random.Enqueue(8);
            List<GameEvent> events = s.PlayRound(East);

            var applied = events.OfType<WeatherAppliedEvent>().Single(e => e.Player == 0);
            Assert.AreEqual(s.Config.stormLevel, applied.ZoneLevel);
            Assert.AreEqual(WeatherState.RoughSea, applied.Perceived);
            Assert.AreEqual(0, s.P(0).Hand.Count, "perde la carta Pirateria del Mare Mosso");
            Assert.IsNotNull(s.P(0).Crew[s.BelowSlot(0)], "non la crew della Tempesta");
        }

        [Test]
        public void AtLevelFiveTheNavigatorStillFacesAStorm_R085()
        {
            RoundScenario s = InZone(WeatherState.Normal);
            s.ZoneAt(Start.X, Start.Y, 5);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Navigatore);
            s.Crew(0, s.AboveSlot(1), CrewCardId.Jolly);
            s.Random.Enqueue(8);
            List<GameEvent> events = s.PlayRound(East);

            Assert.AreEqual(WeatherState.Storm, events.OfType<WeatherAppliedEvent>().Single(e => e.Player == 0).Perceived,
                "5 - 2 = 3: ancora Tempesta");
        }

        [Test]
        public void TheNavigatorCalmsRoughSea_R085()
        {
            RoundScenario s = InZone(WeatherState.RoughSea);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Navigatore);
            s.Hand(0, PirateCardId.Bordata);
            List<GameEvent> events = s.PlayRound(East);

            Assert.AreEqual(WeatherState.Normal, events.OfType<WeatherAppliedEvent>().Single(e => e.Player == 0).Perceived);
            Assert.IsFalse(events.OfType<HeadingRotatedEvent>().Any());
            Assert.IsFalse(events.OfType<DieRolledEvent>().Any());
            Assert.AreEqual(1, s.P(0).Hand.Count);
        }

        [Test]
        public void JokerAndNavigatorCalmALevelTwoStorm_R085_R015()
        {
            RoundScenario s = InZone(WeatherState.Storm);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Navigatore);
            s.Crew(0, s.AboveSlot(1), CrewCardId.Jolly);
            s.Crew(0, s.BelowSlot(0), CrewCardId.Mozzo);
            List<GameEvent> events = s.PlayRound(East);

            Assert.AreEqual(WeatherState.Normal, events.OfType<WeatherAppliedEvent>().Single(e => e.Player == 0).Perceived);
            Assert.IsFalse(events.OfType<HeadingRotatedEvent>().Any());
            Assert.IsFalse(events.OfType<CrewLostEvent>().Any());
        }

        [Test]
        public void TheHelmsmanChoosesTheRotation_R086()
        {
            RoundScenario s = InZone(WeatherState.RoughSea);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Timoniere);
            int pendingRolls = s.Random.Pending;
            List<GameEvent> events = s.PlayRound(East, RoundScenario.Prefer((d, o) => o is DieValueOption v && v.Value == 4));

            PendingDecision choice = s.Decisions.Single(d => d.Kind == DecisionKind.WeatherRotation);
            Assert.IsFalse(choice.IsSecret);
            CollectionAssert.AreEqual(Enumerable.Range(1, HeadingExtensions.Count), choice.Options.Cast<DieValueOption>().Select(o => o.Value));
            Assert.AreEqual(Heading.O, events.OfType<HeadingRotatedEvent>().Single().To);
            Assert.AreEqual(DiceReason.WeatherRotation, events.OfType<DieChosenEvent>().Single().Reason);
            Assert.IsFalse(events.OfType<DieRolledEvent>().Any(), "non tira");
            Assert.AreEqual(pendingRolls, s.Random.Pending);
        }

        [Test]
        public void TailwindGivesImmunityForTheRound_R087()
        {
            RoundScenario s = InZone(WeatherState.Storm);
            s.P(0).TailwindRound = s.Session.State.Round;
            s.Crew(0, s.BelowSlot(0), CrewCardId.Mozzo);
            List<GameEvent> events = s.PlayRound(East);

            Assert.AreEqual(WeatherSkipReason.Tailwind, events.OfType<WeatherSkippedEvent>().Single().Reason);
            Assert.IsFalse(events.OfType<WeatherAppliedEvent>().Any(e => e.Player == 0));
            Assert.AreEqual(Start.Step(Heading.E), s.P(0).Position);
        }

        [Test]
        public void TailwindOfAnotherRoundDoesNotProtect_R087()
        {
            RoundScenario s = InZone(WeatherState.RoughSea);
            s.P(0).TailwindRound = s.Session.State.Round + 1;
            s.Random.Enqueue(8);
            List<GameEvent> events = s.PlayRound(East);
            Assert.AreEqual(WeatherState.RoughSea, events.OfType<WeatherAppliedEvent>().Single(e => e.Player == 0).Perceived);
        }

        [Test]
        public void IslandsBorderAndFreeSeaHaveNoWeather_R080_R082()
        {
            // p0 sull'isola D8, accanto alla nuvola N5 in Tempesta; p1 sulla cornice A8, accanto a N5; p2 in H8, mare libero.
            RoundScenario s = RoundScenario.Create(3).At(0, C("D8").X, C("D8").Y).At(1, C("A8").X, C("A8").Y)
                .At(2, C("H8").X, C("H8").Y).Order(0, 1, 2).Zone("N5", 5);
            Assert.AreEqual(-1, s.State.Map.ZoneOf(C("D8")));
            Assert.AreEqual(-1, s.State.Map.ZoneOf(C("H8")));
            List<GameEvent> events = s.PlayRound(new[] { Heading.N, Heading.E, Heading.E });

            Assert.IsFalse(events.OfType<WeatherAppliedEvent>().Any());
        }

        [Test]
        public void WeatherIsTheOneOfTheCellBeforeMoving_R082()
        {
            // p0 in B7, mare libero, entra in C7, nella nuvola N5 in Tempesta: nessun effetto in questo round.
            RoundScenario s = RoundScenario.Create(2).At(0, C("B7").X, C("B7").Y).At(1, C("Q16").X, C("Q16").Y).Order(0, 1);
            s.Zone("N5", s.Config.stormLevel);
            List<GameEvent> events = s.PlayRound(East);

            Assert.AreEqual(C("C7"), s.P(0).Position);
            Assert.IsFalse(events.OfType<WeatherAppliedEvent>().Any(e => e.Player == 0));
        }

        [Test]
        public void ZonesDoNotDecay_R081()
        {
            RoundScenario s = InZone(WeatherState.Storm);
            int zone = s.State.Map.ZoneOf(Start);
            s.PlayRound(East);
            s.PlayRound(East);
            Assert.AreEqual(s.Config.stormLevel, s.Session.State.ZoneLevels[zone]);
        }

        [Test]
        public void WeatherChoicesFollowTheTurnOrderOfTheRound_R042()
        {
            RoundScenario s = RoundScenario.Create(2).At(0, Start.X, Start.Y).At(1, C("D6").X, C("D6").Y).Order(1, 0)
                .ZoneAt(Start.X, Start.Y, WeatherState.RoughSea);
            s.Hand(0, PirateCardId.Bordata, PirateCardId.Parle);
            s.Hand(1, PirateCardId.Bordata, PirateCardId.Parle);
            s.Random.Enqueue(8, 8);
            s.PlayRound(East);

            CollectionAssert.AreEqual(new[] { 1, 0 },
                s.Decisions.Where(d => d.Kind == DecisionKind.WeatherPirateLoss).Select(d => d.Player));
        }
    }
}
