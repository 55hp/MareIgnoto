using System;
using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Decisions;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.State;
using NUnit.Framework;
using static hp55games.MareIgnoto.Rules.Tests.RoundScenario;

namespace hp55games.MareIgnoto.Rules.Tests
{
    /// <summary>
    /// Carte Pirateria giocate nel turno (02_regole.md §7.3, 03_contenuti.md §2). p0 in (5,7) gioca, p1 in (6,7) è a tiro;
    /// le navi restano ferme. Monete fissate a 10.
    /// </summary>
    public class PirateCardTests
    {
        private static RoundScenario Table(int targetX = 6, int targetY = 7)
        {
            RoundScenario s = Create(2).At(0, 5, 7).At(1, targetX, targetY).Order(0, 1);
            s.P(0).Coins = 10;
            s.P(1).Coins = 10;
            return s;
        }

        private static Func<PendingDecision, DecisionOption, bool> Play(PirateCardId id) =>
            Opt<PlayCardOption>(0, o => o.Card.Id == id);

        private static IEnumerable<PirateCardId> Playable(RoundScenario s) =>
            s.DecisionsOf(DecisionKind.SeaAction, 0)[0].Options.OfType<PlayCardOption>().Select(o => o.Card.Id);

        private static void Cooks(RoundScenario s, int cooks)
        {
            if (cooks >= 1) s.Crew(0, s.AboveSlot(0), CrewCardId.Cuoco);
            if (cooks >= 2) s.Crew(0, s.AboveSlot(1), CrewCardId.Jolly);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void FortunateCatchGivesCoinsPlusOnePerCook_R122(int cooks)
        {
            RoundScenario s = Table();
            Cooks(s, cooks);
            s.Hand(0, PirateCardId.PescaFortunata);
            List<GameEvent> events = s.PlayTurns(Pick(Play(PirateCardId.PescaFortunata)));

            Assert.AreEqual(10 + s.Config.fortunateCatchCoins + cooks * s.Config.cookDrawBonus, s.P(0).Coins);
            Assert.AreEqual(PirateCardId.PescaFortunata, events.OfType<CardPlayedEvent>().Single().Card.Id);
            Assert.AreEqual(EventVisibility.Public, events.OfType<CardPlayedEvent>().Single().Visibility);
            Assert.IsTrue(s.State.Pirate.DiscardPile.Any(c => c.Id == PirateCardId.PescaFortunata));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void TrawlNetIsDiscardedAndDrawsThreePlusOnePerCook_R122(int cooks)
        {
            RoundScenario s = Table();
            Cooks(s, cooks);
            s.Hand(0, PirateCardId.ReteAStrascico);
            s.PlayTurns(Pick(Play(PirateCardId.ReteAStrascico)));

            Assert.AreEqual(s.Config.trawlNetDrawCount + cooks * s.Config.cookDrawBonus, s.P(0).Hand.Count);
            Assert.IsTrue(s.State.Pirate.DiscardPile.Any(c => c.Id == PirateCardId.ReteAStrascico));
        }

        [Test]
        public void ManOverboardMakesTheTargetDiscardACrewOfItsChoice_R120_R021()
        {
            RoundScenario s = Table();
            s.Crew(1, s.AboveSlot(0), CrewCardId.Mozzo);
            CrewCard cook = s.Crew(1, s.BelowSlot(0), CrewCardId.Cuoco);
            s.Hand(0, PirateCardId.UomoInMare);
            List<GameEvent> events = s.PlayTurns(Pick(Play(PirateCardId.UomoInMare),
                Opt<CrewSlotOption>(1, o => o.Slot == s.BelowSlot(0))));

            PendingDecision target = s.DecisionsOf(DecisionKind.CardTarget, 0).SingleOrDefault();
            Assert.IsNull(target, "un solo bersaglio: lo applica il motore");
            PendingDecision discard = s.DecisionsOf(DecisionKind.ManOverboardDiscard, 1).Single();
            Assert.IsTrue(discard.IsSecret, "sceglie il bersaglio, in segreto");
            Assert.AreEqual(2, discard.Options.Count);

            CrewLostEvent lost = events.OfType<CrewLostEvent>().Single();
            Assert.AreEqual(CrewLossCause.ManOverboard, lost.Cause);
            Assert.AreSame(cook, lost.Card);
            Assert.IsTrue(lost.AtSea, "R-022");
            Assert.IsTrue(s.State.Crew.DiscardPile.Contains(cook));
        }

        [Test]
        public void ManOverboardChoosesAmongTargetsInRange_R120()
        {
            RoundScenario s = Create(3).At(0, 5, 7).At(1, 6, 7).At(2, 4, 8).Order(0, 1, 2);
            s.Crew(1, s.AboveSlot(0), CrewCardId.Mozzo);
            s.Crew(2, s.AboveSlot(0), CrewCardId.Mozzo);
            s.Hand(0, PirateCardId.UomoInMare);
            s.PlayTurns(Pick(Play(PirateCardId.UomoInMare), Opt<TargetPlayerOption>(0, o => o.Target == 2)));

            PendingDecision target = s.DecisionsOf(DecisionKind.CardTarget, 0).Single();
            Assert.IsFalse(target.IsSecret);
            CollectionAssert.AreEquivalent(new[] { 1, 2 }, target.Options.Cast<TargetPlayerOption>().Select(o => o.Target));
            Assert.IsNull(s.P(2).Crew[s.AboveSlot(0)]);
            Assert.IsNotNull(s.P(1).Crew[s.AboveSlot(0)]);
        }

        [Test]
        public void ManOverboardNeedsATargetInRangeAtSeaWithCrew_R120()
        {
            RoundScenario far = Table(7, 7);
            far.Crew(1, far.AboveSlot(0), CrewCardId.Mozzo);
            far.Hand(0, PirateCardId.UomoInMare);
            far.PlayTurns();
            CollectionAssert.DoesNotContain(Playable(far), PirateCardId.UomoInMare);

            RoundScenario empty = Table();
            empty.Hand(0, PirateCardId.UomoInMare);
            empty.PlayTurns();
            CollectionAssert.DoesNotContain(Playable(empty), PirateCardId.UomoInMare);
        }

        [Test]
        public void TheMedicoSavesAnAboveDeckCardFromManOverboard_R023()
        {
            RoundScenario s = Table();
            s.Crew(1, s.AboveSlot(0), CrewCardId.Mozzo);
            CrewCard medico = s.Crew(1, s.BelowSlot(0), CrewCardId.Medico);
            s.Hand(0, PirateCardId.UomoInMare);
            List<GameEvent> events = s.PlayTurns(Pick(Play(PirateCardId.UomoInMare),
                Opt<CrewSlotOption>(1, o => o.Slot == 0), Opt<MedicoOption>(1, o => o.Use)));

            Assert.AreSame(medico, s.P(1).Crew[s.AboveSlot(0)]);
            Assert.IsFalse(events.OfType<CrewLostEvent>().Any());
        }

        [Test]
        public void SpyglassSwapsTwoSlotsOfTheTarget_R120()
        {
            RoundScenario s = Table();
            CrewCard mozzo = s.Crew(1, s.AboveSlot(0), CrewCardId.Mozzo);
            CrewCard cook = s.Crew(1, s.BelowSlot(0), CrewCardId.Cuoco);
            s.Hand(0, PirateCardId.Spyglass);
            List<GameEvent> events = s.PlayTurns(Pick(Play(PirateCardId.Spyglass)));

            Assert.AreSame(cook, s.P(1).Crew[s.AboveSlot(0)]);
            Assert.AreSame(mozzo, s.P(1).Crew[s.BelowSlot(0)]);
            var spy = events.OfType<SpyglassEvent>().Single();
            Assert.AreEqual(1, spy.Target);
            Assert.IsFalse(events.OfType<CrewLostEvent>().Any(), "uno scambio non è una perdita (R-021)");
        }

        [Test]
        public void SpyglassCannotSendALockedNostromoBelow_R016()
        {
            RoundScenario s = Table();
            s.Crew(1, s.AboveSlot(0), CrewCardId.Nostromo);
            s.Crew(1, s.AboveSlot(1), CrewCardId.Mozzo);
            s.Crew(1, s.BelowSlot(0), CrewCardId.Cuoco);
            s.Hand(0, PirateCardId.Spyglass);
            s.PlayTurns(Pick(Play(PirateCardId.Spyglass)));

            var pairs = s.DecisionsOf(DecisionKind.CardTarget, 0).Single().Options.Cast<SpyglassOption>()
                .Select(o => (o.SlotA, o.SlotB)).ToList();
            CollectionAssert.AreEquivalent(new[] { (0, 1), (1, s.BelowSlot(0)) }, pairs);
        }

        [TestCase(1, Heading.NE)]
        [TestCase(-1, Heading.NO)]
        public void SupplicaMovesTheWindOneStepEitherWay(int steps, Heading expected)
        {
            RoundScenario s = Table();
            int treasure = s.State.Treasure;
            s.Hand(0, PirateCardId.SupplicaGartya);
            List<GameEvent> events = s.PlayTurns(Pick(Play(PirateCardId.SupplicaGartya), Opt<WindStepOption>(0, o => o.Steps == steps)));

            Assert.AreEqual(expected, s.State.Wind);
            Assert.AreEqual(expected, events.OfType<WindChangedEvent>().Single().Wind);
            int cost = s.Config.PirateCost(PirateCardId.SupplicaGartya);
            Assert.AreEqual(10 - cost, s.P(0).Coins);
            Assert.AreEqual(treasure + cost, s.State.Treasure, "il costo va al Tesoro (R-121)");
        }

        [Test]
        public void RafficaCanagliaReversesTheWind()
        {
            RoundScenario s = Table();
            s.Hand(0, PirateCardId.RafficaCanaglia);
            s.PlayTurns(Pick(Play(PirateCardId.RafficaCanaglia)));
            Assert.AreEqual(Heading.S, s.State.Wind);
            Assert.AreEqual(10, s.P(0).Coins, "costa 0");
        }

        [TestCase(PirateCardId.InvocazioneGartya, WeatherState.Normal, WeatherState.RoughSea)]
        [TestCase(PirateCardId.IraGartya, WeatherState.Normal, WeatherState.Storm)]
        [TestCase(PirateCardId.FavoreGartya, WeatherState.Storm, WeatherState.RoughSea)]
        [TestCase(PirateCardId.FavoreGartya, WeatherState.RoughSea, WeatherState.Normal)]
        [TestCase(PirateCardId.FavoreGartya, WeatherState.Normal, WeatherState.Normal)]
        public void ZoneCardsChangeAnyZoneAndPayTheTreasure_R121(PirateCardId card, WeatherState before, WeatherState after)
        {
            // La zona bersaglio è lontana dalla nave: le carte Meteo bersagliano qualsiasi zona.
            RoundScenario s = Table();
            s.P(0).Coins = 20;
            s.ZoneAt(17, 17, before);
            int zone = s.State.Map.ZoneOf(new Coord(17, 17));
            int treasure = s.State.Treasure;
            s.Hand(0, card);
            List<GameEvent> events = s.PlayTurns(Pick(Play(card), Opt<ZoneOption>(0, o => o.Zone == zone)));

            Assert.AreEqual(after, s.State.ZoneStates[zone]);
            Assert.AreEqual(zone, events.OfType<ZoneChangedEvent>().Single().Zone);
            Assert.AreEqual(s.State.ZoneStates.Length, s.DecisionsOf(DecisionKind.CardTarget, 0).Single().Options.Count);
            int cost = s.Config.PirateCost(card);
            Assert.AreEqual(20 - cost, s.P(0).Coins);
            Assert.AreEqual(treasure + cost, s.State.Treasure);
        }

        [Test]
        public void WeatherCardsNeedTheCoinsForTheirCost_R121_R096()
        {
            RoundScenario s = Table();
            s.P(0).Coins = s.Config.PirateCost(PirateCardId.InvocazioneGartya) - 1;
            s.Hand(0, PirateCardId.InvocazioneGartya, PirateCardId.IraGartya, PirateCardId.FavoreGartya, PirateCardId.VentoInPoppa);
            s.PlayTurns();

            CollectionAssert.AreEquivalent(new[] { PirateCardId.FavoreGartya, PirateCardId.VentoInPoppa }, Playable(s));
        }

        [Test]
        public void TailwindProtectsInTheNextRound_R087()
        {
            RoundScenario s = Table();
            s.Hand(0, PirateCardId.VentoInPoppa);
            s.PlayTurns(Pick(Play(PirateCardId.VentoInPoppa)));
            Assert.AreEqual(2, s.P(0).TailwindRound);

            s.ZoneAt(5, 7, WeatherState.Storm);
            List<GameEvent> round2 = s.PlayTurns();
            Assert.AreEqual(WeatherSkipReason.Tailwind, round2.OfType<WeatherSkippedEvent>().Single(e => e.Player == 0).Reason);
        }

        [Test]
        public void CombatCardsCannotBePlayedOutsideCombat_R123_R102()
        {
            RoundScenario s = Table(16, 16);
            s.Hand(0, PirateCardId.Bordata, PirateCardId.Parle, PirateCardId.Arrembaggio);
            s.PlayTurns();
            Assert.IsEmpty(Playable(s));
        }

        [TestCase(0, 1)]
        [TestCase(1, 2)]
        [TestCase(2, 3)]
        public void OneTurnCardPlusOnePerBuccaneer_R056(int buccaneers, int expected)
        {
            RoundScenario s = Table();
            if (buccaneers >= 1) s.Crew(0, s.AboveSlot(0), CrewCardId.Bucaniere);
            if (buccaneers >= 2) s.Crew(0, s.AboveSlot(1), CrewCardId.Jolly);
            s.Hand(0, PirateCardId.PescaFortunata, PirateCardId.PescaFortunata, PirateCardId.PescaFortunata, PirateCardId.PescaFortunata);
            List<GameEvent> events = s.PlayTurns(Pick(Play(PirateCardId.PescaFortunata)));
            Assert.AreEqual(expected, events.OfType<CardPlayedEvent>().Count());
        }

        [Test]
        public void ABuccaneerBelowDeckDoesNothing_R011()
        {
            RoundScenario s = Table();
            s.Crew(0, s.BelowSlot(0), CrewCardId.Bucaniere);
            s.Hand(0, PirateCardId.PescaFortunata, PirateCardId.PescaFortunata);
            List<GameEvent> events = s.PlayTurns(Pick(Play(PirateCardId.PescaFortunata)));
            Assert.AreEqual(1, events.OfType<CardPlayedEvent>().Count());
        }
    }
}
