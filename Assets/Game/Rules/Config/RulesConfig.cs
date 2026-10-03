using System;
using System.Collections.Generic;
using hp55games.MareIgnoto.Rules.Cards;

namespace hp55games.MareIgnoto.Rules.Config
{
    /// <summary>Copie e costo di una carta Pirateria (03_contenuti.md §2). Il costo vale solo per le carte Meteo (R-121).</summary>
    [Serializable]
    public sealed class PirateCardEntry
    {
        public PirateCardId id;
        public int copies;
        /// <summary>Monete pagate al Tesoro per giocare la carta (solo Meteo).</summary>
        public int cost;

        public PirateCardEntry()
        {
        }

        public PirateCardEntry(PirateCardId id, int copies, int cost = 0)
        {
            this.id = id;
            this.copies = copies;
            this.cost = cost;
        }
    }

    /// <summary>Copie, ricompensa e soglia di una missione Corsaro (03_contenuti.md §3).</summary>
    [Serializable]
    public sealed class MissionEntry
    {
        public MissionId id;
        public int copies;
        /// <summary>Segnalini taglia. Per Barbanera! è il valore per ogni avversario.</summary>
        public int reward;
        /// <summary>Soglia numerica della condizione (battaglie, monete, round...). 0 dove la condizione non ne ha.</summary>
        public int threshold;

        public MissionEntry()
        {
        }

        public MissionEntry(MissionId id, int copies, int reward, int threshold = 0)
        {
            this.id = id;
            this.copies = copies;
            this.reward = reward;
            this.threshold = threshold;
        }
    }

    /// <summary>Segnalini taglia di una combinazione poker (03_contenuti.md §4).</summary>
    [Serializable]
    public sealed class PokerScoreEntry
    {
        public PokerHand hand;
        public int tokens;

        public PokerScoreEntry()
        {
        }

        public PokerScoreEntry(PokerHand hand, int tokens)
        {
            this.hand = hand;
            this.tokens = tokens;
        }
    }

    /// <summary>
    /// Tutti i numeri del gioco (01_architettura.md §4). I valori di default sono gli inizializzatori dei campi
    /// e coincidono con 02_regole.md e 03_contenuti.md. Nel motore, nella UI e nei test nessun numero di regola
    /// è scritto altrove: si legge da qui. Il motore non modifica mai la configurazione.
    /// I campi sono pubblici e minuscoli perché Unity li serializza (RulesConfigAsset).
    /// </summary>
    [Serializable]
    public sealed class RulesConfig
    {
        // ---- Giocatori e mappa (R-001, R-002, R-080) ----
        public int minPlayers = 2;
        public int maxPlayers = 8;
        /// <summary>Lato minimo di una mappa valida (05_mappa.md §5).</summary>
        public int minMapSize = 5;

        // ---- Costruzione delle zone meteo (05_mappa.md §3, controllate da MapLayout.Validate) ----
        public int cloudMinSpacing = 2;
        public int cloudRingMinDistance = 3;
        public int cloudSpawnMinDistance = 2;
        public int ringSliceCount = 8;
        public int ringNonConsecutiveMinDistance = 2;
        public int ringSacredMinDistance = 2;
        public int ringIslandMinDistance = 3;

        // ---- Ciurma (R-010) ----
        public int slotsAbove = 2;
        public int slotsBelow = 3;

        // ---- Mazzo Crew (R-003, R-014) ----
        public int jokerCount = 2;
        /// <summary>Valore per l'ordine turno di J, Q, K (R-014).</summary>
        public int[] courtTurnOrderValues = { 11, 12, 13 };
        /// <summary>Valore Commercio di J, Q, K (R-014).</summary>
        public int courtCommerceValue = 10;
        public int jokerTurnOrderValue = 0;
        public int jokerCommerceValue = 0;

        // ---- Mazzo Pirateria (R-004) e Corsaro (R-005) ----
        public List<PirateCardEntry> pirateCards = DefaultPirateCards();
        public List<MissionEntry> missions = DefaultMissions();

        // ---- Preparazione (R-032–R-035) ----
        public int startingCrewCards = 2;
        public int startingPirateCards = 3;
        public int startingMissionsDrawn = 3;
        public int startingMissionsMinKept = 1;
        public int startingCoins = 10;

