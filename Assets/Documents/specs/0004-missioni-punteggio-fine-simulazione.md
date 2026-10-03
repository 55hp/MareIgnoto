# 0004 — Missioni, fine partita, punteggio, simulazione

Stato: bozza
Commit di riferimento: `0965837`
Regole coinvolte: R-130–R-147, R-150
Documenti: `tech/02_regole.md` §8–10, `tech/03_contenuti.md` §3–4, `tech/08_test-e-simulazione.md`

## Obiettivo

Partite complete dall'inizio alla fine: missioni Corsaro con tracciamento e completamento immediato, Isola Sacra e Tesoro, round di cortesia, punteggio finale con poker, Jolly e Nostromo, `GameResult` con il dettaglio della taglia per giocatore. `SimulationRunner` completo.

## Contratto

- `GameResult`: classifica, vincitore/i, e per ogni giocatore il dettaglio delle fonti (segnalini da battaglie, missioni, Tesoro; monete/10; missioni incomplete; poker con combinazione riconosciuta) — serve alla schermata finale e al tutorial.
- `SimulationRunner` come `08` §3, con report testuale.

## Lato codice (Claude Code)

Solo assembly Rules e test: nessun lavoro Editor, nessun `[SerializeField]`.

**Correzioni prima della 0004** (`tech/02` R-016/R-023 e `04` §6 aggiornati il 03/10)
- Medico (R-023): `Losses.LoseCrew` lo offre per ogni perdita di una carta **sopra coperta**, Nostromo compreso. Le perdite sotto coperta (Tempesta, Uomo in mare! su uno slot coperto) e gli scambi (swap, Spyglass!, Quartiermastro) non passano da `LoseCrew`, quindi non lo chiamano mai.
- Blocco del Nostromo (R-016): `GameState.LockedNostromi` (sopra coperta, bloccati) e `NostromiFreedByMedico` (riportati sotto dal Medico). L'unico punto che sblocca è `GameContext.FreeNostromoByMedico`, chiamato solo dal Medico; `PutCrew` lancia un'eccezione se qualcuno prova a mettere sotto un Nostromo bloccato, e quando un Nostromo risale lo blocca di nuovo. Invariante (`Invariants.CheckNostromo`): sotto coperta mai un Nostromo bloccato; sopra coperta mai un Nostromo non bloccato; nessun Nostromo nei due insiemi insieme.

**Missioni** (`Engine/Missions.cs`)
- `MissionProgress` per ogni missione in mano, creato da `Missions.Track` quando il giocatore la tiene (setup, porto, `RoundScenario.Mission` nei test): da lì conta (R-131).
- `Missions.Observe` riceve ogni evento da `GameContext.Emit` e aggiorna i contatori di `03` §3 (battaglie vinte/perse, avversari battuti, perdite crew nel round e in mare, Bordate nel round, Svaghi, round consecutivi con battaglia, celle di arenamento, monete e mano a zero o sopra soglia).
- `Missions.CompleteReady` completa le missioni la cui condizione è vera nei **punti sicuri**: `FlowRunner` lo chiama ogni volta che si ferma per una decisione (`beforePause`), e `EndFlow` a fine partita. Così il completamento è immediato (R-132) ma non vede mai stati a metà di un'operazione (uno scambio di slot fatto in due passi). Le missioni di stato (Avido, Avidissimo, Bancarotta, Dispersi in mare, Gemelli, Nave d'assalto) scattano quando la condizione **passa da falsa a vera** dopo che la missione è stata tenuta (R-131, `MissionProgress.StateWasTrue`): monete e mano si controllano a ogni evento, quindi conta anche un passaggio che dura un istante (Pesca Fortunata a 0 carte, poi Rete a Strascico); la ciurma nei punti sicuri. Cacciatore di Taglie conta le completate di tutta la partita. Il completamento si ripete finché ne completa, per Cacciatore di Taglie.
- Completamento: la carta passa da `Missions` a `Completed`, `MissionCompletedEvent` (pubblico: la carta si rivela) e `BountyChangedEvent` con `BountyReason.Mission`. Ricompense da `RulesConfig.missions`; Barbanera! × avversari, Gemelli coi due Jolly `twinsJokersReward`.
- Olandese Volante si valuta solo a fine partita (`EvaluateAtGameEnd`), con `AtGameEnd` nell'evento.

