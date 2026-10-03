using System.Collections;
using System.Collections.Generic;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.State;
using TMPro;
using UnityEngine;

namespace hp55games.MareIgnoto.Unity.Views
{
    /// <summary>
    /// Il log di partita in /UI_Canvas/UI_Log (06_presentazione.md §4): una riga di <see cref="UiText.LogLine"/> per ogni
    /// evento che ne ha una; mostra le ultime righe. È completo anche quando le animazioni sono saltate.
    /// </summary>
    public sealed class LogView : GameView
    {
        [SerializeField] private TMP_Text text;

        [Tooltip("Righe mostrate (le più recenti).")]
        [SerializeField, Min(1)] private int maxLines = 40;

        private readonly Queue<string> lines = new Queue<string>();
        private IReadOnlyGameState gameState;

        public override void Initialize(IReadOnlyGameState state)
        {
            if (!Require(text, nameof(text))) return;
            gameState = state;
            text.text = string.Empty;
        }

        public override IEnumerator Play(GameEvent gameEvent, float duration)
        {
            string line = UiText.LogLine(gameEvent, gameState);
            if (line != null)
            {
                lines.Enqueue(line);
                while (lines.Count > maxLines) lines.Dequeue();
                text.text = string.Join("\n", lines);
            }

            yield break;
        }
    }
}