        // ---- Movimento (R-060–R-064) ----
        public int baseSpeed = 1;
        public int windSpeedBonus = 1;
        public int helmsmanSpeedBonus = 1;

        // ---- Abbordaggio fortuito (R-070, R-071) ----
        public int boardingTwoShipsCrewLoss = 1;
        public int boardingTwoShipsPirateLoss = 2;
        public int boardingManyShipsCrewLoss = 1;
        public int boardingManyShipsPirateLoss = 1;
        /// <summary>
        /// Protezione tecnica (R-073a): quanti Abbordaggi a catena, o ripetizioni per tiri uguali (R-070), si risolvono
        /// al massimo in un round; oltre, le navi restano dove sono e il motore emette un evento di diagnostica.
        /// </summary>
        public int maxAbbordaggioChain = 50;

        // ---- Meteo: livelli delle zone (R-038, R-081, R-088) ----
        /// <summary>Livello iniziale degli spicchi dell'anello (R-038, R-081); le nuvole partono da 0.</summary>
        public int ringInitialLevel = 5;
        /// <summary>Livello da cui una zona è Mare Mosso (R-081).</summary>
        public int roughSeaLevel = 1;
        /// <summary>Livello da cui una zona è Tempesta (R-081).</summary>
        public int stormLevel = 2;
        /// <summary>Invocazione di Gartya: livello raggiunto, solo da sotto (R-088).</summary>
        public int invocationLevel = 1;
        /// <summary>Ira di Gartya: livello raggiunto, solo da sotto (R-088).</summary>
        public int wrathLevel = 2;
        /// <summary>Favore di Gartya: livelli tolti, fino a 0 (R-088).</summary>
        public int favorLevelDrop = 1;

        // ---- Meteo (R-083–R-085) ----
        public int roughSeaPirateLoss = 1;
        public int stormBelowDeckCrewLoss = 1;
        public int navigatorWeatherReduction = 1;

        // ---- Fase 2: turno (R-052, R-056, R-058) ----
        public int cabinBoyCoins = 1;
        public int seaDrawCount = 2;
        public int lookoutRange = 1;
        public int buccaneerExtraCards = 1;

        // ---- Porti (R-090–R-095) ----
        public int leisureCost = 3;
        public int recruitDrawCount = 4;
        public int recruitCostKeepOne = 4;
        public int recruitKeepOne = 1;
        public int recruitCostKeepTwo = 8;
        public int recruitKeepTwo = 2;
        public int portMissionDrawCount = 3;
        public int portMissionMinKept = 1;
        public int cookCostDiscount = 1;
        public int cookDrawBonus = 1;

        // ---- Combattimento (R-100–R-111) ----
        public int baseRange = 1;
        public int falconetRange = 1;
        public int sakerRange = 1;
        public int culverinRange = 2;
        public int battleWinTokens = 1;
        public int boardingParryCoins = 5;
        public int gunnerFreeBroadsides = 1;
        public int sakerGainMultiplier = 2;
        public int culverinMinDistance = 2;

        // ---- Carte Pirateria (§7.3, 03 §2) ----
        public int fortunateCatchCoins = 2;
        public int trawlNetDrawCount = 3;
        public int windStepSize = 1;

        // ---- Fine partita e punteggio (R-133, R-140–R-146) ----
        public int treasureTokens = 1;
        public int coinsPerToken = 10;
        public int incompleteMissionPenalty = 1;
        public List<PokerScoreEntry> pokerScores = DefaultPokerScores();
        /// <summary>Punteggio poker fisso con entrambi i Jolly (R-145).</summary>
        public int jokerPairPokerScore = 4;
        /// <summary>Divisore del punteggio poker con un solo Jolly, per difetto (R-145).</summary>
        public int singleJokerPokerDivisor = 2;
        /// <summary>Moltiplicatore del punteggio poker per ogni Nostromo sopra coperta (R-146).</summary>
        public int nostromoPokerMultiplier = 2;
        /// <summary>Ricompensa di Gemelli con i due Jolly (03 §3).</summary>
        public int twinsJokersReward = 4;

        // ---- Sicurezza (R-150) ----
        /// <summary>Numero massimo di round, 0 = nessun limite. Solo per le simulazioni.</summary>
        public int maxRounds = 0;

