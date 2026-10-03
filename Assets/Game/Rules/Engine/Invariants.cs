using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Engine
{
    /// <summary>
    /// Gli invarianti di 04_motore.md §6 che si possono verificare sullo stato in ogni momento. I test e la simulazione
    /// li controllano dopo ogni Submit.
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

            var islandIds = new HashSet<int>(state.Map.IslandIds());
            foreach (PlayerState player in state.Players)
                if (player.IslandMarker != -1 && !islandIds.Contains(player.IslandMarker))
                    errors.Add("p" + player.Id + ": il segnalino è su un'isola inesistente (" + player.IslandMarker + ").");

            CheckBountyAndMissions(state, errors);
            if (state.Phase == GamePhase.Ended && state.Result == null) errors.Add("Partita finita senza esito.");

            CheckShipsOnSeaCells(state, errors);
            CheckNostromo(state, errors);
            CheckTurnsPlayed(state, errors);

            if (state.TurnOrderList.Count > 0 &&
                !state.TurnOrderList.OrderBy(id => id).SequenceEqual(Enumerable.Range(0, state.PlayerCount)))
                errors.Add("L'ordine di turno non contiene ogni giocatore una volta sola: [" + string.Join(",", state.TurnOrderList) + "]");

            return errors;
        }

        /// <summary>
        /// Fuori dal movimento mai due navi sulla stessa cella di mare, salvo R-071 e il limite della catena di R-073a
        /// (celle in <see cref="GameState.SharedSeaCellsAllowed"/>).
        /// </summary>
        private static void CheckShipsOnSeaCells(GameState state, List<string> errors)
        {
            if (state.Round == 0 || state.MovementInProgress) return;
            foreach (var cell in state.Players.GroupBy(p => p.Position).Where(g => g.Count() > 1))
            {
                if (state.Map.KindAt(cell.Key) != Map.CellKind.Sea || state.SharedSeaCellsAllowed.Contains(cell.Key)) continue;
                errors.Add("Navi " + string.Join(",", cell.Select(p => "p" + p.Id)) + " sulla stessa cella di mare " + cell.Key + ".");
            }
        }

        /// <summary>
        /// Il Nostromo, una volta sopra coperta, torna sotto solo col Medico (R-016, R-023): un Nostromo sopra coperta è
        /// bloccato; sotto coperta è bloccato mai, e se ci arriva da sopra è perché il Medico l'ha liberato (l'unico
        /// scrittore di <see cref="GameState.NostromiFreedByMedico"/>). Quando risale, il blocco vale di nuovo. Il blocco
        /// vale solo sulla nave: chi riceve un Nostromo (R-020) può metterlo anche sotto coperta.
        /// </summary>
        private static void CheckNostromo(GameState state, List<string> errors)
        {
            foreach (PlayerState player in state.Players)
                for (int slot = 0; slot < player.Crew.Count; slot++)
                {
                    CrewCard card = player.Crew[slot];
                    if (card == null || card.Kind != CrewCardId.Nostromo) continue;
                    bool locked = state.LockedNostromi.Contains(card.Uid);
                    if (player.Crew.IsBelow(slot) && locked)
                        errors.Add("p" + player.Id + ": il Nostromo " + card + " è tornato sotto coperta senza il Medico (slot " + slot + ").");
                    if (player.Crew.IsAbove(slot) && !locked)
                        errors.Add("p" + player.Id + ": il Nostromo " + card + " è sopra coperta ma non è bloccato (slot " + slot + ").");
                }

            foreach (int uid in state.NostromiFreedByMedico)
                if (state.LockedNostromi.Contains(uid))
                    errors.Add("Il Nostromo #" + uid + " risulta insieme bloccato e liberato dal Medico.");

            // Il blocco vale solo sulla nave (R-016, R-020): fuori dagli slot (mazzo, scarti, in transito) nessun Nostromo è bloccato.
            var onShips = new HashSet<int>(state.Players.SelectMany(p => p.Crew.All()).Select(c => c.Uid));
            foreach (int uid in state.LockedNostromi.Concat(state.NostromiFreedByMedico))
                if (!onShips.Contains(uid))
                    errors.Add("Il Nostromo #" + uid + " è fuori dalle navi ma risulta ancora bloccato o liberato.");
        }

        /// <summary>
        /// I segnalini sono la somma delle fonti (battaglie, missioni, Tesoro: servono al dettaglio di <see cref="GameResult"/>),
        /// e ogni missione in mano ha il suo avanzamento (R-131), nessuna missione completata lo ha ancora.
        /// </summary>
        private static void CheckBountyAndMissions(GameState state, List<string> errors)
        {
            int tracked = 0;
            foreach (PlayerState player in state.Players)
            {
                int sources = player.BountyBySource.Values.Sum();
                if (sources != player.BountyTokens)
                    errors.Add("p" + player.Id + ": segnalini " + player.BountyTokens + ", ma le fonti ne sommano " + sources + ".");

                foreach (MissionCard card in player.Missions)
                {
                    tracked++;
                    if (!state.MissionProgress.TryGetValue(card.Uid, out MissionProgress progress) || progress.Owner != player.Id)
                        errors.Add("p" + player.Id + ": la missione " + card + " non è tracciata.");
                }
            }

            if (state.MissionProgress.Count != tracked)
                errors.Add("Avanzamento per " + state.MissionProgress.Count + " missioni, ma in mano ce ne sono " + tracked + ".");
        }

        /// <summary>La Fase 2 dà un turno a ogni giocatore esattamente una volta (completa quando il round dopo è iniziato).</summary>
        private static void CheckTurnsPlayed(GameState state, List<string> errors)
        {
            if (state.TurnsPlayed.Distinct().Count() != state.TurnsPlayed.Count)
                errors.Add("Un giocatore ha giocato due turni nella stessa Fase 2: [" + string.Join(",", state.TurnsPlayed) + "]");
            bool previousPhase2Done = state.Phase == GamePhase.Preparation && state.Round > 1;
            if (previousPhase2Done && state.TurnsPlayed.Count != state.PlayerCount)
                errors.Add("Nella Fase 2 del round " + (state.Round - 1) + " hanno giocato " + state.TurnsPlayed.Count +
                           " giocatori su " + state.PlayerCount + ".");
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
