using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Decisions;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Map;
using NUnit.Framework;

namespace hp55games.MareIgnoto.Rules.Tests
{
    /// <summary>
    /// Abbordaggio fortuito (02_regole.md §5.3). Salvo dove indicato le navi si incontrano in (4,7): p0 parte da (3,7)
    /// verso E, p1 da (5,7) verso O, p2 da (4,6) verso N; vento da Nord, zone Normali, tiri accodati nell'ordine di turno.
    /// </summary>
    public class BoardingTests
    {
        private static readonly Coord Meeting = new Coord(4, 7);
        private static readonly Heading[] TwoHeadings = { Heading.E, Heading.O };
        private static readonly Heading[] ThreeHeadings = { Heading.E, Heading.O, Heading.N };

        private static Coord C(int x, int y) => new Coord(x, y);

        private static RoundScenario Meet(int players, RulesConfig config = null)
        {
            RoundScenario s = RoundScenario.Create(players, config).At(0, 3, 7).At(1, 5, 7);
            if (players == 2) return s.Order(0, 1);
            s.At(2, 4, 6);
            return s.Order(Enumerable.Range(0, players).ToArray());
        }

        private static Coord Neighbour(Coord cell, int d8) => cell.Step(HeadingExtensions.FromD8(d8));

        [Test]
        public void TwoShipsEachLoseOneCrewOrTwoPirateCardsThenRepositionByDie_R070_R072()
        {
            RoundScenario s = Meet(2);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Mozzo);
            s.Hand(0, PirateCardId.Bordata, PirateCardId.Parle);
            s.Hand(1, PirateCardId.Bordata, PirateCardId.Bordata, PirateCardId.Parle);
            s.Random.Enqueue(1, 5);

            List<GameEvent> events = s.PlayRound(TwoHeadings);

            var losses = s.Decisions.Where(d => d.Kind == DecisionKind.BoardingLoss).ToList();
            CollectionAssert.AreEqual(new[] { 0, 1 }, losses.Select(d => d.Player));
            Assert.IsTrue(losses.All(d => d.IsSecret));

            // p0: la crew dello slot 0, oppure le sue 2 carte Pirateria. Mai entrambe (R-070).
            var p0 = losses[0].Options.Cast<BoardingLossOption>().ToList();
            Assert.AreEqual(2, p0.Count);
            Assert.IsTrue(p0.All(o => o.CrewSlots.Count == 0 ^ o.PirateCards.Count == 0));
            Assert.AreEqual(s.Config.boardingTwoShipsPirateLoss, p0.Single(o => o.PirateCards.Count > 0).PirateCards.Count);
            // p1: niente crew; due coppie distinte tra Bordata, Bordata, Parlè.
            Assert.AreEqual(2, losses[1].Options.Count);

            Assert.IsNull(s.P(0).Crew[s.AboveSlot(0)], "p0 ha scelto la crew (prima opzione)");
            var lost = events.OfType<CrewLostEvent>().Single();
            Assert.AreEqual(CrewLossCause.Boarding, lost.Cause);
            Assert.IsTrue(lost.AtSea, "R-022");
            Assert.AreEqual(1, s.P(1).Hand.Count);

            // R-072: 1 = N, 5 = S.
            Assert.AreEqual(Neighbour(Meeting, 1), s.P(0).Position);
            Assert.AreEqual(Neighbour(Meeting, 5), s.P(1).Position);
            Assert.AreEqual(C(4, 8), s.P(0).Position);
            Assert.AreEqual(C(4, 6), s.P(1).Position);
        }

        [TestCase(1, Heading.N)]
        [TestCase(2, Heading.NE)]
        [TestCase(3, Heading.E)]
        [TestCase(4, Heading.SE)]
        [TestCase(5, Heading.S)]
        [TestCase(6, Heading.SO)]
        [TestCase(7, Heading.O)]
        [TestCase(8, Heading.NO)]
        public void TheD8MapsToTheEightNeighbours_R072(int roll, Heading expected)
        {
            RoundScenario s = Meet(2);
            int other = roll == 8 ? 1 : 8;
            s.Random.Enqueue(roll, other);
            List<GameEvent> events = s.PlayRound(TwoHeadings);

            var moved = events.OfType<ShipRepositionedEvent>().Single(e => e.Player == 0);
            Assert.AreEqual(roll, moved.Value);
            Assert.AreEqual(Meeting.Step(expected), moved.To);
        }

