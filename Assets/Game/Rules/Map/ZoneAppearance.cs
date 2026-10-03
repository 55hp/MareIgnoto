using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Map
{
    /// <summary>La tinta di una zona meteo sul tabellone (06_presentazione.md §2).</summary>
    public enum ZoneTint
    {
        /// <summary>Livello Normale: riempimento quasi trasparente, solo il contorno si vede.</summary>
        Calm,
        RoughSea,
        Storm,
    }

    /// <summary>
    /// Come si mostra una zona dato il suo livello (spec 0005, ZoneOverlayView): logica pura, testabile fuori da Unity.
    /// La zona si disegna sempre; la tinta segue l'effetto del livello (R-081, soglie in RulesConfig) e il numero del
    /// livello compare dal livello 1.
    /// </summary>
    public static class ZoneAppearance
    {
        public static ZoneTint TintFor(int level, RulesConfig config)
        {
            switch (config.WeatherAt(level))
            {
                case WeatherState.Storm: return ZoneTint.Storm;
                case WeatherState.RoughSea: return ZoneTint.RoughSea;
                default: return ZoneTint.Calm;
            }
        }

        /// <summary>Il numero del livello si scrive sulla zona solo dal livello 1 in su.</summary>
        public static bool ShowsLevel(int level) => level >= 1;
    }
}