**Fine partita** (`Engine/EndFlow.cs`, `GameFlow`, `MovementFlow`, `RoundFlow`)
- `MovementFlow` registra in `GameState.SacredIslandArrivals` il passo d'ingresso di ogni nave nell'Isola Sacra (R-140); `BoardingFlow.Move` registra chi un Abbordaggio (a 2 o a 3+ navi) riposiziona sull'Isola Sacra, col passo convenzionale `BoardingArrivalStep`, dopo ogni passo di movimento (R-073a, R-141). Gli eventi hanno `ByBoarding`.
- Dopo movimento e Abbordaggi, `EndFlow.TakeTreasure`: il Tesoro va a chi è entrato nel passo più basso; a parità ognuno prende `treasureTokens` e le monete si dividono per difetto, il resto esce dal gioco (R-141). `TreasureTakenEvent`, `CoinReason.Treasure`, `BountyReason.Treasure`. Le carte Meteo giocate dopo (round di cortesia) si pagano ma le monete escono dal gioco (R-142).
- Il round si completa: Abbordaggi e Fase 2 per tutti (round di cortesia, R-142; chi è sull'Isola Sacra salta il turno, R-055). Poi `EndFlow.End`: missioni di fine partita, punteggio, `FinalScoreEvent` per giocatore, `GameEndedEvent` con motivo `SacredIsland` e vincitori. Anche lo stop per `maxRounds` passa da `EndFlow.End` (motivo `RoundLimit`), così la simulazione ha il punteggio di tutte le partite.

**Punteggio** (`Engine/Scoring.cs`, `Engine/GameResult.cs`)
- `GameResult`: `Reason`, `EndedByRoundLimit`, `RoundsPlayed`, `Ranking` (classifica), `Winners`, `TreasureTakers`, `ScoreOf(player)`.
- `PlayerScore` (R-143): `BattleTokens`, `MissionTokens`, `TreasureTokens` (le fonti si accumulano in `PlayerState.BountyBySource` da `ChangeBounty`), `Coins`/`CoinTokens`, `IncompleteMissions`/`MissionPenalty`, `Poker`, `TookTreasure`, `Total`, `Rank`.
- `PokerScore` (R-144–R-146): `Hand` (la combinazione riconosciuta), `TableScore`, `Jokers`, `JokerPair` (punteggio fisso), `HalvedByJoker`, `AfterJokers`, `NostromoMultiplier`, `Score`. La combinazione è quella che vale più segnalini tra tutte quelle che le carte formano; col Jolly si provano tutte le 52 carte possibili. Un solo Jolly fa parte della combinazione (e dimezza, R-145) solo se la combinazione col Jolly dimezzata vale più di quella delle altre carte; il Jolly sopra coperta accanto al Nostromo conta comunque come secondo Nostromo (R-146): circa ×2 se è nella combinazione, ×4 se no. Mozzo = asso basso e alto, niente scale che girano, scala e colore solo con 5 carte.
- Classifica (R-147): taglia decrescente; a parità vince chi ha preso il Tesoro; le altre parità restano (stesso `Rank`, più vincitori).

**Invarianti nuovi**: i segnalini di ogni giocatore sono la somma delle fonti; ogni missione in mano ha il suo avanzamento e nessun'altra; partita finita ⇒ esito presente.

**Simulazione** (`Bots/SimulationRunner.cs`, `08` §3)
- `SimulationRunner.Run(SimulationOptions)` gioca le partite (numeri di giocatori, seed da–a, `RulesConfig` con `maxRounds`, mappa), controlla gli invarianti dopo ogni risposta, raccoglie eccezioni, violazioni e partite bloccate (`SimulationFailure`) e restituisce un `SimulationReport` con `Clean` e `ToText()`: partite finite e interrotte per `maxRounds` con i seed, round min/media/mediana/max, fonti della taglia, missioni completate, vincitore, combinazioni poker.
- È nell'assembly Rules (niente I/O), così potrà usarlo anche un menu Editor. Nei test: `SimulationRunnerFinishesGamesAndReports` (15 seed × 2/4/8, nella suite) e `Simulation1000Seeds` (`[Explicit]`, il gate, stampa il report).

**Test** (`dotnet test Tools/RulesHarness`: **354 verdi**, più 1 `[Explicit]`)
- `NostromoMedicoTests`: Medico che salva il Nostromo (Abbordaggio, Arrembaggio!), Nostromo liberato che risale con uno swap e torna bloccato, Medico mai chiamato da Tempesta, perdite sotto coperta, Spyglass! e Quartiermastro, Quartiermastro che sposta un Nostromo bloccato solo sopra, l'invariante che rileva le due violazioni.
- `MissionTests`: ogni missione completata e non completata, R-131 (eventi prima della pesca), l'eccezione di Cacciatore di Taglie con la catena dopo un'altra missione, Olandese Volante a fine partita (anche con zero battaglie), missione tenuta al porto.
- `ScoringTests`: ogni combinazione, scale con l'asso alto e basso, niente scala che gira, scala/colore con meno di 5 carte, un Jolly (carta jolly e dimezzato), due Jolly (4), Nostromo che raddoppia (per copia, dopo il Jolly).
- `EndGameTests`: fine con l'Isola Sacra e round di cortesia, Tesoro, passo più basso, parità di passo, taglia con tutte le fonti, le due parità di R-147, punteggio anche allo stop per `maxRounds`.
- Le simulazioni della 0002/0003 ora finiscono le partite (Isola Sacra o `maxRounds`). `RoundScenario.Create` toglie anche le missioni del setup, così ogni test dà quelle che gli servono.
- Verificato che i test mordono: Medico che esclude di nuovo il Nostromo (3 rossi), Nostromo che risale senza tornare bloccato (15 rossi), missioni senza punto sicuro (22 rossi), asso alto rotto (3 rossi), Tesoro solo al primo a parità di passo (1 rosso).

## Per Franci — modifiche isolate

- Tornare sull'Editor per i `.meta` dei file nuovi e committarli: 5 in `Assets/Game/Rules/` (`Engine/Missions.cs`, `Engine/Scoring.cs`, `Engine/EndFlow.cs`, `Events/EndEvents.cs`, `Bots/SimulationRunner.cs`) e 4 in `Assets/Tests/EditMode/Rules/` (`NostromoMedicoTests.cs`, `MissionTests.cs`, `ScoringTests.cs`, `EndGameTests.cs`).
- Test Runner → EditMode, `hp55games.MareIgnoto.Rules.Tests`: deve essere verde come l'harness. `Simulation1000Seeds` è `[Explicit]` (circa 1 minuto nell'harness).

