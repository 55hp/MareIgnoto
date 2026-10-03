using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Map;
using NUnit.Framework;

namespace hp55games.MareIgnoto.Rules.Tests
{
    /// <summary>Tinta e numero delle zone sul tabellone (spec 0005, ZoneOverlayView): le zone si vedono sempre.</summary>
    public class ZoneAppearanceTests
    {
        private static readonly RulesConfig Config = new RulesConfig();

        [TestCase(0, ZoneTint.Calm, false)]
        [TestCase(1, ZoneTint.RoughSea, true)]
        [TestCase(2, ZoneTint.Storm, true)]
        [TestCase(5, ZoneTint.Storm, true)]
        public void TheTintFollowsTheLevelAndTheNumberShowsFromLevelOne_R081(int level, ZoneTint tint, bool number)
        {
            Assert.AreEqual(tint, ZoneAppearance.TintFor(level, Config));
            Assert.AreEqual(number, ZoneAppearance.ShowsLevel(level));
        }

        [Test]
        public void TheThresholdsComeFromTheConfig()
        {
            var config = new RulesConfig { roughSeaLevel = 2, stormLevel = 4 };
            Assert.AreEqual(ZoneTint.Calm, ZoneAppearance.TintFor(1, config));
            Assert.AreEqual(ZoneTint.RoughSea, ZoneAppearance.TintFor(3, config));
            Assert.AreEqual(ZoneTint.Storm, ZoneAppearance.TintFor(4, config));
        }
    }
}
