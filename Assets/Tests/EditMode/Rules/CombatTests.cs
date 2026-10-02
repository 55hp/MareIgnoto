using System;
using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Decisions;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Map;
using NUnit.Framework;
using static hp55games.MareIgnoto.Rules.Tests.RoundScenario;

namespace hp55games.MareIgnoto.Rules.Tests
{
    /// <summary>
    /// Attacco navale (02_regole.md §7.2). p0 in (5,7) attacca p1 in (6,7) (distanza 1) nel proprio turno; le navi
    /// restano ferme (<see cref="RoundScenario.PlayTurns"/>). Monete fissate a 10 per entrambi.
    /// </summary>
    public class CombatTests
    {
        private static RoundScenario Duel(int defenderX = 6, int defenderY = 7)
        {
            RoundScenario s = Create(2).At(0, 5, 7).At(1, defenderX, defenderY).Order(0, 1);
            s.P(0).Coins = 10;
            s.P(1).Coins = 10;
            return s;
        }

        private static Func<PendingDecision, DecisionOption, bool> AttackWith(AttackOpening opening) =>
            Opt<AttackOption>(0, a => a.Target == 1 && a.Opening == opening);

        private static Func<PendingDecision, DecisionOption, bool> Respond(int player, CombatPlay play) =>
            Opt<CombatResponseOption>(player, r => !r.Yield && r.Play == play);

        private static Func<PendingDecision, DecisionOption, bool> GiveUp(int player) =>
            Opt<CombatResponseOption>(player, r => r.Yield);

        private static List<CombatPlayEvent> Plays(List<GameEvent> events) => events.OfType<CombatPlayEvent>().ToList();

        [Test]
        public void TheDuelGoesBackAndForthUntilSomeoneCannotAnswer_R102_R103_R104()
        {
            RoundScenario s = Duel();
            s.Hand(0, PirateCardId.Bordata, PirateCardId.Bordata);
            s.Hand(1, PirateCardId.Parle);
            s.Random.Enqueue(4);
            List<GameEvent> events = s.PlayTurns(Pick(AttackWith(AttackOpening.HandBroadside)));

            CollectionAssert.AreEqual(new[] { CombatPlay.Broadside, CombatPlay.Parle, CombatPlay.Broadside }, Plays(events).Select(p => p.Play));
            CollectionAssert.AreEqual(new[] { 0, 1, 0 }, Plays(events).Select(p => p.Player));
            Assert.AreEqual(1, events.OfType<CombatYieldedEvent>().Single().Player);

            var won = events.OfType<BattleWonEvent>().Single();
            Assert.AreEqual(0, won.Winner);
            Assert.AreEqual(s.Config.battleWinTokens, won.Tokens);
            Assert.AreEqual(4, won.Coins, "1d8 monete dal perdente");
            Assert.AreEqual(s.Config.battleWinTokens, s.P(0).BountyTokens);
            Assert.AreEqual(14, s.P(0).Coins);
            Assert.AreEqual(6, s.P(1).Coins);
            Assert.AreEqual(0, s.P(0).Hand.Count + s.P(1).Hand.Count, "le carte giocate sono negli scarti");

            PendingDecision response = s.Decisions.First(d => d.Kind == DecisionKind.CombatResponse);
            Assert.AreEqual(1, response.Player, "risponde il difensore");
            Assert.IsTrue(response.IsSecret);
        }

        [Test]
        public void TheLootIsCappedByTheLosersCoins_R104()
        {
            RoundScenario s = Duel();
            s.P(1).Coins = 2;
            s.Hand(0, PirateCardId.Bordata);
            s.Random.Enqueue(7);
            List<GameEvent> events = s.PlayTurns(Pick(AttackWith(AttackOpening.HandBroadside)));

            Assert.AreEqual(2, events.OfType<BattleWonEvent>().Single().Coins);
            Assert.AreEqual(0, s.P(1).Coins);
        }

