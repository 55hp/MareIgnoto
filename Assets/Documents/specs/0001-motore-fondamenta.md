# 0001 — Motore: fondamenta, harness, config e mappa

Stato: pronta per Bezi
Commit di riferimento: `b33e14a`
Regole coinvolte: R-001–R-038, R-150
Documenti: `tech/01_architettura.md`, `tech/02_regole.md` §1–3, `tech/03_contenuti.md`, `tech/04_motore.md`, `tech/05_mappa.md`, `tech/08_test-e-simulazione.md`

## Obiettivo

Lo scheletro del motore esiste, compila nell'harness e in Unity, e porta una partita dal setup fino alla prima decisione della Fase 1 (scelta della rotta). Esistono le classi di config e di mappa che Bezi userà per creare gli asset.

## Contratto

- Struttura cartelle e asmdef come `tech/01_architettura.md` §2 (Rules, Unity, Editor, Rules.Tests). L'asmdef Unity può essere quasi vuoto in questo task.
- `Tools/RulesHarness/` funzionante (`dotnet test Tools/RulesHarness` verde) e `bin/`/`obj/` in `.gitignore`.
- Rules: `RulesConfig` con tutti i numeri di `02`/`03` come default; tipi di carta/missione (`03` §1–3 con gli Id indicati); `MapLayout` con `Validate()` e preset dei punti di partenza per N=2..8 (`05` §4–5); `Heading` e utilità di coordinate (`05` §1); `IRandomSource` + implementazione seedata; `GameSession.Start` / `Pending` / `Submit` / `InitialEvents` (`04`); `GameEvent` con visibilità; stato di gioco con `ViewFor`.
- Mazzi: Crew e Pirateria si rimescolano dagli scarti, Corsaro no (R-009).
- Vento iniziale col d8 (R-038), emesso come evento di dado.
- Setup completo (R-030–R-038): mazzi mescolati, navi sui punti di partenza, pescate, scelta missioni, crew iniziali sotto coperta, monete, offerta a Gartya, ordine del round 1, vento e zone iniziali. Termina con la prima `PendingDecision` di scelta rotta.
- Unity: `RulesConfigAsset` (menu `Create/MareIgnoto/Rules Config`) e `MapLayoutAsset` (menu `Create/MareIgnoto/Map Layout`) che producono gli oggetti puri. `MapLayoutAsset` ha in Inspector un pulsante o un messaggio che mostra l'esito di `Validate()` (Editor assembly).
- Il bot casuale (`04` §2, `08` §3) può già rispondere a tutte le decisioni del setup.

## Lato codice (Claude Code)

