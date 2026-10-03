using System.Collections;
using System.Collections.Generic;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.State;
using hp55games.MareIgnoto.Unity.Views;
using UnityEngine;

namespace hp55games.MareIgnoto.Unity.Flow
{
    /// <summary>
    /// Riproduce gli eventi in coda uno alla volta (06_presentazione.md §4): ogni evento va a tutte le viste, e il
    /// successivo parte quando hanno finito. Velocità e salto delle animazioni nell'Inspector.
    /// </summary>
    public sealed class EventPlayer : MonoBehaviour
    {
        [Tooltip("Le viste che riproducono gli eventi (BoardView, ZoneOverlayView, ShipsView, CellHighlightView, WindRoseView, LogView).")]
        [SerializeField] private GameView[] views = new GameView[0];

        [Tooltip("Durata di un'animazione a velocità 1, in secondi (per esempio un passo di una nave).")]
        [SerializeField, Min(0f)] private float secondsPerAnimation = 0.25f;

        [Tooltip("Moltiplicatore della velocità di riproduzione.")]
        [SerializeField, Min(0.01f)] private float speed = 1f;

        [Tooltip("Salta le animazioni: le viste si aggiornano subito, il log resta completo.")]
        [SerializeField] private bool skipAnimations;

        private readonly Queue<GameEvent> queue = new Queue<GameEvent>();
        private bool playing;
        private int running;

        /// <summary>Vero quando non ci sono eventi da riprodurre.</summary>
        public bool IsIdle => queue.Count == 0 && !playing;

        public bool SkipAnimations
        {
            get => skipAnimations;
            set => skipAnimations = value;
        }

        /// <summary>Prepara le viste dallo stato iniziale.</summary>
        public void Initialize(IReadOnlyGameState state)
        {
            for (int i = 0; i < views.Length; i++)
            {
                if (views[i] == null)
                    Debug.LogError("[EventPlayer] su '" + name + "': l'elemento " + i + " di 'views' è vuoto (Inspector).", this);
                else
                    views[i].Initialize(state);
            }
        }

        public void Enqueue(IEnumerable<GameEvent> events)
        {
            foreach (GameEvent gameEvent in events) queue.Enqueue(gameEvent);
            if (!playing && queue.Count > 0) StartCoroutine(PlayQueue());
        }

        private IEnumerator PlayQueue()
        {
            playing = true;
            while (queue.Count > 0)
            {
                GameEvent gameEvent = queue.Dequeue();
                float duration = skipAnimations ? 0f : secondsPerAnimation / speed;
                foreach (GameView view in views)
                {
                    if (view == null || !view.isActiveAndEnabled) continue;
                    running++;
                    StartCoroutine(Run(view.Play(gameEvent, duration)));
                }

                while (running > 0) yield return null;
            }

            playing = false;
        }

        private IEnumerator Run(IEnumerator routine)
        {
            yield return StartCoroutine(routine);
            running--;
        }
    }
}
