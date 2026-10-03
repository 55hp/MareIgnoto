# 0003 — Fase 2: turno in mare, porti, carte Pirateria, combattimento

Stato: bozza
Commit di riferimento: `299e983`
Regole coinvolte: R-010–R-023, R-050–R-058, R-090–R-123
Documenti: `tech/02_regole.md` §2, §4, §7; `tech/03_contenuti.md` §1–2

## Obiettivo

La Fase 2 è completa: turno in mare (pesca 2 oppure swap/attacco/carta), turno in porto (5 azioni), Vedetta, tutte le carte Pirateria (battaglia, economia, Meteo), il combattimento completo (duello Bordata!/Parlè!, Cannoniere, Falconet, Saker, Culverin, Nostromo, Arrembaggio!, Quartiermastro) e tutti gli effetti crew sopra coperta, Jolly compreso.

## Contratto

- Ogni decisione del combattimento ha proprietario e segretezza corretti (`04` §2): il difensore risponde con il dispositivo in mano.
- Eventi di combattimento sufficienti a mostrare il botta e risposta carta per carta.

## Lato codice (Claude Code)

Solo assembly Rules e test: nessun lavoro Editor, nessun `[SerializeField]`.

**Flussi** (`Assets/Game/Rules/Engine/`)
- `RoundFlow.Active` → `TurnFlow.Play` dopo cornice (R-053) e Mozzo (R-052): Isola Sacra senza azioni (R-055), porto se la nave è su un'isola o in Svago (R-054, R-091), Vedetta che sceglie porto o mare (R-058, decisione `LookoutChoice`), altrimenti `SeaTurnFlow`.
- `TurnFlow.Port`: le 5 azioni (R-090–R-094) con il costo effettivo del Cuoco (R-095) e solo se pagabili (R-096). Missione solo col Corsaro non vuoto (R-009). I costi di porto escono dal gioco: il Tesoro riceve solo offerte e carte Meteo (R-140). Reclutamento → `RecruitKeep`, poi `CrewFlow.Receive` per ogni carta tenuta.
- `SeaTurnFlow.Run` (R-056): un ciclo di decisioni `SeaAction` con le azioni ancora disponibili. Pescare è possibile solo come prima azione; swap una volta (R-057); attacco **o** Quartiermastro una volta; carte del turno `1 + Bucaniere` (Jolly compreso); "fine turno" sempre. `PlayCard` gestisce ogni carta di `03` §2: si scarta, paga il costo Meteo al Tesoro (R-121), Cuoco sulle carte pesca (R-122), bersagli e parametri con la decisione `CardTarget` (pubblica).
- `CombatFlow`: gittata e bersagli (R-100, R-101), opzioni d'attacco (R-102, R-105, R-106, R-112), botta e risposta (R-103) con Bordate gratuite del Cannoniere per combattimento e per contendente, Falconet (R-106), Culverin (R-108), vittoria (R-104) con Saker (R-107) e Nostromo (R-109), Arrembaggio! (R-110) e Quartiermastro (R-111).
- `CrewFlow`: `Receive` (R-020, un Nostromo bloccato solo sopra) e `KeepMissions`, condiviso tra setup (R-034) e porto (R-093).
- `GameContext`: `PutCrew` (ogni carta che entra in uno slot; blocca il Nostromo che sale, R-016), `CanGoBelow`/`CanExchange`/`Exchange`, `ChangeBounty`. `Losses.LoseCrew` accetta una destinazione, così Arrembaggio! passa la carta all'attaccante con il Medico (R-023) già gestito.

**Decisioni nuove**: `LookoutChoice`, `PortAction`, `RecruitKeep`, `ReceiveCrew`, `SeaAction`, `ManOverboardDiscard`, `CombatResponse`, `BoardingDefense` (segrete: le prende chi ha il dispositivo in mano, il difensore nel combattimento); `CardTarget`, `StealCrew`, `QuartermasterSwap` (pubbliche). Le opzioni sono in `Decisions/TurnDecisions.cs`; le carte uguali in mano danno una sola opzione.

