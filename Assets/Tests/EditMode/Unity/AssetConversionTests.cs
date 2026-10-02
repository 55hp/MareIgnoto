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
        public void NewMapLayoutAssetHasDefaultPresetsButNoIslandsUntilFilled()
        {
            var asset = Create<MapLayoutAsset>();
            try
            {
                MapLayout layout = asset.ToLayout();
                Assert.AreEqual(20, layout.width);
                Assert.AreEqual(20, layout.height);
                Assert.IsEmpty(layout.islandCells);
                Assert.AreEqual(7, layout.spawnPresets.Count, "preset di default per N = 2..8 (05_mappa.md §4)");

                MapValidationResult result = asset.Validate();
                Assert.IsFalse(result.IsValid);
                Assert.IsTrue(result.Has(MapValidationCode.NoSacredIsland));
                Assert.AreEqual(1, result.Issues.Count, result.ToString());
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
            source.PresetFor(2).cells[1] = new LayoutCell(19, 18);

            var asset = Create<MapLayoutAsset>();
            try
            {
                JsonUtility.FromJsonOverwrite("{\"layout\":" + JsonUtility.ToJson(source) + "}", asset);

                MapLayout layout = asset.ToLayout();
                Assert.AreEqual(1, layout.sacredIslandCells.Count);
                Assert.AreEqual(4, layout.islandCells[0].x);
                Assert.AreEqual(7, layout.spawnPresets.Count);
                Assert.AreEqual(new Coord(19, 18), layout.PresetFor(2).cells[1].ToCoord());
                Assert.AreEqual(new Coord(19, 9), layout.PresetFor(8).cells[7].ToCoord());
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
            if (asset == null) Assert.Ignore("MapLayout.asset non ancora creato (spec 0005, quando il layout è approvato: 05_mappa.md §6).");
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