Tutto il codice del motore è in `Assets/Game/Rules/` (C# 9, nessun riferimento a Unity). Nessun `[SerializeField]` in attesa di collegamento: gli asset si creano dal menu (vedi checklist Bezi).

**Rules** (`Assets/Game/Rules/`, namespace = assembly + sottocartella)
- `Config/RulesConfig.cs`: tutti i numeri di `02`/`03` come default dei campi (giocatori, ciurma, mazzi, setup, movimento, abbordaggio, meteo, porti, combattimento, carte, fine partita, poker, `maxRounds`), le liste per carta/missione/combinazione, `Validate()`. `PokerHand.cs`.
- `Cards/`: `CardIds.cs` (enum `DeckKind`, `CrewCardId`, `PirateCardId`, `MissionId`, `CrewSuit` con gli Id di `03`), `Card.cs` (`Card` con `Uid` unico, `CrewCard`, `PirateCard`, `MissionCard`), `Catalogs.cs`, `CrewEffects.cs` (quante volte vale un effetto crew con il Jolly R-015, valori R-014).
- `Map/`: `Heading` (con `Rotate`, `Opposite`, `FromD8`), `Coord`, `CellKind`, `MapLayout` (con `spawnPresets`: un `SpawnPreset` per ogni N da 2 a 8, default = valori di `05` §4; `Validate()` su tutti i casi di `05` §5; l'esito è un `MapValidationResult` con un codice per ogni errore), `GameMap` (tipo cella, zona, isola, `SpawnPointsFor(n)`).
- `Random/`: `IRandomSource`, `SeededRandom` (PCG32), `ScriptedRandomSource` (tiri prestabiliti per il tutorial).
- `Setup/GameSetup.cs`: `GameSetup`, `PlayerSetup`, `TutorialOptions` (cime dei mazzi, vento e zone iniziali).
- `State/`: stato mutabile interno, interfacce pubbliche `IReadOnlyGameState` / `IReadOnlyPlayerState` e `PlayerView` (`ViewFor(playerId)`: mano, missioni, crew sotto coperta, rotta scelta).
- `Events/`: `GameEvent` con visibilità (`Public`/`Private`; `ViewFor(viewer)` restituisce la forma pubblica senza i dettagli segreti) e gli eventi del setup e della Fase 1.
- `Decisions/Decisions.cs`: `PendingDecision`, `DecisionOption` (con una sottoclasse per tipo), `DecisionAnswer`, `InvalidDecisionException`.
- `Engine/`: `GameSession` (`Start` / `Pending` / `Submit` / `InitialEvents` / `IsOver` / `Result` come in `04`, più `CheckInvariants()`), `Flow.cs` (vedi sotto), `GameContext`, `SetupFlow` (R-030–R-038), `RoundFlow` (rotte R-040/R-041), `TurnOrder` (R-037), `Invariants`.
- `Bots/RandomBot.cs`: sceglie un'opzione legale a caso, con una sorgente propria.

**Come è fatto (per 0002–0004)**
- Il gioco è scritto come **flussi sequenziali**: metodi iteratori che fanno `yield return ctx.Ask(...)` dove serve una scelta e `yield return Flow.Call(...)` per i sotto-flussi (`GameFlow` → `SetupFlow` / `RoundFlow`). Una regola nuova (un'azione di porto, l'effetto di una carta, il movimento) è un altro metodo di questo tipo, senza macchine a stati. `FlowRunner` li pilota fino alla prossima decisione.
- **Risposta = indice dell'opzione** (`DecisionAnswer(decisionId, optionIndex)`, costruita con `pending.Choose(...)`): ogni decisione elenca sempre tutte le sue opzioni legali, quindi validazione e bot sono uniformi. L'id della decisione scarta le risposte a decisioni già chiuse.
- Una decisione con una sola opzione legale la applica il motore; `RequiresConfirmation` (usato per la scelta rotta) la fa confermare comunque (`04` §2).
- Tutto ciò che passa dal motore usa `GameContext` (eventi, pesca con riciclo degli scarti di Crew e Pirateria R-009, dado con evento, monete): è il punto da riusare. Il Corsaro non si rimescola: `DrawToTransit` dà le missioni rimaste, e `CanDraw(DeckKind.Corsair)` dice se l'azione di porto "Missione" (spec 0003) è selezionabile.
- Le carte pescate in attesa di scelta stanno in `PlayerState.InTransit` e contano nella conservazione.
- Pronti: mazzi, ciurma, `CrewEffects`, ordine del round 1 e invarianti (conservazione delle carte, monete ≥ 0, Uid unici, ordine di turno). Gli invarianti su movimento e turni arrivano con le loro spec.

**Unity** (`Assets/Game/Unity/`, `Assets/Game/Editor/`)
- `RulesConfigAsset` (`Create/MareIgnoto/Rules Config`) e `MapLayoutAsset` (`Create/MareIgnoto/Map Layout`): `ToConfig()` / `ToLayout()` restituiscono una copia indipendente.
- `MapLayoutAssetEditor`: l'Inspector mostra l'esito di `Validate()` (box verde o rosso con i messaggi) e un pulsante "Valida".
- asmdef: `hp55games.MareIgnoto.Rules` (`noEngineReferences`), `.Unity`, `.Editor`, `.Rules.Tests` e `.Unity.Tests` (nuovo: test di conversione degli asset, in `Assets/Tests/EditMode/Unity/`).

**Harness e test**
- `Tools/RulesHarness/MareIgnoto.RulesHarness.csproj` (net8.0, `LangVersion 9.0`, NUnit 3.14): compila `Assets/Game/Rules/**` e `Assets/Tests/EditMode/Rules/**`. `bin/` e `obj/` sono ignorati; il `.csproj` dell'harness è l'unica eccezione al `*.csproj` del `.gitignore`.
- `dotnet test Tools/RulesHarness`: **124 test verdi**. Coprono: casualità e determinismo (stessi eventi a parità di seed e risposte, diversi altrimenti), config e dimensioni dei mazzi (54 / 110 / 34), Jolly e valori crew, mappa e `Validate()` su layout valido e su ognuno dei casi non validi, setup a 2, 4 e 8 giocatori, conservazione delle carte, decisioni e loro opzioni, risposte non valide (stato invariato), riciclo degli scarti R-009 (e Corsaro che non si rimescola), preset dei punti di partenza R-031, vento iniziale col d8 R-038, ordine del round 1 con pareggi di offerta e di dado (ritiro tra i pari), visibilità degli eventi e `ViewFor`, opzioni del tutorial, bot su 7 numeri di giocatori × 15 seed.
- Verificato che i test falliscono se si rompe la regola (ordine R-037 invertito: 4 test rossi).

**Non verificato**
- Il codice Unity (`Assets/Game/Unity`, `Assets/Game/Editor`, `Assets/Tests/EditMode/Unity`) l'ho compilato solo contro stub delle API Unity (non si lancia Unity da CLI): va confermato con la Console pulita e il Test Runner (EditMode).

## Lato Editor (Bezi) — checklist

- [ ] Creare `Assets/Game/Content/Config/RulesConfig.asset` dal menu `Create/MareIgnoto/Rules Config` (i default sono già giusti, nessun valore da cambiare). Controllo: in Inspector `Pirate Cards` ha 13 voci, `Missions` 17, `Poker Scores` 10 e `Starting Coins` vale 10.
- [ ] `MapLayout.asset`: si crea nella spec 0005, quando Franci approva il layout (`05_mappa.md` §6). **Non crearlo ora.** Quando esisterà, l'Inspector deve mostrare il box "Layout valido" (verde) prima di collegarlo al `GameBootstrap`.
- Nessuna scena, prefab o `[SerializeField]` da collegare in questo task.

## Per Franci — modifiche isolate

- ~~Se `dotnet` non c'è: installare .NET SDK 8.~~ Mancava: l'ho installato io con `winget install Microsoft.DotNet.SDK.8` (8.0.425, su richiesta di Franci). Aprire un terminale nuovo se `dotnet` non viene trovato.
- Tornare sull'Editor e lasciare importare: Unity genera i `.meta` dei file nuovi (una parte è già nel commit, mancano quelli degli ultimi file). Committarli insieme.
- Controllare che la Console sia senza errori e lanciare Test Runner → EditMode (`hp55games.MareIgnoto.Rules.Tests` e `hp55games.MareIgnoto.Unity.Tests`). Nei test sugli asset del progetto due casi risultano "Ignorati" finché `RulesConfig.asset` e `MapLayout.asset` non esistono: è voluto.

## Verifica

- `dotnet test Tools/RulesHarness`: verde. Test minimi: setup con 2, 4, 8 giocatori; conservazione delle carte dopo il setup; determinismo (stesso seed → stessi eventi); `MapLayout.Validate()` su layout valido e su ognuno dei casi non validi di `05` §5; ordine del round 1 con pareggi di offerta.
- Unity: il progetto compila; Test Runner EditMode verde.

## Report

### Claude Code — 2026-10-02 — commit 311887f

Il motore ha lo scheletro per l'intera `02_regole.md` e porta una partita dal setup (R-030–R-038) alla scelta e alla rivelazione delle rotte di Fase 1 (R-040, R-041). `dotnet test Tools/RulesHarness` verde (112 test). Dettaglio nella sezione "Lato codice".

**Il contratto di `04` regge senza cambiare i nomi vincolanti.** Scelte mie dentro il contratto, da sapere per 0002–0004:
- `DecisionAnswer` è `(DecisionId, OptionIndex)`: si risponde con l'indice nelle `Options`, e `PendingDecision` è una sola classe con `Kind` e opzioni tipizzate (non una classe per decisione). I tipi della tabella di `04` §2 si aggiungono come valori di `DecisionKind` e sottoclassi di `DecisionOption`.
- `PendingDecision.Cards` elenca le carte su cui si decide; `RequiresConfirmation` distingue le decisioni segrete da confermare anche con una sola opzione.
- Le decisioni segrete del setup di ogni giocatore sono consecutive (crew, missioni, offerta), così in hot-seat il dispositivo gira una volta sola. Le pescate e le monete iniziali sono per tutti, prima.
- Offerte a Gartya: le monete si tolgono e finiscono nel Tesoro alla rivelazione, non quando si decide (altrimenti il conteggio pubblico delle monete le tradirebbe).
- Pareggio dell'ordine del round 1: tira per primo il giocatore con l'id più basso, e si ritira solo tra i pari.
- La scelta rotta si chiede nell'ordine di turno del round.
- Estensioni all'API: `GameSession.CheckInvariants()`, `GameSession.Start(setup, config, map)` senza sorgente (usa `SeededRandom(setup.Seed)`), `RulesConfig.Validate()`, `RandomBot` in Rules, `MapLayout.Validate(RulesConfig)` (numero di punti di partenza e lato minimo vengono dalla config, non dal codice).

**Provvisorio fino a 0002**: dopo la rivelazione delle rotte del primo round `Pending` è `null` ma `IsOver` è `false` (il motore non ha ancora altro da fare); `Submit` allora lancia `InvalidDecisionException`. Il punto è `GameFlow.Run`, con un `TODO` che rimanda a 0002–0004.

**Per Bezi**: creare `RulesConfig.asset` (checklist). **Per Franci**: vedi "Per Franci" (import in Unity e `.meta`, Test Runner).

#### Domande

- **R-130 — missioni scartate al setup (R-034)**: sono visibili agli altri? `02` non lo dice. Le ho tenute segrete (gli altri vedono solo quante). `// TODO R-130` in `SetupFlow.ApplyKeepMissions`.
- **R-032 — crew iniziali**: "negli slot sotto coperta che preferisce". Ho dato al giocatore tutti i modi di piazzare le due carte nei tre slot (6 opzioni). Se lo slot non conta davvero (ogni carta sotto coperta è equivalente finché non c'è uno swap) si può ridurre.
- Nessun altro caso scoperto tra R-001–R-038 e R-150.

#### Note

- Il `.csproj` dell'harness non era versionato: `*.csproj` è nel `.gitignore` (per i progetti generati da Unity). Ho aggiunto l'eccezione `!/Tools/RulesHarness/*.csproj`.
- Il commit di codice è `3fd70de`; `311887f` aggiunge il `.csproj` e l'eccezione al `.gitignore`.
- Il codice Unity è verificato solo contro stub delle API, non in Unity (vedi "Non verificato").

### Claude Code — 2026-10-02 — commit b33e14a

Emendamento dopo le risposte di Franci (`00_INDICE.md`, "Registro modifiche"). `dotnet test Tools/RulesHarness` verde (124 test).

- **R-009**: il Corsaro non si rimescola. In `GameContext.DrawOne` solo Crew e Pirateria riciclano gli scarti; con meno missioni del richiesto si pescano quelle rimaste. Nuovo `GameContext.CanDraw(DeckKind)`: per il Corsaro è falso a mazzo vuoto, ed è il controllo che l'azione "Missione" (0003) userà per non comparire tra le opzioni. Gli scarti Corsaro restano nella sua pila, che vale come "fuori dal gioco" (contano per la conservazione delle carte, non tornano mai nel mazzo).
- **R-031 / `05` §4–5**: `MapLayout.spawnCells` (8 celle fisse) è sostituito da `spawnPresets`, una lista di `SpawnPreset { playerCount, cells }` (forma serializzabile da Unity, che non gestisce liste di liste). I default sono quelli di `05` §4, quindi un `MapLayout.asset` nuovo nasce con i preset già compilati. `Validate()` controlla, per ogni N da `minPlayers` a `maxPlayers`: un solo preset, esattamente N punti, tutti sulla cornice, distinti e con mare adiacente. Codici nuovi: `MissingSpawnPreset`, `DuplicateSpawnPreset`, `SpawnPresetOutOfRange`. Il setup prende il preset per il numero di giocatori: il posto k parte dal k-esimo punto (`GameMap.SpawnPointsFor(n)`, che sostituisce `SpawnPoints`).
- **R-038**: `RollD8` con il nuovo `DiceReason.InitialWind` (giocatore -1), vento = Nord ruotato di r scatti orari (8 = Nord). Le zone partono Normali. Il tutorial può ancora fissare il vento con `TutorialOptions.InitialWind`, e in quel caso il dado non si tira. In alternativa basta un tiro prestabilito in `ScriptedRandomSource`.
- **R-034/R-130**: tolto il `TODO R-130`. Le scartate restano coperte: gli altri vedono solo quante (già così, ora è la regola).
- **R-032**: invariato, 6 disposizioni.
- Test: aggiornati quelli di mappa, setup e conversione degli asset. I test di R-037 che contavano tutti i dadi ora contano solo gli spareggi, perché nel setup c'è sempre il tiro del vento.
- Le altre regole cambiate (R-021, R-023, R-042, R-050, R-056, R-073a, R-112, R-141) non sono implementate. Il modello di decisioni ed eventi le esprime senza cambiare forma: sono nuovi valori di `DecisionKind` e `DiceReason` e nuove sottoclassi di opzioni ed eventi.

#### Domande

- Nessuna sulle regole. Un dettaglio di convenzione: i preset di default sono scritti per la mappa 20×20 (coordinate 19 e 9). Se `width` cambia, vanno ricompilati a mano, e `Validate()` lo segnala.

#### Note

- I `.meta` generati da Unity per i file della spec 0001 sono ancora non tracciati nella working copy: non li ho inclusi, restano a Franci (vedi "Per Franci").
- Codice Unity: cambiato solo `AssetConversionTests` (usa solo API di Rules). Va confermato in Unity come il resto (Test Runner EditMode).