        [Test]
        public void TheAttackerLosesWhenHeCannotAnswerAParle_R103_R104()
        {
            RoundScenario s = Duel();
            s.Hand(0, PirateCardId.Bordata);
            s.Hand(1, PirateCardId.Parle);
            s.Random.Enqueue(3);
            List<GameEvent> events = s.PlayTurns(Pick(AttackWith(AttackOpening.HandBroadside)));

            var won = events.OfType<BattleWonEvent>().Single();
            Assert.AreEqual(1, won.Winner, "vince il difensore");
            Assert.AreEqual(0, won.Attacker);
            Assert.AreEqual(s.Config.battleWinTokens, s.P(1).BountyTokens);
            Assert.AreEqual(7, s.P(0).Coins);
        }

        [Test]
        public void OpeningWithAHandCardUsesTheTurnCard_R112()
        {
            RoundScenario s = Duel();
            s.Hand(0, PirateCardId.Bordata, PirateCardId.PescaFortunata);
            s.Random.Enqueue(1);
            List<GameEvent> events = s.PlayTurns(Pick(AttackWith(AttackOpening.HandBroadside)));

            // Dopo l'attacco resta solo "fine turno", che il motore applica da solo: la Pesca Fortunata! resta in mano.
            Assert.AreEqual(1, s.DecisionsOf(DecisionKind.SeaAction, 0).Count);
            Assert.AreEqual(PirateCardId.PescaFortunata, s.P(0).Hand.Single().Id, "la carta del turno è stata usata dall'apertura");
            Assert.IsTrue(events.OfType<CardPlayedEvent>().Single().CountsAsTurnCard);
        }

        [Test]
        public void TheBuccaneerLeavesACardAfterAHandOpening_R112()
        {
            RoundScenario s = Duel();
            s.Crew(0, s.AboveSlot(0), CrewCardId.Bucaniere);
            s.Hand(0, PirateCardId.Bordata, PirateCardId.PescaFortunata);
            s.Random.Enqueue(1);
            s.PlayTurns(Pick(AttackWith(AttackOpening.HandBroadside)));
            Assert.IsTrue(s.DecisionsOf(DecisionKind.SeaAction, 0).Last().Options.OfType<PlayCardOption>().Any());
        }

        [Test]
        public void ArrembaggioAlsoUsesTheTurnCard_R112()
        {
            RoundScenario s = Duel();
            s.Hand(0, PirateCardId.Arrembaggio, PirateCardId.PescaFortunata);
            List<GameEvent> events = s.PlayTurns(Pick(AttackWith(AttackOpening.Arrembaggio),
                Opt<BoardingDefenseOption>(1, o => o.Defense == BoardingDefense.None)));

            Assert.IsTrue(events.OfType<CardPlayedEvent>().Single(c => c.Card.Id == PirateCardId.Arrembaggio).CountsAsTurnCard);
            Assert.AreEqual(PirateCardId.PescaFortunata, s.P(0).Hand.Single().Id);
            Assert.IsFalse(s.DecisionsOf(DecisionKind.SeaAction, 0).Skip(1).Any(d => d.Options.OfType<PlayCardOption>().Any()));
        }

        [Test]
        public void TheGunnersFreeBroadsideOpensWithoutUsingTheTurnCard_R105_R112()
        {
            RoundScenario s = Duel();
            s.Crew(0, s.AboveSlot(0), CrewCardId.Cannoniere);
            s.Hand(0, PirateCardId.PescaFortunata);
            s.Random.Enqueue(2);
            List<GameEvent> events = s.PlayTurns(Pick(AttackWith(AttackOpening.FreeBroadside)));

            Assert.AreEqual(CombatPlay.FreeBroadside, Plays(events).First().Play);
            Assert.AreEqual(0, events.OfType<BattleWonEvent>().Single().Winner);
            Assert.IsTrue(s.DecisionsOf(DecisionKind.SeaAction, 0).Last().Options.OfType<PlayCardOption>().Any());
        }

        [Test]
        public void TheGunnerHasOneFreeBroadsidePerCombat_R105()
        {
            // Aperta la battaglia con la gratuita, p0 non ne ha altre: contro la Parlè! perde.
            RoundScenario s = Duel();
            s.Crew(0, s.AboveSlot(0), CrewCardId.Cannoniere);
            s.Hand(1, PirateCardId.Parle);
            s.Random.Enqueue(2);
            List<GameEvent> events = s.PlayTurns(Pick(AttackWith(AttackOpening.FreeBroadside)));

            Assert.AreEqual(1, events.OfType<BattleWonEvent>().Single().Winner);
            Assert.AreEqual(1, Plays(events).Count(p => p.Play == CombatPlay.FreeBroadside));
        }

