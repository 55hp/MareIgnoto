using System;
using System.Collections.Generic;
using hp55games.MareIgnoto.Rules.Bots;
using hp55games.MareIgnoto.Rules.Engine;
using hp55games.MareIgnoto.Rules.Random;
using hp55games.MareIgnoto.Rules.Setup;
using UnityEngine;

namespace hp55games.MareIgnoto.Unity.Flow
{
    /// <summary>
    /// Il composition root della scena (01_architettura.md §5, CLAUDE.md regola 9). Spec 0005: "avvio diretto di test",
    /// tutti i posti bot casuali, con numero di giocatori e seed nell'Inspector. Crea la GameSession da RulesConfigAsset e
    /// MapLayoutAsset e la passa a GameFlowController.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private RulesConfigAsset rulesConfig;
        [SerializeField] private MapLayoutAsset mapLayout;
        [SerializeField] private GameFlowController flow;

        [Header("Avvio diretto di test")]
        [Tooltip("Numero di giocatori, tutti bot casuali (devono rientrare nei limiti di RulesConfig).")]
        [SerializeField, Min(1)] private int playerCount = 4;

        [Tooltip("Seed della partita e del bot: stesso seed, stessa partita.")]
        [SerializeField] private int seed = 1;

        private void Start()
        {
            if (!Require(rulesConfig, nameof(rulesConfig)) || !Require(mapLayout, nameof(mapLayout)) || !Require(flow, nameof(flow))) return;

            var players = new List<PlayerSetup>();
            for (int seat = 0; seat < playerCount; seat++) players.Add(new PlayerSetup(seat, UiText.BotName(seat)));

            GameSession session;
            try
            {
                session = GameSession.Start(new GameSetup(players, seed), rulesConfig.ToConfig(), mapLayout.ToLayout());
            }
            catch (ArgumentException exception)
            {
                Debug.LogError("[GameBootstrap] partita non avviata: " + exception.Message, this);
                enabled = false;
                return;
            }

            flow.Begin(session, new RandomBot(new SeededRandom(seed)));
        }

        private bool Require(UnityEngine.Object reference, string field)
        {
            if (reference != null) return true;
            Debug.LogError("[GameBootstrap] su '" + name + "': manca il riferimento '" + field + "' (Inspector).", this);
            enabled = false;
            return false;
        }
    }
}