**Eventi nuovi** (`Events/TurnEvents.cs`): `TurnKind`, `PortAction`, `CrewSold`, `CrewReceived`, `CrewSwapped`, `CardPlayed` (con `CountsAsTurnCard`), `Spyglass`, `QuartermasterSwap`. Per il combattimento, carta per carta: `AttackStarted` (apertura, distanza, Parlè! vietata), `CombatPlay`, `CombatYielded`, `BattleWon`, `BountyChanged`, `CardStolen` (privato al ladro), `BoardingDefended`. `TurnSkippedEvent` ha il motivo (cornice o Isola Sacra). Nuove cause di perdita: `ManOverboard`, `Arrembaggio`, `Replaced`.

**Invariante nuovo**: un Nostromo che è stato sopra coperta non è mai sotto (R-016, `GameState.LockedNostromi`).

**Test** (`dotnet test Tools/RulesHarness`: **280 verdi**, più 1 `[Explicit]`)
- `CombatTests`, `PirateCardTests`, `PortAndTurnTests`: ogni carta di `03` §1–2 e ogni effetto crew ha almeno un test, anche col Jolly (Bucaniere, Vedetta, Cuoco su porto e carte pesca, Cannoniere, Nostromo, Quartiermastro, Falconet, Saker, Culverin; Mozzo, Navigatore e Timoniere erano già nella 0002). Ci sono i casi di `08` §2: botta e risposta fino all'esaurimento, Cannoniere, Falconet, Culverin a distanza 2, Saker, Nostromo, Arrembaggio! pagato/parato/riuscito, Medico.
- Ogni regola R-010–R-023, R-050–R-058, R-090–R-096, R-100–R-112, R-120–R-123 è citata da almeno un test.
- `RoundScenario.PlayTurns` gioca un round con le navi ferme (rotte a Sud col vento da Nord), così si prova la sola Fase 2. `Pick`/`Opt` scelgono le risposte per tipo di opzione.
- Simulazione: `Simulation60RoundsWithFullPhase2` (60 round × 2/4/8 × 10 seed, invarianti dopo ogni risposta, e verifica che i bot usino porti, carte e attacchi). La simulazione da 200 round della 0002 ora gira con la Fase 2 completa. Il gate di `08` §3 (`Simulation1000Seeds`, seed 1–1000 × 2/4/8, 200 round, ora con la Fase 2 completa) l'ho lanciato: verde in 1 min 33 s.
- Verificato che i test mordono: Parlè! permessa contro il Culverin da lontano (1 rosso), apertura con la Bordata! della mano che non consuma la carta del turno (1 rosso), Cuoco senza sconto (3 rossi), Saker senza raddoppio (2 rossi).
- I test di scenario della 0002 ora chiudono il turno in mare per default (`RoundScenario.Default`), così la Fase 2 non tocca mani e ciurme.

## Per Franci — modifiche isolate

- Tornare sull'Editor per i `.meta` dei file nuovi (6 in `Assets/Game/Rules/`, 3 in `Assets/Tests/EditMode/Rules/`) e committarli.
- Test Runner → EditMode, `hp55games.MareIgnoto.Rules.Tests`: deve essere verde come l'harness.

## Verifica

- `dotnet test Tools/RulesHarness` verde, con test per ogni carta e ogni effetto crew, più i casi di `08` §2 (combattimento, Medico, Jolly).
- Simulazione: partite di 60 round × 2/4/8 giocatori senza eccezioni né violazioni.

## Report

### Claude Code — 2026-10-02 — commit 299e983

La Fase 2 è completa: porti, Vedetta, turno in mare, tutte le carte Pirateria, combattimento con tutti gli effetti crew e il Jolly. `dotnet test Tools/RulesHarness` verde (280 test); simulazione 60 round × 2/4/8 verde. Dettaglio in "Lato codice".