        [Test]
        public void TheGunnersFreeBroadsideIsRenewedEveryCombat_R105()
        {
            RoundScenario s = Duel();
            s.Crew(0, s.AboveSlot(0), CrewCardId.Cannoniere);
            s.Random.Enqueue(1, 1);
            s.PlayTurns(Pick(AttackWith(AttackOpening.FreeBroadside)));
            List<GameEvent> round2 = s.PlayTurns(Pick(AttackWith(AttackOpening.FreeBroadside)));
            Assert.AreEqual(0, round2.OfType<BattleWonEvent>().Single().Winner);
        }

        [Test]
        public void JokerAndGunnerGiveTwoFreeBroadsides_R105_R015()
        {
            RoundScenario s = Duel();
            s.Crew(0, s.AboveSlot(0), CrewCardId.Cannoniere);
            s.Crew(0, s.AboveSlot(1), CrewCardId.Jolly);
            s.Hand(1, PirateCardId.Parle);
            s.Random.Enqueue(2);
            List<GameEvent> events = s.PlayTurns(Pick(AttackWith(AttackOpening.FreeBroadside), Respond(0, CombatPlay.FreeBroadside)));

            Assert.AreEqual(2, Plays(events).Count(p => p.Player == 0 && p.Play == CombatPlay.FreeBroadside));
            Assert.AreEqual(0, events.OfType<BattleWonEvent>().Single().Winner);
        }

        [Test]
        public void FalconetMakesEveryBroadsideAttackAMandatoryDuel_R106()
        {
            // Duello: primo colpo gratis, poi solo Bordate (il difensore non può usare Parlè!).
            RoundScenario s = Duel();
            s.Crew(0, s.AboveSlot(0), CrewCardId.Falconet);
            s.Hand(0, PirateCardId.Bordata, PirateCardId.PescaFortunata);
            s.Hand(1, PirateCardId.Parle, PirateCardId.Bordata);
            s.Random.Enqueue(5);
            List<GameEvent> events = s.PlayTurns(Pick(AttackWith(AttackOpening.FalconetDuel)));

            PendingDecision firstTurn = s.DecisionsOf(DecisionKind.SeaAction, 0)[0];
            CollectionAssert.AreEquivalent(new[] { AttackOpening.FalconetDuel },
                firstTurn.Options.OfType<AttackOption>().Select(a => a.Opening), "con il Falconet niente apertura dalla mano");

            PendingDecision defense = s.DecisionsOf(DecisionKind.CombatResponse, 1)[0];
            Assert.IsFalse(defense.Options.Cast<CombatResponseOption>().Any(o => o.Play == CombatPlay.Parle && !o.Yield));
            CollectionAssert.AreEqual(new[] { CombatPlay.FreeBroadside, CombatPlay.Broadside, CombatPlay.Broadside },
                Plays(events).Select(p => p.Play));
            Assert.AreEqual(0, events.OfType<BattleWonEvent>().Single().Winner);
            Assert.IsTrue(s.DecisionsOf(DecisionKind.SeaAction, 0).Last().Options.OfType<PlayCardOption>().Any(), "primo colpo gratuito: carta del turno ancora disponibile (R-112)");
        }

        [Test]
        public void TheDefendersGunnerAnswersInAFalconetDuel_R105_R106()
        {
            RoundScenario s = Duel();
            s.Crew(0, s.AboveSlot(0), CrewCardId.Falconet);
            s.Crew(1, s.AboveSlot(0), CrewCardId.Cannoniere);
            s.Random.Enqueue(5);
            List<GameEvent> events = s.PlayTurns(Pick(AttackWith(AttackOpening.FalconetDuel), Respond(1, CombatPlay.FreeBroadside)));

            Assert.AreEqual(CombatPlay.FreeBroadside, Plays(events).Single(p => p.Player == 1).Play);
            Assert.AreEqual(1, events.OfType<BattleWonEvent>().Single().Winner, "p0 non ha Bordate per rispondere");
        }