        public static List<PirateCardEntry> DefaultPirateCards()
        {
            return new List<PirateCardEntry>
            {
                new PirateCardEntry(PirateCardId.Bordata, 20),
                new PirateCardEntry(PirateCardId.Parle, 20),
                new PirateCardEntry(PirateCardId.PescaFortunata, 12),
                new PirateCardEntry(PirateCardId.ReteAStrascico, 8),
                new PirateCardEntry(PirateCardId.Arrembaggio, 8),
                new PirateCardEntry(PirateCardId.UomoInMare, 8),
                new PirateCardEntry(PirateCardId.Spyglass, 4),
                new PirateCardEntry(PirateCardId.SupplicaGartya, 6, 2),
                new PirateCardEntry(PirateCardId.InvocazioneGartya, 6, 5),
                new PirateCardEntry(PirateCardId.IraGartya, 3, 10),
                new PirateCardEntry(PirateCardId.FavoreGartya, 5, 2),
                new PirateCardEntry(PirateCardId.VentoInPoppa, 5, 0),
                new PirateCardEntry(PirateCardId.RafficaCanaglia, 5, 0),
            };
        }

        /// <summary>2 copie per missione (R-005 [DEFAULT]).</summary>
        public static List<MissionEntry> DefaultMissions()
        {
            return new List<MissionEntry>
            {
                new MissionEntry(MissionId.Barbanera, 2, 2),
                new MissionEntry(MissionId.Barbarossa, 2, 1, 5),
                new MissionEntry(MissionId.OlandeseVolante, 2, 2),
                new MissionEntry(MissionId.MaledizionePirata, 2, 2, 2),
                new MissionEntry(MissionId.NaveCorsara, 2, 2, 5),
                new MissionEntry(MissionId.Avido, 2, 1, 30),
                new MissionEntry(MissionId.Avidissimo, 2, 3, 50),
                new MissionEntry(MissionId.SpugnaDiMare, 2, 1, 3),
                new MissionEntry(MissionId.CacciatoreDiTaglie, 2, 2, 3),
                new MissionEntry(MissionId.Attaccabrighe, 2, 3, 3),
                new MissionEntry(MissionId.Bancarotta, 2, 1),
                new MissionEntry(MissionId.DispersiInMare, 2, 1),
                new MissionEntry(MissionId.LupoDiMare, 2, 1),
                new MissionEntry(MissionId.Gemelli, 2, 2),
                new MissionEntry(MissionId.NaveDAssalto, 2, 1),
                new MissionEntry(MissionId.ParlareConIPesci, 2, 3, 3),
                new MissionEntry(MissionId.GambaDiLegno, 2, 3, 3),
            };
        }

        public static List<PokerScoreEntry> DefaultPokerScores()
        {
            return new List<PokerScoreEntry>
            {
                new PokerScoreEntry(PokerHand.HighCard, 0),
                new PokerScoreEntry(PokerHand.Pair, 1),
                new PokerScoreEntry(PokerHand.TwoPair, 2),
                new PokerScoreEntry(PokerHand.ThreeOfAKind, 2),
                new PokerScoreEntry(PokerHand.Straight, 4),
                new PokerScoreEntry(PokerHand.Flush, 4),
                new PokerScoreEntry(PokerHand.FullHouse, 6),
                new PokerScoreEntry(PokerHand.FourOfAKind, 8),
                new PokerScoreEntry(PokerHand.StraightFlush, 10),
                new PokerScoreEntry(PokerHand.RoyalStraightFlush, 12),
            };
        }

        // ---- Meteo ----

        /// <summary>L'effetto di un livello di zona (R-081): Normale, Mare Mosso o Tempesta.</summary>
        public State.WeatherState WeatherAt(int level) =>
            level >= stormLevel ? State.WeatherState.Storm : level >= roughSeaLevel ? State.WeatherState.RoughSea : State.WeatherState.Normal;

        // ---- Accesso per id ----

        public int ShipSlotCount => slotsAbove + slotsBelow;

        public int PirateCopies(PirateCardId id) => FindPirate(id).copies;

        /// <summary>Costo in monete di una carta Meteo (R-121); 0 per le carte non Meteo.</summary>
        public int PirateCost(PirateCardId id) => FindPirate(id).cost;

