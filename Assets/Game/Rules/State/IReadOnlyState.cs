using System.Collections.Generic;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Map;

namespace hp55games.MareIgnoto.Rules.State
{
    /// <summary>
    /// Ciò che di un giocatore è visibile a tutti. Le informazioni segrete (carte sotto coperta, mano, missioni)
    /// si leggono solo da <see cref="PlayerView"/>, ottenuto con <see cref="IReadOnlyGameState.ViewFor"/>.
    /// </summary>
    public interface IReadOnlyPlayerState
    {
        int Id { get; }
        string Name { get; }
        Coord Position { get; }
        int Coins { get; }
        /// <summary>Segnalini taglia (R-007).</summary>
        int BountyTokens { get; }
        /// <summary>La rotta, solo dopo la rivelazione (R-041); null prima.</summary>
        Heading? RevealedHeading { get; }
        /// <summary>Le carte sopra coperta, scoperte (R-010): una voce per slot, null se vuoto.</summary>
        IReadOnlyList<CrewCard> CrewAbove { get; }
        /// <summary>Per ogni slot sotto coperta se è occupato; l'identità della carta è segreta.</summary>
        IReadOnlyList<bool> CrewBelowOccupied { get; }
        int HandCount { get; }
        int MissionsInHandCount { get; }
        /// <summary>Missioni completate: rivelate al completamento (R-132).</summary>
        IReadOnlyList<MissionCard> CompletedMissions { get; }
    }

    /// <summary>Lo stato della partita in sola lettura (04_motore.md §5).</summary>
    public interface IReadOnlyGameState
    {
        /// <summary>La configurazione della partita. Va solo letta: il motore e la presentazione non la modificano.</summary>
        RulesConfig Config { get; }
        GameMap Map { get; }

        /// <summary>Numero del round in corso; 0 durante la preparazione della partita.</summary>
        int Round { get; }
        GamePhase Phase { get; }
        /// <summary>Ordine di turno corrente, per id giocatore. Vuoto prima che sia stabilito.</summary>
        IReadOnlyList<int> TurnOrder { get; }
        /// <summary>Giocatore di cui è il turno in Fase 2; -1 se nessuno.</summary>
        int ActivePlayer { get; }

        /// <summary>Direzione verso cui soffia il vento dominante (R-061).</summary>
        Heading Wind { get; }
        /// <summary>Stato di ogni zona meteo, per indice di zona (05_mappa.md §3).</summary>
        IReadOnlyList<WeatherState> Zones { get; }
        /// <summary>Monete del Tesoro dell'Isola Sacra (R-036).</summary>
        int Treasure { get; }

        int PlayerCount { get; }
        IReadOnlyList<IReadOnlyPlayerState> Players { get; }
        IReadOnlyPlayerState Player(int id);

        DeckInfo CrewDeck { get; }
        DeckInfo PirateDeck { get; }
        DeckInfo CorsairDeck { get; }

        /// <summary>Il punto di vista di un giocatore: stato pubblico più ciò che solo lui può vedere.</summary>
        PlayerView ViewFor(int playerId);
    }
}