Da sapere per la 0004:
- Gli eventi per le missioni ci sono: `BattleWonEvent` (con l'attaccante), `PortActionEvent` (Svago per "Spugna di mare"), `CrewLostEvent` con `AtSea` e la causa, `ShipStrandedEvent`, `BountyChangedEvent` (per ora solo `Battle`), `CardPlayedEvent`.
- La simulazione ha trovato un errore che i test di scenario non coprivano: un Nostromo bloccato, rubato con Arrembaggio!, poteva essere ricevuto sotto coperta. Corretto in `CrewFlow.Receive` (R-020 richiama R-016) e coperto da un test.

#### Domande

- **R-110 — a chi vanno le 5 monete** pagate per evitare Arrembaggio!: `02` non lo dice. Le riceve l'attaccante (`// TODO R-110` in `CombatFlow.Boarding`). Alternative: il Tesoro, oppure escono dal gioco.
- **R-107 — Saker col Jolly**: ho applicato il raddoppio una volta per copia, quindi Saker + Jolly fa ×4 (4 segnalini e 4×1d8 monete). L'altra lettura è ×3 (+1 raddoppio per copia).
- **R-111 — Quartiermastro**: scartare la Bordata! **non** consuma la carta del turno (non è un'apertura d'attacco, R-112). Il bersaglio deve essere una nave in mare a tiro, come per l'attacco, ed entrambi gli slot scambiati devono contenere una carta.
- **R-106 — Falconet e Arrembaggio!**: col Falconet ogni attacco di Bordate è il Duello col primo colpo gratuito (non c'è l'apertura con la Bordata! della mano); Arrembaggio! resta un'apertura normale, perché non è uno scontro di Bordate. Il Cannoniere del difensore vale anche nel Duello del Falconet, l'unico caso in cui il difensore spara.
- **R-058 — Vedetta col Jolly**: la distanza dal porto cresce di `lookoutRange` per copia (Jolly + Vedetta = 2 celle). Se con la Vedetta si sceglie Svago, la nave resta sulla sua cella di mare: nel round dopo non si muove e gioca di nuovo un turno di porto, ma resta attaccabile perché è in mare. Va bene?
- **R-120 — Uomo in mare!**: il bersaglio sceglie una carta qualsiasi (sopra o sotto coperta). Se sceglie una carta sopra coperta può usare il Medico (R-023). Una nave senza crew non è un bersaglio valido.
- **Spyglass!**: servono due slot occupati ("due carte crew"); le coppie che porterebbero sotto un Nostromo bloccato non si possono scegliere.
- **Costi di porto**: Svago e Reclutamento tolgono monete dal gioco, non le danno al Tesoro (R-140 elenca solo offerte e carte Meteo). Va bene?
- **R-052 sull'Isola Sacra**: il Mozzo paga anche lì, perché R-052 esclude solo la cornice. Dalla 0004 la partita finisce prima.
- Restano aperte le domande della 0002 (report della spec 0002).

#### Note

- Le carte Meteo di zona possono bersagliare anche una zona che non cambia (Favore di Gartya su una zona Normale): è legale e costa comunque.
- Nella simulazione i bot fanno tutto (attacchi di ogni tipo, Arrembaggio!, Spyglass!, Medico, Nostromo, tutte le azioni di porto). Il Quartiermastro è raro (serve con una Bordata! in mano e un bersaglio a tiro con carte sopra coperta): è coperto dai test di scenario.

### Claude Code — 2026-10-03 — commit 0965837

#### Letture confermate

Franci ha confermato queste letture delle domande qui sopra; il codice resta com'era (tolto solo il `// TODO R-110`):
- **R-110**: le 5 monete per evitare Arrembaggio! vanno all'attaccante.
- **R-107**: Saker + Jolly = ×4.
- **R-111**: scartare la Bordata! per il Quartiermastro non consuma la carta del turno.
- **R-106**: il Falconet vale solo per gli attacchi aperti con Bordata!; Arrembaggio! resta un'apertura normale.
- **R-058**: Vedetta + Jolly raggiunge 2 celle; Svago scelto con la Vedetta lascia la nave in mare, quindi attaccabile.
- **Costi di porto**: Svago e Reclutamento tolgono monete dal gioco, non vanno al Tesoro.
- **Uomo in mare!, Spyglass!, Mozzo sull'Isola Sacra**: come implementati.

Nello stesso commit, con la spec 0004, ho corretto il Medico (R-023) e il blocco del Nostromo (R-016): dettaglio nel report della spec 0004.
