# 0002 — Round, Fase 1: meteo, movimento, Abbordaggio fortuito

Stato: bozza
Commit di riferimento: `e7018c9`
Regole coinvolte: R-040–R-045, R-060–R-073d, R-080–R-087, R-050–R-053 (ordine turno e cornice)
Documenti: `tech/02_regole.md` §4–6, `tech/04_motore.md`, `tech/05_mappa.md`

## Obiettivo

Il motore gioca round completi di sola Fase 1 + ordine della Fase 2: rotte segrete, meteo, movimento simultaneo cella per cella con collisioni e attraversamenti, Abbordaggi fortuiti a 2 e a 3+ navi, ordine turno dal round 2. La Fase 2 in questo task può essere un passaggio vuoto (ogni giocatore "passa"), da riempire in 0003.

## Contratto

- Eventi di movimento per passo (`04` §3), così la vista può animare.
- Le carte crew che toccano questa parte (Timoniere, Navigatore, Medico, Jolly che le duplica) funzionano già qui.
- Vento in Poppa e lo stato "Svago" (R-045) esistono come stato del giocatore, anche se le carte/azioni che li attivano arrivano in 0003: i test possono impostarli direttamente.

## Lato codice (Claude Code)

Solo assembly Rules e test: nessun lavoro Editor, nessun `[SerializeField]`.