## Verifica

- `dotnet test Tools/RulesHarness` verde, con test di ogni missione (completata e no), R-131, ogni combinazione poker, Jolly, Nostromo, parità (R-147).
- **Gate di Fase 1 del piano**: simulazione di 1000 partite (seed 1–1000, 2/4/8 giocatori) senza eccezioni né violazioni di invarianti. Allegare al report il riepilogo statistico.

## Report

### Claude Code — 2026-10-03 — commit 0965837

Le partite finiscono: missioni Corsaro, Isola Sacra con Tesoro e round di cortesia, punteggio con poker, Jolly e Nostromo, `GameResult` con classifica e dettaglio della taglia, `SimulationRunner`. Prima, le due correzioni chieste su Medico e Nostromo. `dotnet test Tools/RulesHarness` verde (354 test); gate di `08` §3 verde: 3000 partite (seed 1–1000 × 2/4/8 giocatori) senza eccezioni né violazioni di invarianti. Dettaglio in "Lato codice".

**Corretto prima della 0004** (con test, `NostromoMedicoTests`):
1. **Medico (R-023)**: interviene solo sulla perdita di una carta crew sopra coperta, e ora anche quando la carta minacciata è il Nostromo (prima lo escludevo). Tempesta, perdite sotto coperta e scambi (swap, Spyglass!, Quartiermastro) non lo chiamano.
2. **Blocco del Nostromo (R-016, `04` §6)**: l'invariante ammette una sola via verso il basso, il Medico. Lo swap del proprietario, Spyglass! e il Quartiermastro non lo portano sotto; un Nostromo riportato sotto dal Medico risale con un normale swap e torna bloccato. Se un flusso provasse a mettere sotto un Nostromo bloccato, il motore lancia un'eccezione (la simulazione la riporterebbe come errore).

**Gate di Fase 1 (`08` §3)**: `Simulation1000Seeds`, seed 1–1000 × 2/4/8 giocatori, `maxRounds` 500, mappa di test (`TestSupport.StandardMap`, la mappa vera non esiste ancora), 50 s. **Nessuna eccezione, nessuna violazione di invarianti.** Riepilogo:

