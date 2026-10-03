using System.Collections;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.State;
using TMPro;
using UnityEngine;

namespace hp55games.MareIgnoto.Unity.Views
{
    /// <summary>
    /// La rosa dei venti in /UI_Canvas/UI_TopBar: una lancetta che punta verso dove soffia il vento (R-061; Nord in alto,
    /// 45° per scatto in senso orario) e la scritta. Si aggiorna con <see cref="WindChangedEvent"/>.
    /// </summary>
    public sealed class WindRoseView : GameView
    {
        [Tooltip("La lancetta: a rotazione 0 punta verso l'alto (Nord).")]
        [SerializeField] private RectTransform needle;
        [SerializeField] private TMP_Text label;

        public override void Initialize(IReadOnlyGameState state)
        {
            if (!Require(needle, nameof(needle)) || !Require(label, nameof(label))) return;
            Show(state.Wind);
        }

        public override IEnumerator Play(GameEvent gameEvent, float duration)
        {
            if (gameEvent is WindChangedEvent changed) Show(changed.Wind);
            yield break;
        }

        private void Show(Heading wind)
        {
            needle.localEulerAngles = new Vector3(0f, 0f, -360f / HeadingExtensions.Count * (int)wind);
            label.text = UiText.Wind(wind);
        }
    }
}