        [TestCase(CrewCardId.Falconet, 1)]
        [TestCase(CrewCardId.Saker, 1)]
        [TestCase(CrewCardId.Culverin, 2)]
        public void RangeCardsExtendTheRangeAndTheJokerDoublesIt_R100_R015(CrewCardId card, int bonus)
        {
            int baseRange = new RulesConfig().baseRange;
            RoundScenario plain = Duel(5 + baseRange + bonus, 7);
            plain.Crew(0, plain.AboveSlot(0), card);
            plain.Hand(0, PirateCardId.Bordata);
            plain.PlayTurns();
            Assert.IsTrue(plain.DecisionsOf(DecisionKind.SeaAction, 0)[0].Options.OfType<AttackOption>().Any(), "a tiro con " + card);

            RoundScenario far = Duel(5 + baseRange + 2 * bonus, 7);
            far.Crew(0, far.AboveSlot(0), card);
            far.Hand(0, PirateCardId.Bordata);
            far.PlayTurns();
            Assert.IsFalse(far.DecisionsOf(DecisionKind.SeaAction, 0)[0].Options.OfType<AttackOption>().Any(), "fuori tiro senza Jolly");

            RoundScenario joker = Duel(5 + baseRange + 2 * bonus, 7);
            joker.Crew(0, joker.AboveSlot(0), card);
            joker.Crew(0, joker.AboveSlot(1), CrewCardId.Jolly);
            joker.Hand(0, PirateCardId.Bordata);
            joker.PlayTurns();
            Assert.IsTrue(joker.DecisionsOf(DecisionKind.SeaAction, 0)[0].Options.OfType<AttackOption>().Any(), "il Jolly raddoppia la gittata di " + card);
        }

        [Test]
        public void RangeIsChebyshevAndBaseOne_R100()
        {
            RoundScenario diagonal = Duel(6, 8);
            diagonal.Hand(0, PirateCardId.Bordata);
            diagonal.PlayTurns();
            Assert.IsTrue(diagonal.DecisionsOf(DecisionKind.SeaAction, 0)[0].Options.OfType<AttackOption>().Any());

            RoundScenario far = Duel(7, 7);
            far.Hand(0, PirateCardId.Bordata);
            far.PlayTurns();
            Assert.IsFalse(far.DecisionsOf(DecisionKind.SeaAction, 0)[0].Options.OfType<AttackOption>().Any());
        }

        [Test]
        public void ShipsOnIslandsOrTheBorderCannotBeAttacked_R101()
        {
            // p1 resta sull'isola 0 ((5,4) → O → (4,4)); p2 resta sulla cornice ((0,6) verso O). p0 in (5,5) e (1,6) non li vede.
            RoundScenario s = Create(3).At(0, 5, 5).At(1, 5, 4).At(2, 0, 6).Order(0, 1, 2);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Culverin);
            s.Crew(0, s.AboveSlot(1), CrewCardId.Jolly);
            s.Hand(0, PirateCardId.Bordata, PirateCardId.Arrembaggio);
            s.PlayRound(new[] { Heading.S, Heading.O, Heading.O });

            Assert.AreEqual(new Coord(4, 4), s.P(1).Position);
            Assert.IsFalse(s.DecisionsOf(DecisionKind.SeaAction, 0)[0].Options.OfType<AttackOption>().Any());
        }

        [Test]
        public void SakerDoublesTheGainAgainstADefenderWithoutParle_R107()
        {
            RoundScenario s = Duel();
            s.Crew(0, s.AboveSlot(0), CrewCardId.Saker);
            s.Hand(0, PirateCardId.Bordata);
            s.Random.Enqueue(3);
            List<GameEvent> events = s.PlayTurns(Pick(AttackWith(AttackOpening.HandBroadside)));

            var won = events.OfType<BattleWonEvent>().Single();
            Assert.AreEqual(s.Config.sakerGainMultiplier, won.SakerFactor);
            Assert.AreEqual(s.Config.battleWinTokens * s.Config.sakerGainMultiplier, won.Tokens);
            Assert.AreEqual(3 * s.Config.sakerGainMultiplier, won.Coins);
        }