        [Test]
        public void EqualRollsRepeatTheWholeCycle_R070()
        {
            RoundScenario s = Meet(2);
            s.Hand(0, PirateCardId.Bordata, PirateCardId.Bordata, PirateCardId.Bordata, PirateCardId.Bordata);
            s.Hand(1, PirateCardId.Parle, PirateCardId.Parle, PirateCardId.Parle, PirateCardId.Parle);
            s.Random.Enqueue(3, 3, 1, 5);

            List<GameEvent> events = s.PlayRound(TwoHeadings);

            CollectionAssert.AreEqual(new[] { 0, 1 }, events.OfType<BoardingStartedEvent>().Select(e => e.Repeat));
            Assert.AreEqual(4, events.OfType<DieRolledEvent>().Count(e => e.Reason == DiceReason.BoardingReposition));
            Assert.AreEqual(0, s.P(0).Hand.Count, "due perdite da 2 carte (nuova perdita a ogni ciclo)");
            Assert.AreEqual(0, s.P(1).Hand.Count);
            Assert.AreEqual(C(4, 8), s.P(0).Position);
        }

        [Test]
        public void ThreeShipsLoseOneCrewAndOnePirateRollOnceAndStayTogether_R071()
        {
            RoundScenario s = Meet(3);
            for (int p = 0; p < 3; p++)
            {
                s.Crew(p, s.AboveSlot(0), CrewCardId.Mozzo);
                s.Hand(p, PirateCardId.Bordata, PirateCardId.Parle);
            }

            // p0 e p1 finiscono entrambi a N, p2 a S: restano insieme senza nuovo Abbordaggio.
            s.Random.Enqueue(1, 1, 5);
            List<GameEvent> events = s.PlayRound(ThreeHeadings);

            var losses = s.Decisions.Where(d => d.Kind == DecisionKind.BoardingLoss).ToList();
            Assert.AreEqual(3, losses.Count);
            foreach (PendingDecision loss in losses)
                Assert.IsTrue(loss.Options.Cast<BoardingLossOption>().All(o =>
                    o.CrewSlots.Count == s.Config.boardingManyShipsCrewLoss && o.PirateCards.Count == s.Config.boardingManyShipsPirateLoss));

            for (int p = 0; p < 3; p++)
            {
                Assert.IsNull(s.P(p).Crew[s.AboveSlot(0)]);
                Assert.AreEqual(1, s.P(p).Hand.Count);
            }

            Assert.AreEqual(1, events.OfType<BoardingStartedEvent>().Count(), "un solo tiro a testa, nessuna ripetizione né catena");
            Assert.AreEqual(3, events.OfType<DieRolledEvent>().Count(e => e.Reason == DiceReason.BoardingReposition));
            Assert.AreEqual(C(4, 8), s.P(0).Position);
            Assert.AreEqual(C(4, 8), s.P(1).Position);
            Assert.AreEqual(C(4, 6), s.P(2).Position);
            // PlayRound ha verificato gli invarianti: la cella condivisa è ammessa (R-071).
        }

        [Test]
        public void RepositioningOntoTheBorderStrandsTheShip_R073a_R069()
        {
            // Incontro in (1,2): p0 da (1,3) verso S, p1 da (1,1) verso N. p0 tira 7 (O) e finisce sulla cornice (0,2).
            RoundScenario s = RoundScenario.Create(2).At(0, 1, 3).At(1, 1, 1).Order(0, 1).Wind(Heading.NE);
            s.Random.Enqueue(7, 3);
            List<GameEvent> events = s.PlayRound(new[] { Heading.S, Heading.N });

            Assert.AreEqual(C(0, 2), s.P(0).Position);
            Assert.AreEqual(0, events.OfType<ShipStrandedEvent>().Single().Player);
            Assert.AreEqual(C(2, 2), s.P(1).Position);
        }

