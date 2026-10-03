using System;
using System.Linq;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.State;
using NUnit.Framework;

namespace hp55games.MareIgnoto.Rules.Tests
{
    public class RulesConfigTests
    {
        [Test]
        public void DefaultsAreValid()
        {
            Assert.IsEmpty(new RulesConfig().Validate());
        }

        [Test]
        public void DefaultsMatchTheRulebookNumbers()
        {
            var config = new RulesConfig();
            Assert.AreEqual(2, config.minPlayers);
            Assert.AreEqual(8, config.maxPlayers);
            Assert.AreEqual(2, config.slotsAbove);
            Assert.AreEqual(3, config.slotsBelow);
            Assert.AreEqual(2, config.startingCrewCards);
            Assert.AreEqual(3, config.startingPirateCards);
            Assert.AreEqual(3, config.startingMissionsDrawn);
            Assert.AreEqual(1, config.startingMissionsMinKept);
            Assert.AreEqual(10, config.startingCoins);
            Assert.AreEqual(0, config.maxRounds);
        }

        [Test]
        public void DecksHaveTheDocumentedSizes_R003_R004_R005()
        {
            var state = new GameState(new RulesConfig(), GameMap.Create(TestSupport.StandardMap(), new RulesConfig()));
            state.BuildDecks();

            Assert.AreEqual(54, state.CrewDeck.DrawCount);
            Assert.AreEqual(110, state.PirateDeck.DrawCount);
            Assert.AreEqual(34, state.CorsairDeck.DrawCount);

            var config = new RulesConfig();
            int weather = state.Pirate.DrawPile.Count(c => c.IsWeather);
            Assert.AreEqual(30, weather);
            Assert.AreEqual(80, state.PirateDeck.DrawCount - weather);
            Assert.AreEqual(config.PirateCopies(PirateCardId.Bordata), state.Pirate.DrawPile.Count(c => c.Id == PirateCardId.Bordata));
        }

        [Test]
        public void CrewDeckHas13RanksTimes4SuitsPlusJokers_R003()
        {
            var state = new GameState(new RulesConfig(), GameMap.Create(TestSupport.StandardMap(), new RulesConfig()));
            state.BuildDecks();
            var cards = state.Crew.DrawPile;

            Assert.AreEqual(2, cards.Count(c => c.IsJoker));
            for (int rank = 1; rank <= 13; rank++)
            {
                Assert.AreEqual(4, cards.Count(c => c.Rank == rank), "rango " + rank);
                Assert.AreEqual(4, cards.Where(c => c.Rank == rank).Select(c => c.Suit).Distinct().Count());
                Assert.AreEqual(CrewCatalog.KindForRank(rank), cards.First(c => c.Rank == rank).Kind);
            }
        }

        [Test]
        public void CardUidsAreUniqueAcrossDecks()
        {
            var state = new GameState(new RulesConfig(), GameMap.Create(TestSupport.StandardMap(), new RulesConfig()));
            state.BuildDecks();
            var uids = state.Crew.DrawPile.Select(c => c.Uid)
                .Concat(state.Pirate.DrawPile.Select(c => c.Uid))
                .Concat(state.Corsair.DrawPile.Select(c => c.Uid)).ToList();
            Assert.AreEqual(uids.Count, uids.Distinct().Count());
        }

        [Test]
        public void ChangingCopiesChangesTheDeck()
        {
            var config = new RulesConfig();
            config.missions.First(m => m.id == MissionId.Avido).copies = 5;
            var state = new GameState(config, GameMap.Create(TestSupport.StandardMap(), config));
            state.BuildDecks();
            Assert.AreEqual(5, state.Corsair.DrawPile.Count(c => c.Id == MissionId.Avido));
            Assert.AreEqual(37, state.CorsairDeck.DrawCount);
        }