        [Test]
        public void SakerDoesNotDoubleIfTheDefenderPlayedParle_R107()
        {
            RoundScenario s = Duel();
            s.Crew(0, s.AboveSlot(0), CrewCardId.Saker);
            s.Hand(0, PirateCardId.Bordata, PirateCardId.Bordata);
            s.Hand(1, PirateCardId.Parle);
            s.Random.Enqueue(3);
            List<GameEvent> events = s.PlayTurns(Pick(AttackWith(AttackOpening.HandBroadside)));

            var won = events.OfType<BattleWonEvent>().Single();
            Assert.AreEqual(0, won.Winner);
            Assert.AreEqual(1, won.SakerFactor);
            Assert.AreEqual(3, won.Coins);
        }

        [Test]
        public void JokerAndSakerDoubleTwice_R107_R015()
        {
            RoundScenario s = Duel();
            s.P(1).Coins = 50;
            s.Crew(0, s.AboveSlot(0), CrewCardId.Saker);
            s.Crew(0, s.AboveSlot(1), CrewCardId.Jolly);
            s.Hand(0, PirateCardId.Bordata);
            s.Random.Enqueue(3);
            List<GameEvent> events = s.PlayTurns(Pick(AttackWith(AttackOpening.HandBroadside)));

            int factor = s.Config.sakerGainMultiplier * s.Config.sakerGainMultiplier;
            var won = events.OfType<BattleWonEvent>().Single();
            Assert.AreEqual(factor, won.SakerFactor);
            Assert.AreEqual(3 * factor, won.Coins);
        }

        [Test]
        public void CulverinFromTwoCellsForbidsParle_R108()
        {
            RoundScenario s = Duel(7, 7);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Culverin);
            s.Hand(0, PirateCardId.Bordata);
            s.Hand(1, PirateCardId.Parle);
            s.Random.Enqueue(2);
            List<GameEvent> events = s.PlayTurns(Pick(AttackWith(AttackOpening.HandBroadside)));

