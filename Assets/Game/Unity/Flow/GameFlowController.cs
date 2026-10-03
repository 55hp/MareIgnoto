using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Bots;
using hp55games.MareIgnoto.Rules.Engine;
using hp55games.MareIgnoto.Rules.Events;
using UnityEngine;

namespace hp55games.MareIgnoto.Unity.Flow
{
    /// <summary>
    /// Il ciclo della partita (01_architettura.md §5): fa riprodurre gli eventi, poi risponde alla decisione in attesa e
    /// ricomincia. In questa spec (0005) tutti i posti sono bot casuali: rispondono subito, con una breve pausa perché la
    /// scena resti leggibile (06 §3). Niente UI di decisione né passaggio del dispositivo.
    /// </summary>
    public sealed class GameFlowController : MonoBehaviour
    {
        /// <summary>Il punto di vista di chi guarda una partita di soli bot: vede solo la forma pubblica degli eventi privati.</summary>
        private const int Spectator = -1;

        [SerializeField] private EventPlayer eventPlayer;

        [Tooltip("Pausa prima di ogni risposta dei bot, in secondi.")]
        [SerializeField, Min(0f)] private float botDelaySeconds = 0.3f;

        private GameSession session;
        private RandomBot bot;

        /// <summary>Avvia la partita: lo chiama GameBootstrap, il composition root.</summary>
        public void Begin(GameSession gameSession, RandomBot randomBot)
        {
            if (eventPlayer == null)
            {
                Debug.LogError("[GameFlowController] su '" + name + "': manca il riferimento 'eventPlayer' (Inspector).", this);
                enabled = false;
                return;
            }

            session = gameSession;
            bot = randomBot;
            eventPlayer.Initialize(session.State);
            eventPlayer.Enqueue(ForSpectator(session.InitialEvents));
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            while (session.Pending != null)
            {
                while (!eventPlayer.IsIdle) yield return null;
                if (botDelaySeconds > 0f) yield return new WaitForSeconds(botDelaySeconds);

                IReadOnlyList<GameEvent> events;
                try
                {
                    events = session.Submit(bot.Choose(session.Pending));
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                    yield break;
                }

                eventPlayer.Enqueue(ForSpectator(events));
            }
        }

        private static IEnumerable<GameEvent> ForSpectator(IEnumerable<GameEvent> events) => events.Select(e => e.ViewFor(Spectator)).ToList();
    }
}