**Flussi** (`Assets/Game/Rules/Engine/`, stessa struttura a iteratori della 0001)
- `GameFlow`: ciclo dei round; con `maxRounds > 0` la partita finisce dopo quel round (R-150, evento `GameEndedEvent`, `GameResult` con `EndedByRoundLimit` e `RoundsPlayed`). La fine con l'Isola Sacra è della 0004 (`TODO R-140`).
- `RoundFlow.Run`: dal round 2 ricalcola l'ordine all'inizio del round (vale per le scelte di Fase 1, R-042), poi `Preparation` (rotte R-040/R-041, Svago R-045, meteo, movimento, Abbordaggi) e `Active`.
- `RoundFlow.Active`: ordine ricalcolato a inizio Fase 2 (R-050, dal round 2; nel round 1 resta quello delle offerte), un turno a testa con `TurnStarted`/`TurnEnded`. Cornice: `TurnSkippedEvent` (R-053). Altrimenti Mozzo (R-052), poi il giocatore passa: le azioni del turno sono della 0003 (`TODO spec 0003` nel punto esatto).
- `WeatherFlow` (R-080–R-087): per ogni nave in zona, nell'ordine del round. Navigatore e Jolly abbassano l'intensità percepita; rotazione col d8 o scelta del Timoniere (`RollOrChoose`, riusato dall'Abbordaggio); Mare Mosso → 1 Pirateria, Tempesta → 1 crew sotto coperta. Vento in Poppa: `PlayerState.TailwindRound`.
- `MovementFlow` (R-060–R-069): velocità (`Speed`), passi simultanei, attraversamenti (R-067, decisione `CrossingCell`), terra e collisioni (R-066), arenamento (R-069). Svago: `PlayerState.LeisureRound`.
- `BoardingFlow` (R-070–R-073d): celle con 2+ navi in ordine di turno; a 2 navi perdita a scelta e tiro ripetuto sui pari; a 3+ crew + Pirateria e tiro unico; catene di R-073a in coda. `maxAbbordaggioChain` (nuovo in `RulesConfig`, 50) limita i cicli di un round, ripetizioni comprese.
- `Losses`: ogni perdita di crew passa da `LoseCrew`, che offre il Medico (R-023) se la carta è sopra coperta e non è il Nostromo. Lo riuseranno le carte della 0003.
- `TurnOrder.FromCrew` (R-050/R-051): somma crescente, poi meno monete, meno carte in mano, dado come R-037.

**Decisioni nuove** (`DecisionKind`): `WeatherRotation` e `BoardingReposition` (Timoniere, pubbliche, 8 opzioni), `WeatherPirateLoss`, `StormCrewLoss`, `BoardingLoss`, `UseMedico` (segrete), `CrossingCell` (pubblica). Opzioni Pirateria deduplicate per id (carte uguali = una opzione). `BoardingLossOption` ha liste di slot e di carte, così i numeri di perdita restano in `RulesConfig`.

**Eventi nuovi** (`Events/MovementEvents.cs`): meteo (`WeatherApplied`, `WeatherSkipped`, `HeadingRotated`, `DieChosen`), movimento per passo (`MovementStarted`, `ShipMoved`, `ShipStopped` con il motivo, `CrossingResolved`, `ShipStranded`, `SacredIslandEntered` col passo per R-141, `MovementEnded`), Abbordaggio (`BoardingStarted` con profondità di catena e ripetizione, `ShipRepositioned`, `BoardingChainLimit`), perdite (`CrewLost` privato sotto coperta, con `AtSea` per R-022; `MedicoUsed`; la Pirateria persa usa `CardsDiscardedEvent`), turni (`TurnStarted`, `TurnSkipped`, `TurnEnded`), `GameEnded`. `HeadingsRevealedEvent.Headings` ora è `Heading?` (null per chi è in Svago).

**Stato**: `IReadOnlyPlayerState.LeisureRound` e `TailwindRound` (li imposterà la 0003). Invarianti nuovi: fuori dal movimento nessuna cella di mare con 2+ navi salvo R-071 e limite della catena (`SharedSeaCellsAllowed`); un turno a testa nella Fase 2. L'invariante del Nostromo arriva con gli scambi (0003).

**Test** (`dotnet test Tools/RulesHarness`: **190 verdi**, più 1 `[Explicit]`)
- `RoundScenario` (test): dopo il setup svuota ciurme e mani, mette navi e carte prendendole dai mazzi (la conservazione resta vera), accoda i tiri del round (`QueuedRandom`) e gioca un round con rotte date.
- `MovementTests`, `BoardingTests`, `WeatherTests`, `RoundTests`: ogni regola della spec ha almeno un test con l'ID nel nome, e ci sono tutti i casi limite di `08` §2 per movimento, Abbordaggio, meteo e ordine turno.
- Simulazione: `Simulation200RoundsWithRandomBots` (200 round × 2/4/8 giocatori × 3 seed, invarianti dopo ogni risposta) è nella suite normale. `Simulation1000Seeds` (`[Explicit]`, seed 1–1000 × 2/4/8, 200 round) l'ho lanciata: verde in 43 s.
- Verificato che i test mordono: tolto il −1 del vento contrario (4 rossi), tolta la ripetizione sui pari (1 rosso), attraversamento scelto sempre dalla prima nave (1 rosso).
- I test della 0001 che giocavano "finché non ci sono decisioni" ora si fermano a fine setup (`TestSupport.Play` ha un criterio di stop) oppure usano `maxRounds`. Il determinismo si verifica su 10 round.

## Per Franci — modifiche isolate

- Tornare sull'Editor per i `.meta` dei file nuovi (5 in `Assets/Game/Rules/`, 5 in `Assets/Tests/EditMode/Rules/`) e committarli.
- Test Runner → EditMode, `hp55games.MareIgnoto.Rules.Tests`: deve essere verde come l'harness. `Simulation1000Seeds` è `[Explicit]`: non parte da sola.

## Verifica

- `dotnet test Tools/RulesHarness` verde, con test per ogni caso limite di `08` §2 (movimento, Abbordaggio, meteo, ordine turno).
- La simulazione con bot casuale gioca 200 round × 2/4/8 giocatori senza eccezioni né violazioni di invarianti.

## Report

### Claude Code — 2026-10-02 — commit e7018c9

Il motore gioca round completi: Fase 1 intera (rotte, meteo, movimento cella per cella, Abbordaggi) e Fase 2 con ordine di turno, cornice e Mozzo; le azioni del turno arrivano con la 0003. `dotnet test Tools/RulesHarness` verde (190 test); simulazione 200 round × 2/4/8 verde, gate di `08` §3 (1000 seed) verde a mano. Dettaglio in "Lato codice".

Da sapere per 0003–0004:
- `Pending` adesso è null solo a partita finita. Senza `maxRounds` e senza la fine dell'Isola Sacra (0004) la partita non finisce: i test usano `maxRounds` o un criterio di stop.
- Nella simulazione il meteo non scatta mai, perché le zone cambiano solo con le carte Meteo (0003). È coperto dai test di scenario.
- L'ordine di turno di Fase 2 si emette con un secondo `TurnOrderSetEvent` dello stesso round (dal round 2).
- Per lo Svago (0003): basta mettere `LeisureRound = round + 1`; la Fase 1 lo rispetta già. Il "turno di porto di nuovo" in Fase 2 è della 0003.
- Estensioni del contratto di `04`: `GameResult.EndedByRoundLimit`/`RoundsPlayed`, `RulesConfig.maxAbbordaggioChain` (citato da R-073a), i tipi di decisione e gli eventi elencati sopra.

#### Domande

- **R-066 — rotta che esce dalla mappa**: una nave sulla cornice che punta verso l'esterno (es. in (0,7) verso O) non ha una cella dove andare. Non si muove (`ShipStoppedEvent` con motivo `MapEdge`) e non conta come nuovo arenamento. Va bene, o la scelta della rotta deve escludere quelle direzioni? Lo stesso succede quando il meteo ruota la rotta verso fuori. `// TODO R-066` in `MovementFlow`.
- **R-067 — chi sceglie la cella**: ho letto "il giocatore con il valore più basso secondo l'ordine turno" come chi viene prima nell'ordine di turno del round, cioè quello delle offerte nel round 1. Senza dadi a metà movimento. Inoltre l'attraversamento conta solo tra due celle di mare: se una delle due è terra, vale R-068 e nessuno collide.
- **R-084 — Tempesta e carte Pirateria**: "come Mare Mosso per la rotazione; inoltre perde 1 crew sotto coperta". L'ho letta come rotazione + crew sotto coperta, **senza** la carta Pirateria del Mare Mosso. Così una Tempesta abbassata dal Navigatore costa la Pirateria e non la crew. Se la Tempesta deve togliere anche la Pirateria, è una riga.
- **R-073c — scelta di un tipo che non si ha**: nell'Abbordaggio a 2 navi offro solo i tipi di carta che il giocatore ha (senza crew deve perdere Pirateria). Letto alla lettera ("perde quelle che ha") si potrebbe scegliere il tipo che manca e non perdere nulla. Quale delle due?
- **R-140 — riposizionamento sull'Isola Sacra**: dopo un Abbordaggio una nave può finire su una cella dell'Isola Sacra (adiacente). Conta come "entrare durante il movimento" e fa finire la partita? Per ora no, e non emetto `SacredIslandEnteredEvent`. `// TODO R-140` in `BoardingFlow.Move`, da decidere per la 0004.
- **R-073a con R-071**: se un riposizionamento a catena (Abbordaggio a 2) finisce su una cella dove ci sono solo navi già riposizionate da un Abbordaggio a 3+ nello stesso round, ho fatto prevalere R-071: nessun nuovo Abbordaggio, la cella resta condivisa. Se la cella d'arrivo ha 2+ navi non ancora sistemate, l'Abbordaggio a catena le coinvolge tutte, con la regola delle 3+.
- **R-069 — arenamento**: l'evento lo emetto solo se la nave si è mossa o è stata riposizionata in quel round. Una nave che resta ferma sulla cornice non si "riarena" ogni round (conta per "Gamba di legno").

#### Note

- Il limite `maxAbbordaggioChain` conta tutti i cicli di Abbordaggio del round, ripetizioni sui pari comprese. Senza questo, due Timonieri che scelgono sempre lo stesso numero bloccherebbero il motore. Nei 3000 giochi simulati il limite non è mai scattato.
- L'Isola Sacra vale come porto per R-062 (niente vento in partenza). È irrilevante finché entrarci non chiude la partita.
- Non ho toccato i file non miei rimasti modificati o nuovi nella working copy (`Assets/Game/Content/`, `Assets/Scenes/Board.unity`, `specs/0005`).