```
Simulazione: seed 1-1000, maxRounds 500, bot casuale (seed del bot = seed della partita)

2 giocatori: 1000 partite, 915 finite con l'Isola Sacra, 85 interrotte da maxRounds, 0 errori
  round (partite finite): min 17, media 183.92, mediana 154, max 500
  interrotte da maxRounds, seed: 12, 15, 18, 25, 36, 44, 50, 52, 54, 60, 76, 125, 143, 175, 191, 193, 204, 207, 241, 285, 287, 310, 323, 346, 362, 367, 380, 393, 402, 425, 434, 437, 439, 440, 442, 455, 463, 487, 491, 493, 498, 526, 553, 566, 572, 591, 596, 616, 620, 636, 646, 649, 654, 655, 694, 696, 711, 716, 733, 734, 751, 776, 779, 782, 784, 802, 804, 811, 839, 850, 857, 867, 879, 882, 884, 894, 900, 905, 917, 919, 921, 953, 977, 982, 995
  taglia media per giocatore: 3.12 = battaglie 0.76 + missioni 1.57 + Tesoro 0.46 + monete 3.02 - missioni incomplete 2.73 + poker 0.05
  quota delle fonti positive: battaglie 13.0%, missioni 26.8%, Tesoro 7.8%, monete 51.5%, poker 0.8%
  missioni completate per partita: 1.63; vincitore: taglia media 7.00, ha preso il Tesoro nel 85.7% delle partite; parità al primo posto: 4
  poker: HighCard 92.7%, Pair 7.0%, TwoPair 0.2%, ThreeOfAKind 0.2%

4 giocatori: 1000 partite, 997 finite con l'Isola Sacra, 3 interrotte da maxRounds, 0 errori
  round (partite finite): min 9, media 110.80, mediana 85, max 488
  interrotte da maxRounds, seed: 737, 826, 880
  taglia media per giocatore: 2.07 = battaglie 0.70 + missioni 0.93 + Tesoro 0.25 + monete 2.18 - missioni incomplete 2.05 + poker 0.06
  quota delle fonti positive: battaglie 17.0%, missioni 22.7%, Tesoro 6.1%, monete 52.9%, poker 1.3%
  missioni completate per partita: 1.92; vincitore: taglia media 9.14, ha preso il Tesoro nel 94.4% delle partite; parità al primo posto: 4
  poker: HighCard 92.0%, Pair 7.9%, ThreeOfAKind 0.2%

8 giocatori: 1000 partite, 1000 finite con l'Isola Sacra, 0 interrotte da maxRounds, 0 errori
  round (partite finite): min 7, media 59.86, mediana 48, max 296
  taglia media per giocatore: 1.35 = battaglie 0.77 + missioni 0.64 + Tesoro 0.13 + monete 1.47 - missioni incomplete 1.70 + poker 0.05
  quota delle fonti positive: battaglie 25.1%, missioni 20.9%, Tesoro 4.1%, monete 48.3%, poker 1.5%
  missioni completate per partita: 2.65; vincitore: taglia media 11.38, ha preso il Tesoro nel 98.3% delle partite; parità al primo posto: 1
  poker: HighCard 91.9%, Pair 8.1%, TwoPair 0.0%, ThreeOfAKind 0.0%

Errori: nessuno
```

Lettura: con bot casuali le navi vagano a caso, quindi le partite a 2 sono lunghe e 85 su 1000 non arrivano all'Isola Sacra entro 500 round (a 4 giocatori 3, a 8 nessuna); sono partite interrotte, non errori. Le statistiche sono di bot casuali: utili per trovare errori, non per il bilanciamento (per esempio il poker è quasi sempre "carta alta" perché i bot vendono e perdono la ciurma, e le missioni incomplete pesano perché i bot tengono missioni a caso).

Estensioni del contratto di `04` (non ho modificato `04`, che ha modifiche di Athena non committate): `GameResult` (`Reason`, `Ranking`, `Winners`, `TreasureTakers`, `ScoreOf`), `PlayerScore`, `PokerScore`; `GameEndReason.SacredIsland`; eventi `MissionCompletedEvent`, `TreasureTakenEvent`, `FinalScoreEvent`; `GameEndedEvent.Winners`; `BountyReason.Mission`/`Treasure`, `CoinReason.Treasure`. `GameResult(bool, int)` non esiste più.

#### Domande