        public MissionEntry Mission(MissionId id)
        {
            foreach (MissionEntry entry in missions)
                if (entry.id == id) return entry;
            throw new InvalidOperationException("Missione assente dalla configurazione: " + id);
        }

        public int PokerTokens(PokerHand hand)
        {
            foreach (PokerScoreEntry entry in pokerScores)
                if (entry.hand == hand) return entry.tokens;
            throw new InvalidOperationException("Combinazione poker assente dalla configurazione: " + hand);
        }

        private PirateCardEntry FindPirate(PirateCardId id)
        {
            foreach (PirateCardEntry entry in pirateCards)
                if (entry.id == id) return entry;
            throw new InvalidOperationException("Carta Pirateria assente dalla configurazione: " + id);
        }

        /// <summary>Elenco degli errori di configurazione; vuoto se è valida.</summary>
        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();

            if (minPlayers < 1) errors.Add("minPlayers deve essere almeno 1.");
            if (maxPlayers < minPlayers) errors.Add("maxPlayers non può essere minore di minPlayers.");
            if (minMapSize < 3) errors.Add("minMapSize deve essere almeno 3.");
            if (roughSeaLevel < 1 || stormLevel <= roughSeaLevel) errors.Add("Serve 1 <= roughSeaLevel < stormLevel (R-081).");
            if (ringInitialLevel < 0 || invocationLevel < 0 || wrathLevel < 0 || favorLevelDrop < 0)
                errors.Add("I livelli delle zone non possono essere negativi.");
            if (slotsAbove < 1 || slotsBelow < 0) errors.Add("La ciurma richiede almeno 1 slot sopra coperta.");
            if (jokerCount < 0) errors.Add("jokerCount non può essere negativo.");
            if (courtTurnOrderValues == null || courtTurnOrderValues.Length != 3)
                errors.Add("courtTurnOrderValues deve avere 3 valori (J, Q, K).");
            if (startingCrewCards < 0 || startingCrewCards > slotsBelow)
                errors.Add("startingCrewCards deve stare negli slot sotto coperta (R-032).");
            if (startingPirateCards < 0) errors.Add("startingPirateCards non può essere negativo.");
            if (startingMissionsDrawn < 1) errors.Add("startingMissionsDrawn deve essere almeno 1.");
            if (startingMissionsMinKept < 1 || startingMissionsMinKept > startingMissionsDrawn)
                errors.Add("startingMissionsMinKept deve stare tra 1 e startingMissionsDrawn (R-034).");
            if (startingCoins < 0) errors.Add("startingCoins non può essere negativo.");
            if (maxRounds < 0) errors.Add("maxRounds non può essere negativo.");
            if (maxAbbordaggioChain < 1) errors.Add("maxAbbordaggioChain deve essere almeno 1.");
            if (baseSpeed < 0) errors.Add("baseSpeed non può essere negativo.");
            if (coinsPerToken < 1) errors.Add("coinsPerToken deve essere almeno 1.");
            if (singleJokerPokerDivisor < 1) errors.Add("singleJokerPokerDivisor deve essere almeno 1.");

            ValidateEntries(errors, "pirateCards", pirateCards, Enum.GetValues(typeof(PirateCardId)),
                e => e.id, e => e.copies < 0 || e.cost < 0);
            ValidateEntries(errors, "missions", missions, Enum.GetValues(typeof(MissionId)),
                e => e.id, e => e.copies < 0);
            ValidateEntries(errors, "pokerScores", pokerScores, Enum.GetValues(typeof(PokerHand)),
                e => e.hand, e => e.tokens < 0);

            return errors;
        }

        private static void ValidateEntries<TEntry, TId>(List<string> errors, string field, List<TEntry> entries,
            Array allIds, Func<TEntry, TId> idOf, Func<TEntry, bool> isNegative)
        {
            if (entries == null)
            {
                errors.Add(field + " è nullo.");
                return;
            }

            var seen = new HashSet<TId>();
            foreach (TEntry entry in entries)
            {
                if (!seen.Add(idOf(entry))) errors.Add(field + ": id duplicato " + idOf(entry) + ".");
                if (isNegative(entry)) errors.Add(field + ": valori negativi per " + idOf(entry) + ".");
            }

            foreach (TId id in allIds)
                if (!seen.Contains(id)) errors.Add(field + ": manca " + id + ".");
        }
    }
}
