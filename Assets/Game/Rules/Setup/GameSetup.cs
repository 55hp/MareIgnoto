using System;
using System.Collections.Generic;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.Random;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Setup
{
    public sealed class PlayerSetup
    {
        /// <summary>Posto al tavolo, da 0: coincide con l'id del giocatore e con il punto di partenza (R-031).</summary>
        public int Seat { get; }
        public string Name { get; }

        public PlayerSetup(int seat, string name)
        {
            Seat = seat;
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }

    /// <summary>Una carta crew desiderata in cima al mazzo; con <see cref="CrewSuit.None"/> va bene qualsiasi seme.</summary>
    public readonly struct CrewCardSpec
    {
        public readonly CrewCardId Kind;
        public readonly CrewSuit Suit;

        public CrewCardSpec(CrewCardId kind, CrewSuit suit = CrewSuit.None)
        {
            Kind = kind;
            Suit = suit;
        }

        internal bool Matches(CrewCard card) =>
            card.Kind == Kind && (Suit == CrewSuit.None || card.Suit == Suit);
    }

    public readonly struct ZoneSetup
    {
        public readonly int Zone;
        public readonly WeatherState State;

        public ZoneSetup(int zone, WeatherState state)
        {
            Zone = zone;
            State = state;
        }
    }

    /// <summary>
    /// Opzioni di preparazione per il tutorial (04_motore.md §1): le carte indicate vengono portate in cima ai mazzi
    /// dopo il mescolamento (nell'ordine di pesca), vento e zone iniziali sono prestabiliti.
    /// Tutto è facoltativo.
    /// </summary>
    public sealed class TutorialOptions
    {
        public IReadOnlyList<CrewCardSpec> CrewDeckTop { get; set; }
        public IReadOnlyList<PirateCardId> PirateDeckTop { get; set; }
        public IReadOnlyList<MissionId> CorsairDeckTop { get; set; }
        public Heading? InitialWind { get; set; }
        public IReadOnlyList<ZoneSetup> InitialZones { get; set; }
    }

    /// <summary>I dati di una partita: chi gioca, il seed e le eventuali opzioni del tutorial.</summary>
    public sealed class GameSetup
    {
        public IReadOnlyList<PlayerSetup> Players { get; }

        /// <summary>
        /// Il seed della partita. Il chiamante lo usa per creare la <see cref="SeededRandom"/> passata a
        /// GameSession.Start (la sovrascrittura senza sorgente lo fa da sola).
        /// </summary>
        public int Seed { get; }

        /// <summary>Null in una partita normale.</summary>
        public TutorialOptions Tutorial { get; set; }

        public GameSetup(IReadOnlyList<PlayerSetup> players, int seed)
        {
            Players = players ?? throw new ArgumentNullException(nameof(players));
            Seed = seed;
        }

        /// <summary>Partita con <paramref name="playerCount"/> giocatori senza nome proprio ("P1", "P2"...), per test e simulazioni.</summary>
        public static GameSetup ForPlayers(int playerCount, int seed)
        {
            var players = new List<PlayerSetup>();
            for (int seat = 0; seat < playerCount; seat++)
                players.Add(new PlayerSetup(seat, "P" + (seat + 1)));
            return new GameSetup(players, seed);
        }
    }
}