            Assert.IsTrue(events.OfType<AttackStartedEvent>().Single().ParleForbidden);
            Assert.AreEqual(0, events.OfType<BattleWonEvent>().Single().Winner);
            Assert.AreEqual(1, s.P(1).Hand.Count, "la Parlè! resta in mano");
        }

        [Test]
        public void CulverinFromOneCellAllowsParle_R108()
        {
            RoundScenario s = Duel();
            s.Crew(0, s.AboveSlot(0), CrewCardId.Culverin);
            s.Hand(0, PirateCardId.Bordata);
            s.Hand(1, PirateCardId.Parle);
            s.Random.Enqueue(2);
            List<GameEvent> events = s.PlayTurns(Pick(AttackWith(AttackOpening.HandBroadside)));

            Assert.IsFalse(events.OfType<AttackStartedEvent>().Single().ParleForbidden);
            Assert.AreEqual(1, events.OfType<BattleWonEvent>().Single().Winner);
        }

        [Test]
        public void TheWinnersNostromoStealsARandomPirateCard_R109()
        {
            RoundScenario s = Duel();
            s.Crew(0, s.AboveSlot(0), CrewCardId.Nostromo);
            s.Hand(0, PirateCardId.Bordata);
            s.Hand(1, PirateCardId.PescaFortunata, PirateCardId.Spyglass);
            s.Random.Enqueue(1);
            List<GameEvent> events = s.PlayTurns(Pick(AttackWith(AttackOpening.HandBroadside)));

            CardStolenEvent stolen = events.OfType<CardStolenEvent>().Single();
            Assert.AreEqual(0, stolen.Thief);
            Assert.AreEqual(1, s.P(1).Hand.Count);
            Assert.IsTrue(s.P(0).Hand.Contains(stolen.Card));
            Assert.IsNull(((CardStolenEvent)stolen.ViewFor(1)).Card, "la carta la vede solo il ladro");
        }

        [Test]
        public void JokerAndNostromoStealTwoCards_R109_R015()
        {
            RoundScenario s = Duel();
            s.Crew(0, s.AboveSlot(0), CrewCardId.Nostromo);
            s.Crew(0, s.AboveSlot(1), CrewCardId.Jolly);
            s.Hand(0, PirateCardId.Bordata);
            s.Hand(1, PirateCardId.PescaFortunata, PirateCardId.Spyglass, PirateCardId.UomoInMare);
            s.Random.Enqueue(1);
            List<GameEvent> events = s.PlayTurns(Pick(AttackWith(AttackOpening.HandBroadside)));
            Assert.AreEqual(2, events.OfType<CardStolenEvent>().Count());
            Assert.AreEqual(1, s.P(1).Hand.Count);
        }

        [Test]
        public void ArrembaggioCanBeParried_R110()
        {
            RoundScenario s = Duel();
            s.Crew(1, s.AboveSlot(0), CrewCardId.Cannoniere);
            s.Hand(0, PirateCardId.Arrembaggio);
            s.Hand(1, PirateCardId.Parle);
            List<GameEvent> events = s.PlayTurns(Pick(AttackWith(AttackOpening.Arrembaggio),
                Opt<BoardingDefenseOption>(1, o => o.Defense == BoardingDefense.Parle)));

            PendingDecision defense = s.DecisionsOf(DecisionKind.BoardingDefense, 1).Single();
            Assert.IsTrue(defense.IsSecret);
            CollectionAssert.AreEquivalent(new[] { BoardingDefense.Parle, BoardingDefense.Paid, BoardingDefense.None },
                defense.Options.Cast<BoardingDefenseOption>().Select(o => o.Defense));
            Assert.IsNotNull(s.P(1).Crew[s.AboveSlot(0)]);
            Assert.IsFalse(events.OfType<BattleWonEvent>().Any(), "Arrembaggio! non è una battaglia vinta");
            Assert.AreEqual(0, s.P(1).Hand.Count);
        }

        [Test]
        public void ArrembaggioCanBeBoughtOff_R110()
        {
            RoundScenario s = Duel();
            s.Crew(1, s.AboveSlot(0), CrewCardId.Cannoniere);
            s.Hand(0, PirateCardId.Arrembaggio);
            List<GameEvent> events = s.PlayTurns(Pick(AttackWith(AttackOpening.Arrembaggio),
                Opt<BoardingDefenseOption>(1, o => o.Defense == BoardingDefense.Paid)));

            Assert.AreEqual(10 - s.Config.boardingParryCoins, s.P(1).Coins);
            Assert.AreEqual(10 + s.Config.boardingParryCoins, s.P(0).Coins);
            Assert.IsNotNull(s.P(1).Crew[s.AboveSlot(0)]);
            Assert.AreEqual(0, s.P(0).BountyTokens);
        }

        [Test]
        public void ArrembaggioTakesAnAboveDeckCrewChosenByTheAttacker_R110_R020()
        {
            RoundScenario s = Duel();
            s.P(1).Coins = s.Config.boardingParryCoins - 1; // non può pagare
            CrewCard gunner = s.Crew(1, s.AboveSlot(0), CrewCardId.Cannoniere);
            s.Crew(1, s.AboveSlot(1), CrewCardId.Mozzo);
            s.Hand(0, PirateCardId.Arrembaggio);
            List<GameEvent> events = s.PlayTurns(Pick(AttackWith(AttackOpening.Arrembaggio),
                Opt<CrewSlotOption>(0, o => o.Slot == 0), Opt<ReceiveCrewOption>(0, o => o.Slot == s.BelowSlot(1))));

            PendingDecision steal = s.DecisionsOf(DecisionKind.StealCrew, 0).Single();
            Assert.IsFalse(steal.IsSecret);
            Assert.AreEqual(2, steal.Options.Count, "solo sopra coperta");
            Assert.AreEqual(CrewLossCause.Arrembaggio, events.OfType<CrewLostEvent>().Single().Cause);
            Assert.IsNull(s.P(1).Crew[s.AboveSlot(0)]);
            Assert.AreSame(gunner, s.P(0).Crew[s.BelowSlot(1)], "la riceve dove sceglie (R-020)");
            Assert.AreEqual(0, s.P(0).BountyTokens);
        }

        [Test]
        public void ArrembaggioIsBlockedByTheDefendersMedico_R110_R023()
        {
            RoundScenario s = Duel();
            s.P(1).Coins = 0;
            s.Crew(1, s.AboveSlot(0), CrewCardId.Cannoniere);
            CrewCard medico = s.Crew(1, s.BelowSlot(0), CrewCardId.Medico);
            s.Hand(0, PirateCardId.Arrembaggio);
            List<GameEvent> events = s.PlayTurns(Pick(AttackWith(AttackOpening.Arrembaggio), Opt<MedicoOption>(1, o => o.Use)));

            Assert.AreSame(medico, s.P(1).Crew[s.AboveSlot(0)]);
            Assert.IsFalse(events.OfType<CrewLostEvent>().Any());
            Assert.IsTrue(s.P(0).Crew.All().Count() == 0, "l'attaccante non riceve nulla");
        }

        [Test]
        public void CulverinFromAfarForbidsParleAgainstArrembaggio_R108_R110()
        {
            RoundScenario s = Duel(7, 7);
            s.P(1).Coins = 0;
            s.Crew(0, s.AboveSlot(0), CrewCardId.Culverin);
            s.Crew(1, s.AboveSlot(0), CrewCardId.Mozzo);
            s.Hand(0, PirateCardId.Arrembaggio);
            s.Hand(1, PirateCardId.Parle);
            s.PlayTurns(Pick(AttackWith(AttackOpening.Arrembaggio)));

            Assert.IsFalse(s.DecisionsOf(DecisionKind.BoardingDefense, 1).Any(), "unica difesa possibile: nessuna (applicata dal motore)");
            Assert.IsNull(s.P(1).Crew[s.AboveSlot(0)]);
        }

        [Test]
        public void QuartermasterSwapsAboveDeckCardsInsteadOfAttacking_R111()
        {
            RoundScenario s = Duel();
            s.Crew(0, s.AboveSlot(0), CrewCardId.Quartiermastro);
            CrewCard mozzo = s.Crew(0, s.AboveSlot(1), CrewCardId.Mozzo);
            CrewCard helm = s.Crew(1, s.AboveSlot(0), CrewCardId.Timoniere);
            s.Hand(0, PirateCardId.Bordata, PirateCardId.Bordata);
            List<GameEvent> events = s.PlayTurns(Pick(Opt<QuartermasterOption>(0),
                Opt<QuartermasterSwapOption>(0, o => o.OwnSlot == 1 && o.TargetSlot == 0)));

            Assert.AreSame(helm, s.P(0).Crew[s.AboveSlot(1)]);
            Assert.AreSame(mozzo, s.P(1).Crew[s.AboveSlot(0)]);
            Assert.AreEqual(1, s.P(0).Hand.Count, "una Bordata! scartata");
            Assert.IsFalse(events.OfType<CardPlayedEvent>().Single().CountsAsTurnCard);
            Assert.IsFalse(events.OfType<CrewLostEvent>().Any(), "uno scambio non è una perdita (R-021)");
            Assert.IsFalse(events.OfType<AttackStartedEvent>().Any());
            Assert.IsFalse(s.DecisionsOf(DecisionKind.SeaAction, 0).Last().Options.OfType<AttackOption>().Any(), "niente battaglia nello stesso turno");
            Assert.IsFalse(s.DecisionsOf(DecisionKind.QuartermasterSwap, 0).Single().IsSecret);
        }

        [Test]
        public void QuartermasterNeedsABroadsideAndWorksWithTheJoker_R111_R015()
        {
            RoundScenario none = Duel();
            none.Crew(0, none.AboveSlot(0), CrewCardId.Quartiermastro);
            none.Crew(1, none.AboveSlot(0), CrewCardId.Timoniere);
            none.PlayTurns();
            Assert.IsFalse(none.DecisionsOf(DecisionKind.SeaAction, 0)[0].Options.OfType<QuartermasterOption>().Any());

            RoundScenario joker = Duel();
            joker.Crew(0, joker.AboveSlot(0), CrewCardId.Quartiermastro);
            joker.Crew(0, joker.AboveSlot(1), CrewCardId.Jolly);
            joker.Crew(1, joker.AboveSlot(0), CrewCardId.Timoniere);
            joker.Hand(0, PirateCardId.Bordata, PirateCardId.Bordata);
            joker.PlayTurns(Pick(Opt<QuartermasterOption>(0)));
            Assert.AreEqual(1, joker.Decisions.Count(d => d.Kind == DecisionKind.QuartermasterSwap), "una volta per turno anche col Jolly");
        }
    }
}