- **R-131 — condizioni sullo stato già vere quando si pesca la missione** (Avido, Avidissimo, Bancarotta, Dispersi in mare, Gemelli, Nave d'assalto): le ho lette come stati, quindi una missione tenuta mentre la condizione è già vera si completa subito (per esempio Avido pescato con 30 monete). L'alternativa è richiedere che la condizione diventi vera *dopo* la pesca ("è arrivato a 0 monete"). Test: `AStateConditionAlreadyTrueWhenTheMissionIsKeptCompletesAtOnce_R131`.
- **R-146 — Jolly e Nostromo sopra coperta**: il Jolly fa da carta jolly per il poker (R-145, punteggio dimezzato) e duplica anche il Nostromo (R-015), quindi il punteggio si moltiplica per 4. È coerente con Saker + Jolly ×4, ma R-015 dice "effetti di fine partita: §9" e si potrebbe leggere che a fine partita il Jolly valga solo per R-145 (×2). Test: `TheNostromoDoublesAfterTheJokerHalving_R145_R146`.
- **R-147 — chi "ha raggiunto l'Isola Sacra"**: per la parità conta chi ha preso il Tesoro. Una nave entrata nello stesso round ma a un passo più alto (R-141) non vince le parità.
- **R-140 — quando si prende il Tesoro**: subito dopo il movimento, prima degli Abbordaggi e del round di cortesia. Le monete delle carte Meteo giocate nel round di cortesia finiscono nel Tesoro e non vanno a nessuno.
- **R-140 — riposizionamento sull'Isola Sacra** (domanda aperta dalla 0002): dopo un Abbordaggio una nave può finire su una cella dell'Isola Sacra. Per ora non è "entrare durante il movimento" e la partita non finisce; se dal round dopo si muove su un'altra cella dell'Isola Sacra, entra e la partita finisce. `// TODO R-140` in `BoardingFlow.Move`.
- **Nave Corsara**: contano tutte le Bordate giocate in combattimento (apertura dalla mano, gratuite di Cannoniere e Falconet, risposte del duello). La Bordata! scartata per il Quartiermastro non conta: è scartata, non giocata.
- **Cacciatore di Taglie**: conta le altre missioni completate in tutta la partita, anche quelle completate a fine partita (Olandese Volante), quindi può completarsi a catena nel conteggio finale.
- **R-020/R-023 — sostituzione volontaria**: se un giocatore riceve una crew e sceglie di sostituire una sua carta sopra coperta, è una perdita (R-021) ma il Medico non è offerto, perché la scelta è sua. Va bene?
- **R-016 — Nostromo bloccato che torna in un mazzo**: un Nostromo bloccato che viene perso (scarti), rimescolato e poi ripescato resta bloccato, quindi chi lo riceve può metterlo solo sopra coperta. L'alternativa è che il blocco valga solo finché la carta resta su una nave.

#### Note

- Le missioni contano gli eventi: Barbanera! e Barbarossa! solo le battaglie vinte (R-104, non Arrembaggio!), Attaccabrighe solo le battaglie di Bordate (R-103/R-106) da attaccante o difensore, Maledizione pirata e Parlare con i pesci ogni perdita di R-021 (anche Arrembaggio!, Uomo in mare! e sostituzioni), Gamba di legno gli `ShipStrandedEvent` (anche dopo un riposizionamento).
- `RoundScenario.Create` ora toglie le missioni del setup: i test di scenario non completano missioni per caso.
- Non ho toccato i file di altri rimasti modificati nella working copy (`tech/00`, `02`, `03`, `04`).

### Claude Code — 2026-10-03 — commit 4a25416

Emendamento (`tech/00`, registro del 02/10 (4)): quattro modifiche, con test. `dotnet test Tools/RulesHarness` verde (360 test).

1. **R-131 (errore mio)**: le missioni di stato (Avido, Avidissimo, Bancarotta, Dispersi in mare, Gemelli, Nave d'assalto) si completano solo quando la condizione **diventa** vera dopo che la missione è stata tenuta. Se è già vera quando si tiene, non si completa finché non diventa falsa e poi di nuovo vera. Test: `AStateConditionAlreadyTrueWhenKeptMustGoFalseAndTrueAgain_R131`, `ACrewConditionAlreadyTrueWhenKeptDoesNotComplete_R131`; Gemelli e Nave d'assalto ora si provano con uno swap dopo la pesca.
2. **R-140/R-073a/R-141**: una nave riposizionata da un Abbordaggio sull'Isola Sacra la raggiunge: prende il Tesoro e la partita finisce a fine round. Arriva dopo l'ultimo passo di movimento: chi è entrato in movimento ha la precedenza; tra arrivi per Abbordaggio la parità divide il Tesoro come in R-141. Vale anche nell'Abbordaggio a 3+ navi (R-071). Il Tesoro ora si assegna dopo gli Abbordaggi (prima: subito dopo il movimento). Tolto il `// TODO R-140` in `BoardingFlow`. Test: `AShipRepositionedOntoTheSacredIslandTakesTheTreasureAndEndsTheGame_R073a_R140`, `AnArrivalByMovementBeatsAnArrivalByBoarding_R141`, `BoardingArrivalsTieAndSplitTheTreasureAlsoWithThreeShips_R071_R141`.
3. **R-142**: le carte Meteo giocate nel round di cortesia si pagano normalmente e le monete escono dal gioco (il Tesoro è già assegnato). Test: `MeteoCardsInTheCourtesyRoundArePaidButTheCoinsLeaveTheGame_R142`.
4. **R-146**: il Jolly sopra coperta accanto al Nostromo conta come secondo Nostromo. Se il Jolly fa parte della combinazione, il dimezzamento di R-145 resta (prima il dimezzamento, poi i raddoppi: circa ×2); ×4 pieno solo se non ne fa parte. "Fa parte" l'ho reso così: un solo Jolly entra nella combinazione quando la combinazione col Jolly, dimezzata, vale più di quella delle altre carte da sole; altrimenti il Jolly resta fuori e non dimezza. Effetto anche senza Nostromo: per esempio quattro 9 + Jolly valgono il poker intero (8), non 4. Test: `AJokerNextToTheNostromoOutsideTheCombinationGivesTheFullX4_R146`, `AJokerThatAddsNothingAfterHalvingIsNotPartOfTheCombination_R145_R146`, `TheNostromoDoublesAfterTheJokerHalving_R145_R146`.

Verificato che i test mordono: R-131 come prima (2 rossi), nessun arrivo per Abbordaggio (2 rossi), Meteo del round di cortesia al Tesoro (1 rosso), Jolly che dimezza sempre (3 rossi).

**Gate rilanciato** (`Simulation1000Seeds`, seed 1–1000 × 2/4/8, `maxRounds` 500, mappa di test, 48 s): **nessuna eccezione, nessuna violazione di invarianti**. Partite interrotte invariate (85 / 3 / 0, stessi seed). Nel poker "Coppia" cala a favore di "Carta alta": un Jolly con una sola carta faceva una coppia dimezzata a 0, ora resta fuori; i punti non cambiano.

```
Simulazione: seed 1-1000, maxRounds 500, bot casuale (seed del bot = seed della partita)

2 giocatori: 1000 partite, 915 finite con l'Isola Sacra, 85 interrotte da maxRounds, 0 errori
  round (partite finite): min 17, media 183.92, mediana 154, max 500
  interrotte da maxRounds, seed: 12, 15, 18, 25, 36, 44, 50, 52, 54, 60, 76, 125, 143, 175, 191, 193, 204, 207, 241, 285, 287, 310, 323, 346, 362, 367, 380, 393, 402, 425, 434, 437, 439, 440, 442, 455, 463, 487, 491, 493, 498, 526, 553, 566, 572, 591, 596, 616, 620, 636, 646, 649, 654, 655, 694, 696, 711, 716, 733, 734, 751, 776, 779, 782, 784, 802, 804, 811, 839, 850, 857, 867, 879, 882, 884, 894, 900, 905, 917, 919, 921, 953, 977, 982, 995
  taglia media per giocatore: 3.11 = battaglie 0.76 + missioni 1.56 + Tesoro 0.46 + monete 3.02 - missioni incomplete 2.73 + poker 0.05
  quota delle fonti positive: battaglie 13.1%, missioni 26.7%, Tesoro 7.9%, monete 51.6%, poker 0.8%
  missioni completate per partita: 1.62; vincitore: taglia media 6.98, ha preso il Tesoro nel 85.7% delle partite; parità al primo posto: 4
  poker: HighCard 96.9%, Pair 2.9%, TwoPair 0.2%, ThreeOfAKind 0.1%

4 giocatori: 1000 partite, 997 finite con l'Isola Sacra, 3 interrotte da maxRounds, 0 errori
  round (partite finite): min 9, media 110.79, mediana 85, max 488
  interrotte da maxRounds, seed: 737, 826, 880
  taglia media per giocatore: 2.05 = battaglie 0.70 + missioni 0.93 + Tesoro 0.25 + monete 2.18 - missioni incomplete 2.05 + poker 0.06
  quota delle fonti positive: battaglie 17.0%, missioni 22.6%, Tesoro 6.1%, monete 53.0%, poker 1.3%
  missioni completate per partita: 1.90; vincitore: taglia media 9.13, ha preso il Tesoro nel 94.2% delle partite; parità al primo posto: 4
  poker: HighCard 96.0%, Pair 4.0%, ThreeOfAKind 0.0%

8 giocatori: 1000 partite, 1000 finite con l'Isola Sacra, 0 interrotte da maxRounds, 0 errori
  round (partite finite): min 7, media 59.85, mediana 48, max 296
  taglia media per giocatore: 1.34 = battaglie 0.77 + missioni 0.63 + Tesoro 0.13 + monete 1.47 - missioni incomplete 1.70 + poker 0.05
  quota delle fonti positive: battaglie 25.2%, missioni 20.8%, Tesoro 4.1%, monete 48.4%, poker 1.5%
  missioni completate per partita: 2.62; vincitore: taglia media 11.37, ha preso il Tesoro nel 98.2% delle partite; parità al primo posto: 1
  poker: HighCard 96.4%, Pair 3.6%, TwoPair 0.0%, ThreeOfAKind 0.0%

Errori: nessuno
```

#### Domande

- **R-146 — quando il Jolly "fa parte della combinazione"**: a parità (la combinazione col Jolly dimezzata vale quanto quella senza) lo tengo fuori. Il punteggio non cambia, cambia solo la combinazione mostrata (coppia invece di tris).

### Claude Code — 2026-10-03 — commit 854f413

R-145 (patch del 02/10 (5)): verificato, nessun comportamento nuovo. `Scoring.Poker` sceglie la lettura che dà il punteggio poker finale più alto e a parità lascia fuori il Jolly, anche con i Nostromi: il moltiplicatore di R-146 è identico nelle due letture, quindi confrontarle prima dei raddoppi dà lo stesso esito. Aggiunto `OnATieTheJokerStaysOutAlsoWithTheNostromoMultipliers_R145_R146` (morde: con `>=` al posto di `>` diventa rosso); `dotnet test Tools/RulesHarness` verde, 361 test.

### Claude Code — 2026-10-03 — commit bd9b891

Riordino dei file (commit "chore: CLAUDE.md alla radice, README di radice"):
- `CLAUDE.md` spostato da `Assets/` alla radice del repo (`git mv`), rimosso `Assets/CLAUDE.md.meta`. Nessun documento citava il percorso `Assets/CLAUDE.md`, né i miei né quelli in `tech/`.
- `README.md` di radice: esiste già (contiene solo il titolo "# MareIgnoto"), quindi non l'ho creato né modificato. Il testo previsto per la radice oggi sta in `Assets/README.md`.
- File e cartelle sotto `Assets/` senza `.meta` (controllo sul disco, escluse le voci nascoste e con `~` che Unity ignora): **nessuno**. Ogni file tracciato ha anche il suo `.meta` tracciato. Se in futuro la lista non è vuota, Bezi deve aggiornare l'Editor per generarli: non si scrivono a mano.

### Claude Code — 2026-10-03 — commit 645131b

**R-020/R-016** (patch `docs: patch Athena (R-016, R-020)`): tolto il vincolo che avevo aggiunto nella 0003, per cui un Nostromo bloccato ricevuto (Arrembaggio!, Reclutamento dopo un rimescolamento) poteva andare solo sopra coperta. Ora il blocco vale solo mentre il Nostromo è sopra coperta su una nave: ogni uscita dagli slot passa da `GameContext.TakeCrew` (perdita, Arrembaggio!, Commercio, sostituzione), che lo sblocca, e `CrewFlow.Receive` offre tutti gli slot. Restano invariati il Medico come unica via verso il basso, lo swap del proprietario, Spyglass! e Quartiermastro (che sposta solo tra slot sopra coperta, quindi il Nostromo resta bloccato anche sulla nave nuova). Chiude anche la mia domanda della 0004 sul Nostromo rimescolato e ripescato.

- Invariante aggiornato: oltre a "sopra coperta ⇒ bloccato, sotto ⇒ mai bloccato", nessun Nostromo fuori dalle navi (mazzo, scarti, in transito) risulta bloccato o liberato.
- Test: `AStolenLockedNostromoCanOnlyBeReceivedAbove_R016_R020` invertito in `AStolenNostromoCanBeReceivedBelowDeck_R020_R016`; nuovo `AReceivedNostromoIsLockedOnceItGoesAbove_R020_R016`. `RoundScenario.ClearSlot` sblocca la carta che toglie. `dotnet test Tools/RulesHarness` verde, 362 test.
- Verificato che mordono: blocco che resta alla carta fuori dalla nave → 6 rossi (il test del furto e l'invariante nelle simulazioni). Rimettere il vecchio filtro alla ricezione non cambia più nulla, perché la carta arriva già sbloccata.

**Gate rilanciato** (seed 1–1000 × 2/4/8, `maxRounds` 500, mappa di test, 51 s): **nessuna eccezione, nessuna violazione di invarianti**. Alcune partite cambiano percorso perché cambiano le opzioni di ricezione: interrotte 89 / 4 / 0 (prima 85 / 3 / 0).

```
Simulazione: seed 1-1000, maxRounds 500, bot casuale (seed del bot = seed della partita)

2 giocatori: 1000 partite, 911 finite con l'Isola Sacra, 89 interrotte da maxRounds, 0 errori
  round (partite finite): min 17, media 183.56, mediana 152, max 500
  interrotte da maxRounds, seed: 12, 15, 18, 25, 36, 44, 50, 52, 54, 60, 76, 106, 125, 143, 175, 191, 193, 204, 207, 241, 285, 287, 310, 323, 346, 362, 367, 380, 393, 402, 425, 434, 437, 439, 440, 442, 455, 463, 487, 491, 493, 498, 526, 553, 566, 572, 591, 596, 612, 616, 620, 636, 646, 649, 654, 655, 694, 696, 711, 716, 720, 733, 734, 751, 776, 779, 782, 784, 802, 804, 811, 839, 850, 857, 867, 879, 882, 884, 894, 900, 905, 917, 919, 921, 939, 953, 977, 982, 995
  taglia media per giocatore: 3.07 = battaglie 0.78 + missioni 1.55 + Tesoro 0.46 + monete 3.00 - missioni incomplete 2.75 + poker 0.04
  quota delle fonti positive: battaglie 13.3%, missioni 26.6%, Tesoro 7.8%, monete 51.5%, poker 0.8%
  missioni completate per partita: 1.61; vincitore: taglia media 6.92, ha preso il Tesoro nel 85.4% delle partite; parità al primo posto: 4
  poker: HighCard 96.9%, Pair 2.9%, TwoPair 0.2%, ThreeOfAKind 0.1%

4 giocatori: 1000 partite, 996 finite con l'Isola Sacra, 4 interrotte da maxRounds, 0 errori
  round (partite finite): min 9, media 110.18, mediana 85, max 476
  interrotte da maxRounds, seed: 468, 737, 826, 880
  taglia media per giocatore: 2.05 = battaglie 0.70 + missioni 0.93 + Tesoro 0.25 + monete 2.16 - missioni incomplete 2.05 + poker 0.06
  quota delle fonti positive: battaglie 17.2%, missioni 22.6%, Tesoro 6.1%, monete 52.8%, poker 1.4%
  missioni completate per partita: 1.90; vincitore: taglia media 9.09, ha preso il Tesoro nel 94.2% delle partite; parità al primo posto: 4
  poker: HighCard 95.9%, Pair 4.1%, ThreeOfAKind 0.0%

8 giocatori: 1000 partite, 1000 finite con l'Isola Sacra, 0 interrotte da maxRounds, 0 errori
  round (partite finite): min 7, media 60.65, mediana 50, max 310
  taglia media per giocatore: 1.38 = battaglie 0.79 + missioni 0.64 + Tesoro 0.13 + monete 1.48 - missioni incomplete 1.70 + poker 0.05
  quota delle fonti positive: battaglie 25.5%, missioni 20.7%, Tesoro 4.1%, monete 48.2%, poker 1.5%
  missioni completate per partita: 2.65; vincitore: taglia media 11.54, ha preso il Tesoro nel 98.3% delle partite; parità al primo posto: 1
  poker: HighCard 96.4%, Pair 3.6%, TwoPair 0.0%, ThreeOfAKind 0.0%, Straight 0.0%

Errori: nessuno
```