        [Test]
        public void RepositioningOntoAnIslandPutsTheShipInPort_R073a()
        {
            // Incontro in D4: p0 da D5 verso S, p1 da E4 verso O. p0 tira 8 (NO) → C5, isola 1.
            RoundScenario s = RoundScenario.Create(2).At(0, 3, 5).At(1, 4, 4).Order(0, 1).Wind(Heading.NE);
            s.Random.Enqueue(8, 3);
            List<GameEvent> events = s.PlayRound(new[] { Heading.S, Heading.O });

            Assert.AreEqual(C(3, 4), events.OfType<BoardingStartedEvent>().Single().Cell);
            Assert.AreEqual(C(2, 5), s.P(0).Position);
            Assert.AreEqual(CellKind.Island, s.State.Map.KindAt(s.P(0).Position));
            Assert.AreEqual(1, events.OfType<BoardingStartedEvent>().Count(), "l'isola è zona franca: nessun nuovo Abbordaggio");
        }

        [Test]
        public void RepositioningOntoAnUninvolvedShipStartsANewBoarding_R073a()
        {
            // p2 resta ferma in (4,8) (contro vento). p0 tira 1 (N) e la raggiunge: nuovo Abbordaggio p0–p2.
            RoundScenario s = Meet(3).At(2, 4, 8);
            s.Random.Enqueue(1, 5, 3, 7);
            List<GameEvent> events = s.PlayRound(new[] { Heading.E, Heading.O, Heading.S });

            var boardings = events.OfType<BoardingStartedEvent>().ToList();
            Assert.AreEqual(2, boardings.Count);
            Assert.AreEqual(0, boardings[0].ChainDepth);
            Assert.AreEqual(C(4, 8), boardings[1].Cell);
            Assert.AreEqual(1, boardings[1].ChainDepth);
            CollectionAssert.AreEqual(new[] { 0, 2 }, boardings[1].Players);
            Assert.AreEqual(C(5, 8), s.P(0).Position);
            Assert.AreEqual(C(3, 8), s.P(2).Position);
        }

        [Test]
        public void TheChainStopsAtMaxAbbordaggioChain_R073a()
        {
            var config = new RulesConfig { maxAbbordaggioChain = 1 };
            RoundScenario s = Meet(3, config).At(2, 4, 8);
            s.Random.Enqueue(1, 5);
            List<GameEvent> events = s.PlayRound(new[] { Heading.E, Heading.O, Heading.S });

            Assert.AreEqual(1, events.OfType<BoardingStartedEvent>().Count());
            var limit = events.OfType<BoardingChainLimitEvent>().Single();
            Assert.AreEqual(C(4, 8), limit.Cell);
            Assert.AreEqual(config.maxAbbordaggioChain, limit.Limit);
            Assert.AreEqual(s.P(2).Position, s.P(0).Position, "le navi restano dove sono");
        }

        [Test]
        public void TheHelmsmanChoosesTheRepositionLastAfterSeeingTheOtherRoll_R073b()
        {
            RoundScenario s = Meet(2);
            // p1 (Timoniere, velocità 2) si ferma comunque in (4,7) per la collisione; perde la Pirateria per tenersi il Timoniere.
            s.Crew(1, s.AboveSlot(0), CrewCardId.Timoniere);
            s.Hand(1, PirateCardId.Bordata);
            s.Random.Enqueue(2);
            List<GameEvent> events = s.PlayRound(TwoHeadings, RoundScenario.Prefer((d, o) =>
                (o is DieValueOption v && v.Value == 4) || (o is BoardingLossOption l && l.PirateCards.Count > 0)));

            PendingDecision choice = s.Decisions.Single(d => d.Kind == DecisionKind.BoardingReposition);
            Assert.AreEqual(1, choice.Player);
            Assert.IsFalse(choice.IsSecret);
            Assert.AreEqual(HeadingExtensions.Count, choice.Options.Count);

            int rolled = events.FindIndex(e => e is DieRolledEvent r && r.Reason == DiceReason.BoardingReposition);
            int chosen = events.FindIndex(e => e is DieChosenEvent);
            Assert.Less(rolled, chosen, "sceglie dopo aver visto il tiro dell'altro");
            Assert.AreEqual(Neighbour(Meeting, 2), s.P(0).Position);
            Assert.AreEqual(Neighbour(Meeting, 4), s.P(1).Position);
        }

