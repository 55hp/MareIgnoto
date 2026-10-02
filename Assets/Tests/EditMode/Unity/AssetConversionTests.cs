using System.Linq;
using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace hp55games.MareIgnoto.Unity.Tests
{
    public class AssetConversionTests
    {
        private static T Create<T>() where T : ScriptableObject => ScriptableObject.CreateInstance<T>();

        [Test]
        public void NewRulesConfigAssetCarriesTheDefaults()
        {
            var asset = Create<RulesConfigAsset>();
            try
            {
                RulesConfig config = asset.ToConfig();
                Assert.IsEmpty(config.Validate());
                Assert.AreEqual(JsonUtility.ToJson(new RulesConfig()), JsonUtility.ToJson(config));
                Assert.AreEqual(13, config.pirateCards.Count);
                Assert.AreEqual(17, config.missions.Count);
                Assert.AreEqual(10, config.pokerScores.Count);
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void ToConfigReturnsAnIndependentCopy()
        {
            var asset = Create<RulesConfigAsset>();
            try
            {
                RulesConfig first = asset.ToConfig();
                first.startingCoins = 99;
                first.pirateCards[0].copies = 99;

                RulesConfig second = asset.ToConfig();
                Assert.AreEqual(new RulesConfig().startingCoins, second.startingCoins);
                Assert.AreEqual(new RulesConfig().pirateCards[0].copies, second.pirateCards[0].copies);
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void NewMapLayoutAssetIsAnEmptyInvalidLayoutUntilFilled()
        {
            var asset = Create<MapLayoutAsset>();
            try
            {
                MapLayout layout = asset.ToLayout();
                Assert.AreEqual(20, layout.width);
                Assert.AreEqual(20, layout.height);
                Assert.IsEmpty(layout.islandCells);

                MapValidationResult result = asset.Validate();
                Assert.IsFalse(result.IsValid);
                Assert.IsTrue(result.Has(MapValidationCode.NoSacredIsland));
                Assert.IsTrue(result.Has(MapValidationCode.WrongSpawnCount));
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void MapLayoutAssetConvertsItsContentsAndValidates()
        {
            var source = new MapLayout();
            source.sacredIslandCells.Add(new LayoutCell(9, 9));
            source.islandCells.Add(new LayoutIslandCell(4, 4, 0));
            int[,] spawns = { { 10, 0 }, { 10, 19 }, { 0, 10 }, { 19, 10 }, { 0, 3 }, { 19, 16 }, { 16, 0 }, { 3, 19 } };
            for (int i = 0; i < 8; i++) source.spawnCells.Add(new LayoutCell(spawns[i, 0], spawns[i, 1]));

            var asset = Create<MapLayoutAsset>();
            try
            {
                JsonUtility.FromJsonOverwrite("{\"layout\":" + JsonUtility.ToJson(source) + "}", asset);

                MapLayout layout = asset.ToLayout();
                Assert.AreEqual(1, layout.sacredIslandCells.Count);
                Assert.AreEqual(4, layout.islandCells[0].x);
                Assert.AreEqual(8, layout.spawnCells.Count);
                Assert.AreEqual(new Coord(3, 19), layout.spawnCells[7].ToCoord());
                Assert.IsTrue(asset.Validate().IsValid, asset.Validate().ToString());
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void ProjectRulesConfigAssetIsValidIfPresent()
        {
            RulesConfigAsset asset = FindProjectAsset<RulesConfigAsset>();
            if (asset == null) Assert.Ignore("RulesConfig.asset non ancora creato (checklist Bezi, spec 0001).");
            Assert.IsEmpty(asset.ToConfig().Validate());
        }

        [Test]
        public void ProjectMapLayoutAssetIsValidIfPresent()
        {
            MapLayoutAsset asset = FindProjectAsset<MapLayoutAsset>();
            if (asset == null) Assert.Ignore("MapLayout.asset non ancora creato (spec 0005, dopo l'audit 0000).");
            MapValidationResult result = asset.Validate();
            Assert.IsTrue(result.IsValid, result.ToString());
        }

        private static T FindProjectAsset<T>() where T : Object
        {
            string guid = AssetDatabase.FindAssets("t:" + typeof(T).Name).FirstOrDefault();
            return guid == null ? null : AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
        }
    }
}
