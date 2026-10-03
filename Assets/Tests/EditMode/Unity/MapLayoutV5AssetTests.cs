using System.Linq;
using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Unity;
using NUnit.Framework;
using UnityEditor;

namespace hp55games.MareIgnoto.Unity.Tests
{
    /// <summary>
    /// MapLayout.asset del progetto (spec 0005, passo A) contro il layout v5 (LayoutV5, tech/05_mappa.md §3–§6): valido e
    /// identico cella per cella. Ignorato finché l'asset non esiste.
    /// </summary>
    public class MapLayoutV5AssetTests
    {
        private const string AssetPath = "Assets/Game/Content/Config/MapLayout.asset";

        private static MapLayoutAsset LoadOrIgnore()
        {
            MapLayoutAsset asset = AssetDatabase.LoadAssetAtPath<MapLayoutAsset>(AssetPath);
            if (asset == null)
                Assert.Ignore(AssetPath + " non esiste ancora: eseguire MareIgnoto > Create MapLayout v5 (spec 0005, passo A).");
            return asset;
        }

        [Test]
        public void TheProjectMapLayoutIsValid()
        {
            MapValidationResult result = LoadOrIgnore().Validate();
            Assert.IsTrue(result.IsValid, result.ToString());
        }

        [Test]
        public void TheProjectMapLayoutMatchesLayoutV5CellByCell()
        {
            MapLayout layout = LoadOrIgnore().ToLayout();
            MapLayout expected = LayoutV5.Create();
            var config = new RulesConfig();
            GameMap actualMap = GameMap.Create(layout, config);
            GameMap expectedMap = GameMap.Create(expected, config);

            Assert.AreEqual(expectedMap.Width, actualMap.Width);
            Assert.AreEqual(expectedMap.Height, actualMap.Height);
            for (int x = 0; x < expectedMap.Width; x++)
                for (int y = 0; y < expectedMap.Height; y++)
                {
                    var cell = new Coord(x, y);
                    Assert.AreEqual(expectedMap.KindAt(cell), actualMap.KindAt(cell), cell.Name + ": tipo");
                    Assert.AreEqual(expectedMap.IslandIdAt(cell), actualMap.IslandIdAt(cell), cell.Name + ": id isola");
                    int expectedZone = expectedMap.ZoneOf(cell), actualZone = actualMap.ZoneOf(cell);
                    Assert.AreEqual(expectedZone < 0 ? null : expectedMap.ZoneId(expectedZone),
                        actualZone < 0 ? null : actualMap.ZoneId(actualZone), cell.Name + ": zona");
                }

            Assert.AreEqual(expectedMap.ZoneCount, actualMap.ZoneCount);
            for (int zone = 0; zone < expectedMap.ZoneCount; zone++)
            {
                string id = expectedMap.ZoneId(zone);
                Assert.AreEqual(id, actualMap.ZoneId(zone), "ordine delle zone (gli spicchi R1…R8 in ordine)");
                Assert.AreEqual(expectedMap.ZoneKindOf(zone), actualMap.ZoneKindOf(zone), id + ": tipo");
                Assert.AreEqual(expectedMap.ZoneInitialLevel(zone), actualMap.ZoneInitialLevel(zone), id + ": livello iniziale");
            }

            for (int n = config.minPlayers; n <= config.maxPlayers; n++)
                CollectionAssert.AreEqual(expectedMap.SpawnPointsFor(n).ToArray(), actualMap.SpawnPointsFor(n).ToArray(),
                    "preset per " + n + " giocatori");
        }
    }
}