        [Test]
        public void WhoLacksCardsLosesWhatHeHasOrNothing_R073c()
        {
            // p0: nessuna crew e 1 sola carta Pirateria → perde quella (unica opzione, la applica il motore).
            // p1: nessuna carta → non perde nulla e non decide.
            RoundScenario s = Meet(2);
            s.Hand(0, PirateCardId.Bordata);
            s.Random.Enqueue(1, 5);
            List<GameEvent> events = s.PlayRound(TwoHeadings);

            Assert.IsFalse(s.Decisions.Any(d => d.Kind == DecisionKind.BoardingLoss));
            Assert.AreEqual(0, s.P(0).Hand.Count);
            Assert.AreEqual(1, events.OfType<CardsDiscardedEvent>().Single(e => e.Player == 0).Count);
            Assert.IsFalse(events.OfType<CardsDiscardedEvent>().Any(e => e.Player == 1));
            Assert.IsFalse(events.OfType<CrewLostEvent>().Any());
        }

        [Test]
        public void TheMedicoTakesTheThreatenedCardsPlaceAboveDeck_R073d_R023()
        {
            RoundScenario s = Meet(2);
            CrewCard gunner = s.Crew(0, s.AboveSlot(0), CrewCardId.Cannoniere);
            CrewCard medico = s.Crew(0, s.BelowSlot(0), CrewCardId.Medico);
            s.Random.Enqueue(1, 5);
            List<GameEvent> events = s.PlayRound(TwoHeadings, RoundScenario.Prefer((d, o) =>
                (o is BoardingLossOption l && l.CrewSlots.Contains(0)) || (o is MedicoOption m && m.Use)));

            PendingDecision ask = s.Decisions.Single(d => d.Kind == DecisionKind.UseMedico);
            Assert.AreEqual(0, ask.Player);
            Assert.IsTrue(ask.IsSecret);
            Assert.AreEqual(2, ask.Options.Count, "non usarlo, oppure usare l'unico Medico");

            Assert.AreSame(medico, s.P(0).Crew[s.AboveSlot(0)]);
            Assert.AreSame(gunner, s.P(0).Crew[s.BelowSlot(0)]);
            Assert.IsFalse(events.OfType<CrewLostEvent>().Any(), "non è una perdita (R-021)");
            Assert.AreEqual(1, events.OfType<MedicoUsedEvent>().Count());
        }

        [Test]
        public void TheMedicoCanBeDeclined_R023()
        {
            RoundScenario s = Meet(2);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Cannoniere);
            CrewCard medico = s.Crew(0, s.BelowSlot(0), CrewCardId.Medico);
            s.Random.Enqueue(1, 5);
            List<GameEvent> events = s.PlayRound(TwoHeadings, RoundScenario.Prefer((d, o) =>
                (o is BoardingLossOption l && l.CrewSlots.Contains(0)) || (o is MedicoOption m && !m.Use)));

            Assert.IsNull(s.P(0).Crew[s.AboveSlot(0)]);
            Assert.AreSame(medico, s.P(0).Crew[s.BelowSlot(0)]);
            Assert.AreEqual(1, events.OfType<CrewLostEvent>().Count());
        }

        [Test]
        public void BelowDeckLossesAreSecretToTheOthers_R010()
        {
            RoundScenario s = Meet(2);
            s.Crew(0, s.BelowSlot(1), CrewCardId.Cuoco);
            s.Random.Enqueue(1, 5);
            List<GameEvent> events = s.PlayRound(TwoHeadings);

            CrewLostEvent lost = events.OfType<CrewLostEvent>().Single();
            Assert.IsFalse(lost.Above);
            Assert.AreEqual(CrewCardId.Cuoco, lost.Card.Kind);
            var seen = (CrewLostEvent)lost.ViewFor(1);
            Assert.IsNull(seen.Card);
            Assert.IsTrue(seen.IsRedacted);
        }
    }
}
