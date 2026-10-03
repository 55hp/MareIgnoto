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
        public void NewMapLayoutAssetIs25x25AndEmptyUntilFilled()
        {
            var asset = Create<MapLayoutAsset>();
            try
            {
                MapLayout layout = asset.ToLayout();
                Assert.AreEqual(25, layout.width);
                Assert.AreEqual(25, layout.height);
                Assert.IsEmpty(layout.islandCells);
                Assert.IsEmpty(layout.zones);
                Assert.IsEmpty(layout.spawnPresets, "il layout v4 si inserisce come dato (05_mappa.md §3–§6, spec 0005)");

                MapValidationResult result = asset.Validate();
                Assert.IsFalse(result.IsValid);
                Assert.IsTrue(result.Has(MapValidationCode.NoSacredIsland));
                Assert.IsTrue(result.Has(MapValidationCode.MissingSpawnPreset));
                Assert.IsTrue(result.Has(MapValidationCode.RingSliceCount));
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void MapLayoutAssetConvertsZonesAndPresets()
        {
            var source = new MapLayout();
            source.sacredIslandCells.Add(new LayoutCell(12, 12));
            source.islandCells.Add(new LayoutIslandCell(2, 5, 1));
            source.zones.Add(new LayoutZone("R1", ZoneKind.RingSlice, 5, new[] { new LayoutCell(15, 12), new LayoutCell(15, 13) }));
            source.zones.Add(new LayoutZone("N1", ZoneKind.Cloud, 0, new[] { new LayoutCell(20, 10) }));
            source.spawnPresets.Add(new SpawnPreset(2, new LayoutCell(1, 1), new LayoutCell(23, 23)));

            var asset = Create<MapLayoutAsset>();
            try
            {
                JsonUtility.FromJsonOverwrite("{\"layout\":" + JsonUtility.ToJson(source) + "}", asset);

                MapLayout layout = asset.ToLayout();
                Assert.AreEqual(1, layout.sacredIslandCells.Count);
                Assert.AreEqual(1, layout.islandCells[0].islandId);
                Assert.AreEqual(2, layout.zones.Count);
                Assert.AreEqual("R1", layout.zones[0].id);
                Assert.AreEqual(ZoneKind.RingSlice, layout.zones[0].kind);
                Assert.AreEqual(5, layout.zones[0].initialLevel);
                Assert.AreEqual(new Coord(15, 13), layout.zones[0].cells[1].ToCoord());
                Assert.AreEqual(ZoneKind.Cloud, layout.zones[1].kind);
                Assert.AreEqual(new Coord(23, 23), layout.PresetFor(2).cells[1].ToCoord());
                Assert.AreEqual(source.Validate().ToString(), asset.Validate().ToString(), "stessi dati, stessa validazione");
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
            if (asset == null) Assert.Ignore("MapLayout.asset non ancora creato (spec 0005, passo A: layout v4 di 05_mappa.md §3–§6).");
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
