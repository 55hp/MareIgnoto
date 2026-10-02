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
    /// Meteo della Fase 1 (02_regole.md §6). p0 in (2,2), zona 0; p1 lontano in (16,16) con meteo Normale.
    /// Vento da Nord, rotte verso E: senza meteo p0 arriva in (3,2).
    /// </summary>
    public class WeatherTests
    {
        private static readonly Heading[] East = { Heading.E, Heading.E };

        private static RoundScenario InZone(WeatherState weather)
        {
            return RoundScenario.Create(2).At(0, 2, 2).At(1, 16, 16).Order(0, 1).ZoneAt(2, 2, weather);
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
            Assert.AreEqual(new Coord(3, 1), s.P(0).Position, "si muove con la rotta ruotata");

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
            Assert.AreEqual(new Coord(3, 2), s.P(0).Position);
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

        [Test]
        public void TheNavigatorLowersTheStormToRoughSea_R085()
        {
            RoundScenario s = InZone(WeatherState.Storm);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Navigatore);
            s.Crew(0, s.BelowSlot(0), CrewCardId.Mozzo);
            s.Hand(0, PirateCardId.Bordata);
            s.Random.Enqueue(8);
            List<GameEvent> events = s.PlayRound(East);

            var applied = events.OfType<WeatherAppliedEvent>().Single(e => e.Player == 0);
            Assert.AreEqual(WeatherState.Storm, applied.ZoneState);
            Assert.AreEqual(WeatherState.RoughSea, applied.Perceived);
            Assert.AreEqual(0, s.P(0).Hand.Count, "perde la carta Pirateria del Mare Mosso");
            Assert.IsNotNull(s.P(0).Crew[s.BelowSlot(0)], "non la crew della Tempesta");
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
        public void JokerAndNavigatorCalmTheStorm_R085_R015()
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
            Assert.AreEqual(new Coord(3, 2), s.P(0).Position);
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
        public void IslandsAndBorderHaveNoWeather_R080_R082()
        {
            // p0 sull'isola 0 (4,4), dentro il blocco della zona 7 in Tempesta; p1 sulla cornice accanto alla zona 0 in Tempesta.
            RoundScenario s = RoundScenario.Create(2).At(0, 4, 4).At(1, 0, 2).Order(0, 1)
                .ZoneAt(5, 5, WeatherState.Storm).ZoneAt(2, 2, WeatherState.Storm);
            Assert.AreEqual(-1, s.State.Map.ZoneOf(new Coord(4, 4)));
            List<GameEvent> events = s.PlayRound(new[] { Heading.N, Heading.E });

            Assert.IsFalse(events.OfType<WeatherAppliedEvent>().Any());
        }

        [Test]
        public void WeatherIsTheOneOfTheZoneBeforeMoving_R082()
        {
            // p0 in (3,2), zona 0 Normale, entra in (4,2), zona 1 in Tempesta: nessun effetto in questo round.
            RoundScenario s = RoundScenario.Create(2).At(0, 3, 2).At(1, 16, 16).Order(0, 1).ZoneAt(4, 2, WeatherState.Storm);
            List<GameEvent> events = s.PlayRound(East);

            Assert.AreEqual(new Coord(4, 2), s.P(0).Position);
            Assert.AreEqual(WeatherState.Normal, events.OfType<WeatherAppliedEvent>().Single(e => e.Player == 0).Perceived);
        }

        [Test]
        public void ZonesDoNotDecay_R081()
        {
            RoundScenario s = InZone(WeatherState.Storm);
            int zone = s.State.Map.ZoneOf(new Coord(2, 2));
            s.PlayRound(East);
            s.PlayRound(East);
            Assert.AreEqual(WeatherState.Storm, s.Session.State.Zones[zone]);
        }

        [Test]
        public void WeatherChoicesFollowTheTurnOrderOfTheRound_R042()
        {
            RoundScenario s = RoundScenario.Create(2).At(0, 2, 2).At(1, 2, 3).Order(1, 0).ZoneAt(2, 2, WeatherState.RoughSea);
            s.Hand(0, PirateCardId.Bordata, PirateCardId.Parle);
            s.Hand(1, PirateCardId.Bordata, PirateCardId.Parle);
            s.Random.Enqueue(8, 8);
            s.PlayRound(East);

            CollectionAssert.AreEqual(new[] { 1, 0 },
                s.Decisions.Where(d => d.Kind == DecisionKind.WeatherPirateLoss).Select(d => d.Player));
        }
    }
}
