using UnityEngine;

namespace hp55games.MareIgnoto.Unity
{
    /// <summary>
    /// I colori dei giocatori per posto (06_presentazione.md §6: navi colorate per giocatore). Unica fonte per navi,
    /// segnalini isola e rotte. Un asset nuovo nasce con 8 colori distinti; si cambiano da Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "MareIgnoto/Player Palette", fileName = "PlayerPalette")]
    public sealed class PlayerPaletteAsset : ScriptableObject
    {
        [SerializeField] private Color[] colors =
        {
            new Color(0.86f, 0.20f, 0.18f), // rosso
            new Color(0.20f, 0.45f, 0.90f), // blu
            new Color(0.20f, 0.70f, 0.30f), // verde
            new Color(0.95f, 0.80f, 0.15f), // giallo
            new Color(0.60f, 0.30f, 0.80f), // viola
            new Color(0.95f, 0.55f, 0.15f), // arancio
            new Color(0.20f, 0.80f, 0.80f), // ciano
            new Color(0.90f, 0.40f, 0.70f), // rosa
        };

        /// <summary>Il colore del posto <paramref name="seat"/>; se i colori sono meno dei posti si ricomincia dal primo.</summary>
        public Color ColorOf(int seat) => colors == null || colors.Length == 0 ? Color.white : colors[seat % colors.Length];
    }
}
