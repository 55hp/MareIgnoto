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
    /// Il composition root della scena (01_architettura.md §5, CLAUDE.md regola 9). "Avvio diretto di test": tutti i posti
    /// sono bot (spec 0005), con numero di giocatori, seed e tipo di bot per posto nell'Inspector (spec 0011). Crea la
    /// GameSession da RulesConfigAsset e MapLayoutAsset e la passa a GameFlowController con un bot per posto.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private RulesConfigAsset rulesConfig;
        [SerializeField] private MapLayoutAsset mapLayout;
        [SerializeField] private GameFlowController flow;

        [Header("Avvio diretto di test")]
        [Tooltip("Numero di giocatori, tutti bot (devono rientrare nei limiti di RulesConfig).")]
        [SerializeField, Min(1)] private int playerCount = 4;

        [Tooltip("Il bot di ogni posto, in ordine (09_bot.md). I posti oltre la lista giocano col bot casuale.")]
        [SerializeField] private List<SeatBot> seats = new List<SeatBot> { SeatBot.Rush, SeatBot.Random, SeatBot.Hunter, SeatBot.Random };

        [Tooltip("Seed della partita e del bot: stesso seed, stessa partita.")]
        [SerializeField] private int seed = 1;

        private void Start()
        {
            if (!Require(rulesConfig, nameof(rulesConfig)) || !Require(mapLayout, nameof(mapLayout)) || !Require(flow, nameof(flow))) return;

            var kinds = new List<SeatBot>();
            for (int seat = 0; seat < playerCount; seat++) kinds.Add(seat < seats.Count ? seats[seat] : SeatBot.Random);
            IReadOnlyList<string> names = UiText.BotNames(kinds);

            var players = new List<PlayerSetup>();
            var bots = new List<IBot>();
            for (int seat = 0; seat < playerCount; seat++)
            {
                players.Add(new PlayerSetup(seat, names[seat]));
                bots.Add(CreateBot(kinds[seat], seat));
            }

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

            flow.Begin(session, bots);
        }

        /// <summary>Un bot per posto, ognuno con la sua casualità derivata da seed e posto (09_bot.md §1).</summary>
        private IBot CreateBot(SeatBot kind, int seat)
        {
            var random = new SeededRandom(StrategicBot.SeedFor(seed, seat));
            switch (kind)
            {
                case SeatBot.Rush: return new StrategicBot(BotProfile.Rush, random);
                case SeatBot.Hunter: return new StrategicBot(BotProfile.Hunter, random);
                default: return new RandomBot(random);
            }
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
