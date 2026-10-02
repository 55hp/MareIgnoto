using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Engine
{
    /// <summary>
    /// Gli invarianti di 04_motore.md §6 che si possono verificare sullo stato in ogni momento. I test e la simulazione
    /// li controllano dopo ogni Submit. Gli invarianti sul movimento e sui turni (navi sulla stessa cella a inizio Fase 2,
    /// un turno a testa, Nostromo) si aggiungono con le spec che introducono quelle regole.
    /// </summary>
    internal static class Invariants
    {
        public static IReadOnlyList<string> Check(GameState state)
        {
            var errors = new List<string>();

            CheckCardConservation(state, errors);

            foreach (PlayerState player in state.Players)
            {
                if (player.Coins < 0) errors.Add("p" + player.Id + " ha monete negative: " + player.Coins);
                if (state.Phase != GamePhase.Setup && !state.Map.IsInBounds(player.Position))
                    errors.Add("p" + player.Id + " è fuori dalla mappa: " + player.Position);
            }

            if (state.Treasure < 0) errors.Add("Il Tesoro è negativo: " + state.Treasure);

            if (state.TurnOrderList.Count > 0 &&
                !state.TurnOrderList.OrderBy(id => id).SequenceEqual(Enumerable.Range(0, state.PlayerCount)))
                errors.Add("L'ordine di turno non contiene ogni giocatore una volta sola: [" + string.Join(",", state.TurnOrderList) + "]");

            return errors;
        }

        /// <summary>Per ogni mazzo: carte nel mazzo + scarti + mani + slot + in transito + missioni = totale iniziale.</summary>
        private static void CheckCardConservation(GameState state, List<string> errors)
        {
            var all = new List<Card>();
            all.AddRange(state.Crew.DrawPile);
            all.AddRange(state.Crew.DiscardPile);
            all.AddRange(state.Pirate.DrawPile);
            all.AddRange(state.Pirate.DiscardPile);
            all.AddRange(state.Corsair.DrawPile);
            all.AddRange(state.Corsair.DiscardPile);
            foreach (PlayerState player in state.Players)
            {
                all.AddRange(player.Crew.All());
                all.AddRange(player.Hand);
                all.AddRange(player.Missions);
                all.AddRange(player.Completed);
                all.AddRange(player.InTransit);
            }

            Compare(errors, "Crew", all.Count(c => c.Deck == DeckKind.Crew), state.InitialCrewCards);
            Compare(errors, "Pirateria", all.Count(c => c.Deck == DeckKind.Pirate), state.InitialPirateCards);
            Compare(errors, "Corsaro", all.Count(c => c.Deck == DeckKind.Corsair), state.InitialCorsairCards);

            int distinct = all.Select(c => c.Uid).Distinct().Count();
            if (distinct != all.Count)
                errors.Add("Ci sono carte con lo stesso Uid: " + (all.Count - distinct) + " duplicate.");
        }

        private static void Compare(List<string> errors, string deck, int found, int expected)
        {
            if (found != expected)
                errors.Add("Conservazione carte " + deck + ": trovate " + found + ", attese " + expected + ".");
        }
    }
}