        [Test]
        public void MeteoCostsAreInTheConfig_R121()
        {
            var config = new RulesConfig();
            Assert.AreEqual(2, config.PirateCost(PirateCardId.SupplicaGartya));
            Assert.AreEqual(5, config.PirateCost(PirateCardId.InvocazioneGartya));
            Assert.AreEqual(10, config.PirateCost(PirateCardId.IraGartya));
            Assert.AreEqual(2, config.PirateCost(PirateCardId.FavoreGartya));
            Assert.AreEqual(0, config.PirateCost(PirateCardId.VentoInPoppa));
            Assert.AreEqual(0, config.PirateCost(PirateCardId.RafficaCanaglia));
        }

        [Test]
        public void MissionRewardsAndPokerTableAreInTheConfig()
        {
            var config = new RulesConfig();
            Assert.AreEqual(3, config.Mission(MissionId.Avidissimo).reward);
            Assert.AreEqual(50, config.Mission(MissionId.Avidissimo).threshold);
            Assert.AreEqual(12, config.PokerTokens(PokerHand.RoyalStraightFlush));
            Assert.AreEqual(2, config.PokerTokens(PokerHand.ThreeOfAKind));
            Assert.AreEqual(2, config.PokerTokens(PokerHand.TwoPair));
        }

        [Test]
        public void ValidateReportsMissingDuplicateAndNegativeEntries()
        {
            var missing = new RulesConfig();
            missing.pirateCards.RemoveAll(e => e.id == PirateCardId.Spyglass);
            Assert.That(missing.Validate(), Has.Some.Contains("Spyglass"));

            var duplicate = new RulesConfig();
            duplicate.missions.Add(new MissionEntry(MissionId.Avido, 1, 1));
            Assert.That(duplicate.Validate(), Has.Some.Contains("duplicato"));

            var negative = new RulesConfig();
            negative.pirateCards[0].copies = -1;
            Assert.That(negative.Validate(), Has.Some.Contains("negativi"));
        }

        [Test]
        public void ValidateReportsInconsistentSetupNumbers()
        {
            Assert.IsNotEmpty(new RulesConfig { maxPlayers = 1 }.Validate());
            Assert.IsNotEmpty(new RulesConfig { startingCrewCards = 4 }.Validate());
            Assert.IsNotEmpty(new RulesConfig { startingMissionsMinKept = 4 }.Validate());
            Assert.IsNotEmpty(new RulesConfig { stormLevel = 1 }.Validate());
        }
    }

    public class CrewEffectsTests
    {
        private static CrewCard Card(CrewCardId kind, int rank, CrewSuit suit = CrewSuit.Hearts) =>
            new CrewCard(rank * 10, kind, rank, suit);

        private static CrewCard Joker() => new CrewCard(999, CrewCardId.Jolly, 0, CrewSuit.None);

        private static readonly CrewCard Timoniere = Card(CrewCardId.Timoniere, 8);
        private static readonly CrewCard Navigatore = Card(CrewCardId.Navigatore, 6);

        [Test]
        public void CopiesStack_R013()
        {
            var above = new[] { Timoniere, Card(CrewCardId.Timoniere, 8, CrewSuit.Spades) };
            Assert.AreEqual(2, CrewEffects.EffectiveCount(above, CrewCardId.Timoniere));
            Assert.AreEqual(0, CrewEffects.EffectiveCount(above, CrewCardId.Navigatore));
        }

        [Test]
        public void JokerDuplicatesTheOtherAboveCard_R015()
        {
            Assert.AreEqual(2, CrewEffects.EffectiveCount(new[] { Joker(), Timoniere }, CrewCardId.Timoniere));
            Assert.AreEqual(2, CrewEffects.EffectiveCount(new[] { Timoniere, Joker() }, CrewCardId.Timoniere));
            Assert.AreEqual(0, CrewEffects.EffectiveCount(new[] { Joker(), Timoniere }, CrewCardId.Navigatore));
        }

