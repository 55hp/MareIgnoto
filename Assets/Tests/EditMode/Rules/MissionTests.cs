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
    /// Missioni Corsaro (02_regole.md §8, 03_contenuti.md §3): ognuna completata e non completata, R-131 e R-132.
    /// Duelli: p0 in (5,7) attacca p1 in (6,7) con una Bordata! della mano, p1 non ha Parlè! e perde. Navi ferme.
    /// </summary>
    public class MissionTests
    {
        private static RoundScenario Duel(int players = 2, RulesConfig config = null)
        {
            RoundScenario s = Create(players, config).At(0, 5, 7).At(1, 6, 7);
            if (players > 2) s.At(2, 5, 8);
            s.Order(Enumerable.Range(0, players).ToArray());
            foreach (int id in Enumerable.Range(0, players)) s.P(id).Coins = 10;
            return s;
        }

        /// <summary>p0 vince una battaglia contro <paramref name="target"/> in questo round.</summary>
        private static List<GameEvent> Win(RoundScenario s, int target = 1)
        {
            s.Hand(0, PirateCardId.Bordata);
            return s.PlayTurns(Pick(Opt<AttackOption>(0, a => a.Target == target && a.Opening == AttackOpening.HandBroadside)));
        }

        private static int Threshold(RoundScenario s, MissionId id) => s.Config.Mission(id).threshold;

        private static int Reward(RoundScenario s, MissionId id) => s.Config.Mission(id).reward;

        private static bool Done(RoundScenario s, int player, MissionCard card) => s.P(player).Completed.Contains(card);

        private static MissionCompletedEvent Completed(List<GameEvent> events, MissionCard card) =>
            events.OfType<MissionCompletedEvent>().SingleOrDefault(e => e.Card == card);

        // ---- Barbanera! ----

        [Test]
        public void BarbaneraNeedsAWinAgainstEveryOpponentAndPaysPerOpponent()
        {
            RoundScenario s = Duel(3);
            MissionCard card = s.Mission(0, MissionId.Barbanera);
            Win(s, 1);
            Assert.IsFalse(Done(s, 0, card), "manca p2");

            List<GameEvent> events = Win(s, 2);
            Assert.IsTrue(Done(s, 0, card));
            Assert.AreEqual(Reward(s, MissionId.Barbanera) * 2, Completed(events, card).Reward, "2 avversari");
            Assert.AreEqual(Reward(s, MissionId.Barbanera) * 2, s.P(0).BountyFrom(BountyReason.Mission));
        }

        [Test]
        public void EventsBeforeTheMissionWasDrawnDoNotCount_R131()
        {
            RoundScenario s = Duel();
            Win(s);
            MissionCard card = s.Mission(0, MissionId.Barbanera);
            s.PlayTurns();
            Assert.IsFalse(Done(s, 0, card), "la vittoria del round 1 è precedente");

            Win(s);
            Assert.IsTrue(Done(s, 0, card));
        }

        // ---- Barbarossa! ----

        [Test]
        public void BarbarossaCompletesOnTheFifthWin()
        {
            RoundScenario s = Duel();
            MissionCard card = s.Mission(0, MissionId.Barbarossa);
            int wins = Threshold(s, MissionId.Barbarossa);
            for (int i = 1; i < wins; i++) Win(s);
            Assert.IsFalse(Done(s, 0, card));

            List<GameEvent> events = Win(s);
            Assert.IsTrue(Done(s, 0, card));
            Assert.AreEqual(Reward(s, MissionId.Barbarossa), Completed(events, card).Reward);
        }

        // ---- Olandese Volante (fine partita) ----

        [Test]
        public void OlandeseVolanteIsEvaluatedAtTheEndOnlyForWhoNeverLost_R132_R133()
        {
            RoundScenario s = Duel(2, new RulesConfig { maxRounds = 1 });
            MissionCard winner = s.Mission(0, MissionId.OlandeseVolante);
            MissionCard loser = s.Mission(1, MissionId.OlandeseVolante);
            List<GameEvent> events = Win(s);

            Assert.IsTrue(s.Session.IsOver);
            MissionCompletedEvent done = Completed(events, winner);
            Assert.IsTrue(done.AtGameEnd);
            Assert.AreEqual(Reward(s, MissionId.OlandeseVolante), done.Reward);
            Assert.IsNull(Completed(events, loser));
            Assert.AreEqual(1, s.Session.Result.ScoreOf(1).IncompleteMissions);
            Assert.AreEqual(s.Config.incompleteMissionPenalty, s.Session.Result.ScoreOf(1).MissionPenalty);
        }

        [Test]
        public void OlandeseVolanteHoldsWithZeroBattles()
        {
            RoundScenario s = Duel(2, new RulesConfig { maxRounds = 1 });
            MissionCard card = s.Mission(0, MissionId.OlandeseVolante);
            s.PlayTurns();
            Assert.IsTrue(Done(s, 0, card));
        }

        // ---- Maledizione pirata, Parlare con i pesci (perdite crew) ----

        /// <summary>Abbordaggio fortuito in (4,7): p0 da (3,7) verso E, p1 da (5,7) verso O; p0 può perdere solo crew.</summary>
        private static List<GameEvent> Boarding(RoundScenario s, params int[] rolls)
        {
            s.At(0, 3, 7).At(1, 5, 7).Order(0, 1);
            s.Random.Enqueue(rolls);
            return s.PlayRound(new[] { Heading.E, Heading.O });
        }

        [Test]
        public void MaledizionePirataNeedsTwoCrewLossesInTheSameRound()
        {
            RoundScenario s = Create(2);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Mozzo);
            s.Crew(0, s.BelowSlot(0), CrewCardId.Cuoco);
            MissionCard card = s.Mission(0, MissionId.MaledizionePirata);
            Boarding(s, 3, 3, 1, 5); // pari: il ciclo si ripete, due perdite
            Assert.IsTrue(Done(s, 0, card));
        }

        [Test]
        public void MaledizionePirataDoesNotAddLossesOfDifferentRounds()
        {
            RoundScenario s = Create(2);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Mozzo);
            s.Crew(0, s.BelowSlot(0), CrewCardId.Cuoco);
            MissionCard card = s.Mission(0, MissionId.MaledizionePirata);
            Boarding(s, 1, 5);
            Boarding(s, 1, 5);
            Assert.AreEqual(0, s.P(0).Crew.All().Count(), "due perdite, in due round");
            Assert.IsFalse(Done(s, 0, card));
        }

        [TestCase(3, true)]
        [TestCase(2, false)]
        public void ParlareConIPesciCountsCrewLostAtSea_R022(int losses, bool done)
        {
            RoundScenario s = Create(2);
            s.Crew(0, s.AboveSlot(0), CrewCardId.Mozzo);
            s.Crew(0, s.BelowSlot(0), CrewCardId.Cuoco);
            s.Crew(0, s.BelowSlot(1), CrewCardId.Vedetta);
            MissionCard card = s.Mission(0, MissionId.ParlareConIPesci);
            var rolls = new List<int>();
            for (int i = 1; i < losses; i++) rolls.AddRange(new[] { 3, 3 });
            rolls.AddRange(new[] { 1, 5 });
            List<GameEvent> events = Boarding(s, rolls.ToArray());

            Assert.AreEqual(losses, events.OfType<CrewLostEvent>().Count(e => e.Player == 0 && e.AtSea));
            Assert.AreEqual(done, Done(s, 0, card));
        }

        // ---- Nave Corsara (Bordate nello stesso round) ----

        [TestCase(4, true)]
        [TestCase(3, false)]
        public void NaveCorsaraCountsEveryBroadsideOfTheRoundFreeOnesIncluded(int handBroadsides, bool done)
        {
            // Col Falconet il primo colpo è gratuito: p0 spara 1 + le Bordate della mano, p1 risponde con 4.
            RoundScenario s = Duel();
            s.Crew(0, s.AboveSlot(0), CrewCardId.Falconet);
            MissionCard card = s.Mission(0, MissionId.NaveCorsara);
            for (int i = 0; i < handBroadsides; i++) s.Hand(0, PirateCardId.Bordata);
            for (int i = 0; i < 4; i++) s.Hand(1, PirateCardId.Bordata);
            List<GameEvent> events = s.PlayTurns(Pick(Opt<AttackOption>(0, a => a.Opening == AttackOpening.FalconetDuel)));

            Assert.AreEqual(1 + handBroadsides, events.OfType<CombatPlayEvent>().Count(e => e.Player == 0));
            Assert.AreEqual(done, Done(s, 0, card), "soglia " + Threshold(s, MissionId.NaveCorsara));
        }

        /// <summary>p0 da (3,4) entra nel porto dell'isola (4,4) e sceglie l'azione; in Svago resta lì nei round dopo.</summary>
        private static RoundScenario InPort() => Create(2).At(0, 3, 4).At(1, 16, 16).Order(0, 1);

        private static List<GameEvent> PortTurn(RoundScenario s, PortAction action) =>
            s.PlayRound(new[] { Heading.E, Heading.S }, Pick(Opt<PortActionOption>(0, a => a.Action == action)));

        // ---- Monete e mano: Avido, Avidissimo, Bancarotta, Dispersi in mare ----

        private static RoundScenario AtSea()
        {
            RoundScenario s = Create(2).At(0, 5, 7).At(1, 16, 16).Order(0, 1);
            s.P(0).Coins = 10;
            return s;
        }

        private static List<GameEvent> PlayCatch(RoundScenario s) =>
            s.PlayTurns(Pick(Opt<PlayCardOption>(0, o => o.Card.Id == PirateCardId.PescaFortunata)));

        [TestCase(MissionId.Avido, 0, true)]
        [TestCase(MissionId.Avido, -1, false)]
        [TestCase(MissionId.Avidissimo, 0, true)]
        [TestCase(MissionId.Avidissimo, -1, false)]
        public void AvidoAndAvidissimoCompleteWhenTheCoinsReachTheThreshold(MissionId id, int offset, bool done)
        {
            RoundScenario s = AtSea();
            s.P(0).Coins = Threshold(s, id) - s.Config.fortunateCatchCoins + offset;
            MissionCard card = s.Mission(0, id);
            s.Hand(0, PirateCardId.PescaFortunata);
            List<GameEvent> events = PlayCatch(s);

            Assert.AreEqual(done, Done(s, 0, card));
            if (done)
            {
                Assert.AreEqual(Reward(s, id), Completed(events, card).Reward);
                Assert.AreEqual(EventVisibility.Public, Completed(events, card).Visibility, "rivelata (R-132)");
                CollectionAssert.Contains(s.Session.State.Player(0).CompletedMissions, card);
            }
        }

        [Test]
        public void AStateConditionAlreadyTrueWhenTheMissionIsKeptCompletesAtOnce_R131()
        {
            RoundScenario s = AtSea();
            s.P(0).Coins = Threshold(s, MissionId.Avido);
            MissionCard card = s.Mission(0, MissionId.Avido);
            s.PlayTurns();
            Assert.IsTrue(Done(s, 0, card));
        }

        [TestCase(0, true)]
        [TestCase(1, false)]
        public void BancarottaCompletesAtZeroCoins(int coinsLeft, bool done)
        {
            RoundScenario s = InPort();
            s.P(0).Coins = s.Config.leisureCost + coinsLeft;
            MissionCard card = s.Mission(0, MissionId.Bancarotta);
            PortTurn(s, PortAction.Leisure);

            Assert.AreEqual(coinsLeft, s.P(0).Coins);
            Assert.AreEqual(done, Done(s, 0, card));
        }

        [TestCase(false, true)]
        [TestCase(true, false)]
        public void DispersiInMareCompletesWithAnEmptyHand(bool secondCard, bool done)
        {
            RoundScenario s = AtSea();
            MissionCard card = s.Mission(0, MissionId.DispersiInMare);
            s.Hand(0, PirateCardId.PescaFortunata);
            if (secondCard) s.Hand(0, PirateCardId.Bordata);
            PlayCatch(s);
            Assert.AreEqual(done, Done(s, 0, card));
        }

        // ---- Spugna di mare ----

        [TestCase(0, true)]
        [TestCase(-1, false)]
        public void SpugnaDiMareCountsLeisureActions(int offset, bool done)
        {
            RoundScenario s = InPort();
            int times = Threshold(s, MissionId.SpugnaDiMare) + offset;
            s.P(0).Coins = times * s.Config.leisureCost;
            MissionCard card = s.Mission(0, MissionId.SpugnaDiMare);
            for (int i = 0; i < times; i++) PortTurn(s, PortAction.Leisure);
            Assert.AreEqual(0, s.P(0).Coins, "tutti gli Svaghi pagati");
            Assert.AreEqual(done, Done(s, 0, card));
        }

        // ---- Cacciatore di Taglie ----

        [TestCase(2, true)]
        [TestCase(1, false)]
        public void CacciatoreDiTaglieCountsTheWholeGameAndChainsAfterAnotherMission(int completedBefore, bool done)
        {
            RoundScenario s = AtSea();
            // Missioni completate prima di pescarla: contano (eccezione a R-131).
            foreach (MissionCard old in s.State.Corsair.DrawPile.Where(c => c.Id == MissionId.Gemelli).Take(completedBefore).ToList())
            {
                s.State.Corsair.Remove(old);
                s.P(0).Completed.Add(old);
            }

            MissionCard hunter = s.Mission(0, MissionId.CacciatoreDiTaglie);
            MissionCard greedy = s.Mission(0, MissionId.Avido);
            s.P(0).Coins = Threshold(s, MissionId.Avido) - s.Config.fortunateCatchCoins;
            s.Hand(0, PirateCardId.PescaFortunata);
            List<GameEvent> events = PlayCatch(s);

            Assert.IsTrue(Done(s, 0, greedy));
            Assert.AreEqual(done, Done(s, 0, hunter), "soglia " + Threshold(s, MissionId.CacciatoreDiTaglie));
            if (done)
                Assert.Less(events.IndexOf(Completed(events, greedy)), events.IndexOf(Completed(events, hunter)));
        }

        // ---- Attaccabrighe ----

        [Test]
        public void AttaccabrigheNeedsBattlesInConsecutiveRoundsForAttackerAndDefender()
        {
            RoundScenario s = Duel();
            MissionCard attacker = s.Mission(0, MissionId.Attaccabrighe);
            MissionCard defender = s.Mission(1, MissionId.Attaccabrighe);
            int rounds = Threshold(s, MissionId.Attaccabrighe);
            for (int i = 1; i < rounds; i++) Win(s);
            Assert.IsFalse(Done(s, 0, attacker));

            Win(s);
            Assert.IsTrue(Done(s, 0, attacker));
            Assert.IsTrue(Done(s, 1, defender), "conta anche chi si difende");
        }

        [Test]
        public void AttaccabrigheResetsAfterARoundWithoutBattles()
        {
            RoundScenario s = Duel();
            MissionCard card = s.Mission(0, MissionId.Attaccabrighe);
            int rounds = Threshold(s, MissionId.Attaccabrighe);
            for (int i = 1; i < rounds; i++) Win(s);
            s.PlayTurns();
            Win(s);
            Assert.IsFalse(Done(s, 0, card));
        }

        [Test]
        public void AttaccabrigheDoesNotCountArrembaggio()
        {
            RoundScenario s = Duel();
            s.P(1).Coins = 0;
            MissionCard card = s.Mission(0, MissionId.Attaccabrighe);
            for (int i = 0; i < Threshold(s, MissionId.Attaccabrighe); i++)
            {
                s.Hand(0, PirateCardId.Arrembaggio);
                s.PlayTurns(Pick(Opt<AttackOption>(0, a => a.Opening == AttackOpening.Arrembaggio)));
            }

            Assert.IsFalse(Done(s, 0, card));
        }

        // ---- Lupo di mare ----

        [TestCase(false, true)]
        [TestCase(true, false)]
        public void LupoDiMareNeedsAWinWithNoCrewAboveDeck(bool crewAbove, bool done)
        {
            RoundScenario s = Duel();
            if (crewAbove) s.Crew(0, s.AboveSlot(0), CrewCardId.Mozzo);
            MissionCard card = s.Mission(0, MissionId.LupoDiMare);
            Win(s);
            Assert.AreEqual(done, Done(s, 0, card));
        }

        // ---- Gemelli, Nave d'assalto (stato della ciurma) ----

        [TestCase(CrewCardId.Mozzo, CrewCardId.Mozzo, true)]
        [TestCase(CrewCardId.Mozzo, CrewCardId.Cuoco, false)]
        public void GemelliNeedsTwoCardsOfTheSameRankAboveDeck(CrewCardId a, CrewCardId b, bool done)
        {
            RoundScenario s = AtSea();
            s.Crew(0, s.AboveSlot(0), a);
            s.Crew(0, s.AboveSlot(1), b);
            MissionCard card = s.Mission(0, MissionId.Gemelli);
            List<GameEvent> events = s.PlayTurns();
            Assert.AreEqual(done, Done(s, 0, card));
            if (done) Assert.AreEqual(Reward(s, MissionId.Gemelli), Completed(events, card).Reward);
        }

        [Test]
        public void GemelliWithTheTwoJokersPaysMore()
        {
            RoundScenario s = AtSea();
            s.Crew(0, s.AboveSlot(0), CrewCardId.Jolly);
            s.Crew(0, s.AboveSlot(1), CrewCardId.Jolly);
            MissionCard card = s.Mission(0, MissionId.Gemelli);
            List<GameEvent> events = s.PlayTurns();
            Assert.AreEqual(s.Config.twinsJokersReward, Completed(events, card).Reward);
        }

        [Test]
        public void GemelliCompletesAfterASwapInTheTurn()
        {
            RoundScenario s = AtSea();
            s.Crew(0, s.AboveSlot(0), CrewCardId.Mozzo);
            s.Crew(0, s.BelowSlot(0), CrewCardId.Mozzo);
            MissionCard card = s.Mission(0, MissionId.Gemelli);
            s.PlayTurns(Pick(Opt<SwapOption>(0, o => o.From == s.BelowSlot(0) && o.To == s.AboveSlot(1))));
            Assert.IsTrue(Done(s, 0, card));
        }

        [TestCase(CrewCardId.Falconet, CrewCardId.Culverin, true)]
        [TestCase(CrewCardId.Saker, CrewCardId.Saker, true)]
        [TestCase(CrewCardId.Falconet, CrewCardId.Jolly, false)]
        public void NaveDAssaltoNeedsTwoCourtCardsAboveDeck(CrewCardId a, CrewCardId b, bool done)
        {
            RoundScenario s = AtSea();
            s.Crew(0, s.AboveSlot(0), a);
            s.Crew(0, s.AboveSlot(1), b);
            MissionCard card = s.Mission(0, MissionId.NaveDAssalto);
            s.PlayTurns();
            Assert.AreEqual(done, Done(s, 0, card));
        }

        // ---- Gamba di legno ----

        [TestCase(true, true)]
        [TestCase(false, false)]
        public void GambaDiLegnoNeedsThreeStrandingsOnDifferentBorderCells_R069(bool differentCells, bool done)
        {
            RoundScenario s = Create(2).At(1, 16, 16).Order(0, 1);
            MissionCard card = s.Mission(0, MissionId.GambaDiLegno);
            int times = Threshold(s, MissionId.GambaDiLegno);
            for (int i = 0; i < times; i++)
            {
                s.At(0, 1, differentCells ? 5 + 3 * i : 5);
                s.PlayRound(new[] { Heading.O, Heading.S });
            }

            Assert.AreEqual(done, Done(s, 0, card));
        }

        // ---- Missione dal porto ----

        [Test]
        public void AMissionKeptAtThePortIsTrackedFromThen_R093_R131()
        {
            RoundScenario s = InPort();
            PortTurn(s, PortAction.Mission);
            Assert.Greater(s.P(0).Missions.Count + s.P(0).Completed.Count, 0);
            foreach (MissionCard card in s.P(0).Missions)
                Assert.IsTrue(s.State.MissionProgress.ContainsKey(card.Uid));
        }
    }
}