        [Test]
        public void JokerAloneOrWithAnotherJokerDoesNothing_R015()
        {
            Assert.AreEqual(0, CrewEffects.EffectiveCount(new[] { Joker(), null }, CrewCardId.Timoniere));
            Assert.AreEqual(0, CrewEffects.EffectiveCount(new[] { Joker(), Joker() }, CrewCardId.Timoniere));
            Assert.AreEqual(2, CrewEffects.EffectiveCount(new[] { Joker(), Joker() }, CrewCardId.Jolly));
        }

        [Test]
        public void JokerPlusNavigatorWorksAsTwoNavigators_R015_R085()
        {
            Assert.AreEqual(2, CrewEffects.EffectiveCount(new[] { Navigatore, Joker() }, CrewCardId.Navigatore));
        }

        [Test]
        public void MedicHasNoEffectAboveDeck_R011()
        {
            var medic = Card(CrewCardId.Medico, 5);
            Assert.AreEqual(0, CrewEffects.EffectiveCount(new[] { medic, null }, CrewCardId.Medico));
            Assert.AreEqual(0, CrewEffects.EffectiveCount(new[] { medic, Joker() }, CrewCardId.Medico));
            Assert.IsTrue(CrewCatalog.EffectIsBelowDeck(CrewCardId.Medico));
        }

        [Test]
        public void EmptySlotsCountNothing()
        {
            Assert.AreEqual(0, CrewEffects.EffectiveCount(new CrewCard[] { null, null }, CrewCardId.Mozzo));
        }

        [Test]
        public void TurnOrderValues_R014()
        {
            var config = new RulesConfig();
            Assert.AreEqual(1, CrewEffects.TurnOrderValue(Card(CrewCardId.Mozzo, 1), config));
            Assert.AreEqual(10, CrewEffects.TurnOrderValue(Card(CrewCardId.Quartiermastro, 10), config));
            Assert.AreEqual(11, CrewEffects.TurnOrderValue(Card(CrewCardId.Falconet, 11), config));
            Assert.AreEqual(12, CrewEffects.TurnOrderValue(Card(CrewCardId.Saker, 12), config));
            Assert.AreEqual(13, CrewEffects.TurnOrderValue(Card(CrewCardId.Culverin, 13), config));
            Assert.AreEqual(0, CrewEffects.TurnOrderValue(Joker(), config));
            Assert.AreEqual(0, CrewEffects.TurnOrderValue(null, config));
        }

        [Test]
        public void CommerceValues_R014()
        {
            var config = new RulesConfig();
            Assert.AreEqual(7, CrewEffects.CommerceValue(Card(CrewCardId.Cannoniere, 7), config));
            Assert.AreEqual(10, CrewEffects.CommerceValue(Card(CrewCardId.Falconet, 11), config));
            Assert.AreEqual(10, CrewEffects.CommerceValue(Card(CrewCardId.Saker, 12), config));
            Assert.AreEqual(10, CrewEffects.CommerceValue(Card(CrewCardId.Culverin, 13), config));
            Assert.AreEqual(0, CrewEffects.CommerceValue(Joker(), config));
            Assert.AreEqual(0, CrewEffects.CommerceValue(null, config));
        }

        [Test]
        public void ValuesFollowTheConfig()
        {
            var config = new RulesConfig { jokerTurnOrderValue = 3, courtCommerceValue = 9 };
            Assert.AreEqual(3, CrewEffects.TurnOrderValue(Joker(), config));
            Assert.AreEqual(9, CrewEffects.CommerceValue(Card(CrewCardId.Saker, 12), config));
        }

        [Test]
        public void KindForRankCoversAllRanks()
        {
            Assert.AreEqual(CrewCardId.Mozzo, CrewCatalog.KindForRank(1));
            Assert.AreEqual(CrewCardId.Quartiermastro, CrewCatalog.KindForRank(10));
            Assert.AreEqual(CrewCardId.Culverin, CrewCatalog.KindForRank(13));
            Assert.Throws<ArgumentOutOfRangeException>(() => CrewCatalog.KindForRank(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => CrewCatalog.KindForRank(14));
        }
    }
}
