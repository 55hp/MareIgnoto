# 01 — Architettura

## 1. Principio

Il gioco è un board game a turni con molte regole e pochi requisiti real-time. Quindi:

- **Le regole vivono in un motore C# puro**, senza `UnityEngine`, deterministico e testabile fuori da Unity.
- **Unity fa solo presentazione e input**: legge lo stato, riproduce gli eventi, raccoglie le scelte del giocatore e le passa al motore.
- **Tutorial, hot-seat, simulazioni e test usano la stessa interfaccia del motore** (`04_motore.md`): una "decisione in attesa" e una "risposta". Nessun percorso parallelo.

Il codice del 2024 (singleton, `Player : MonoBehaviour` creato con `new`, `GameManager` a coroutine, AssetBundle) **non si riusa**. Viene rimosso nella Fase 0 del piano. Dal vecchio prototipo si tengono solo gli asset grafici (vedi `_PER_FRANCI/ANALISI_REPO.md`).

## 2. Assembly e cartelle

| Assembly (asmdef) | Cartella | Riferimenti | Contenuto |
|---|---|---|---|
| `hp55games.MareIgnoto.Rules` | `Assets/Game/Rules/` | nessuno; `noEngineReferences: true` | Stato, regole, mazzi, mappa logica, motore a decisioni, eventi, sorgente casuale |
| `hp55games.MareIgnoto.Unity` | `Assets/Game/Unity/` | Rules, `Unity.TextMeshPro` | ScriptableObject di config (`RulesConfigAsset`, `MapLayoutAsset`, `TutorialScenarioAsset`), composizione (`GameBootstrap`), viste, UI, tutorial runtime, testi UI |
| `hp55games.MareIgnoto.Editor` | `Assets/Game/Editor/` | Rules, Unity; solo Editor | Validatori, inspector, menu di utilità |
| `hp55games.MareIgnoto.Rules.Tests` | `Assets/Tests/EditMode/Rules/` | Rules, NUnit (UTF) | Test EditMode delle regole |

Contenuti (asset, prefab, materiali): `Assets/Game/Content/` (Bezi). Grafica: `Assets/Art/` (Franci). Scene: `Assets/Scenes/`. Documentazione: `Assets/Documents/`.

Namespace = nome dell'assembly (`hp55games.MareIgnoto.Rules`, `...Unity`, ...), sottocartelle → sotto-namespace.

## 3. Harness .NET fuori da Unity

`Tools/RulesHarness/` (alla radice del repo, quindi ignorato da Unity) contiene un progetto .NET che compila i sorgenti di `Assets/Game/Rules/**/*.cs` e i test di `Assets/Tests/EditMode/Rules/**/*.cs` come file collegati, con NUnit. Serve a Claude Code per compilare ed eseguire i test **senza aprire Unity** (`dotnet test Tools/RulesHarness`).

Vincoli che ne derivano:
- Il codice Rules deve compilare con **C# 9** (versione di Unity 2022.3): niente file-scoped namespace, `record struct`, `global using`, `required`, raw string literal. Il progetto harness imposta `LangVersion 9.0`.
- Le API usate devono esistere in .NET Standard 2.1.
- I test Rules non usano `UnityEngine` né `UnityTest`: solo NUnit `[Test]` / `[TestCase]`.
- `bin/` e `obj/` dell'harness vanno in `.gitignore`.

Dettaglio: `08_test-e-simulazione.md`.

## 4. Configurazione: una sola fonte per ogni numero

- `RulesConfig` (classe C# pura, Rules): tutti i numeri del gioco (monete iniziali, carte pescate, copie per carta, costi, ricompense, `maxRounds`...). I valori di default sono gli inizializzatori dei campi e coincidono con `02_regole.md` e `03_contenuti.md`.
- `RulesConfigAsset` (ScriptableObject, Unity): espone gli stessi campi per il bilanciamento da Inspector e produce un `RulesConfig`. Un asset creato dal menu `Create/MareIgnoto/Rules Config` nasce già con i default giusti, quindi crearlo è un passo da un clic.
- Nessun numero di regola scritto altrove (né nel codice degli effetti, né nella UI, né nei test: i test leggono da `RulesConfig`).
- `MapLayoutAsset` → `MapLayout` con lo stesso schema (`05_mappa.md`).

## 5. Flusso a runtime

```
GameBootstrap (MonoBehaviour, unico composition root in scena)
  ├─ legge RulesConfigAsset + MapLayoutAsset (+ TutorialScenarioAsset se tutorial)
  ├─ crea GameSession (Rules) con setup e seed
  └─ passa la sessione a GameFlowController (Unity)

GameFlowController
  loop:
    1. prende gli eventi prodotti dall'ultimo Submit e li fa riprodurre alle viste (EventPlayer, in sequenza, con animazioni brevi)
    2. legge session.Pending (la decisione in attesa: chi, cosa, opzioni legali)
    3. se serve, mostra la schermata "passa il dispositivo a X" (hot-seat, 06_presentazione.md)
    4. apre il pannello giusto per quel tipo di decisione
    5. il pannello costruisce la risposta → session.Submit(risposta)
```

Regole:
- Le viste **non modificano mai lo stato**. Leggono `GameState` (in sola lettura) ed eventi.
- Niente singleton, niente stato statico mutabile. I riferimenti passano da `[SerializeField]` o dal bootstrap.
- Gameplay ↔ UI: solo tramite decisioni ed eventi del motore. Nessuna vista chiama un'altra vista per logica di gioco.

## 6. Tutorial e bot

- **Tutorial**: una partita a 2 con seed fisso, mazzi in ordine prestabilito e mappa dedicata o standard. L'avversario risponde con una sequenza di risposte predefinita. Un `TutorialDirector` (Unity) osserva decisioni ed eventi, mostra i passi e, quando un passo lo richiede, **filtra le opzioni legali** della decisione in attesa per guidare il giocatore. Il motore non sa che è un tutorial. Dettaglio: `07_tutorial.md`.
- **Bot casuale**: un agente che sceglie un'opzione legale a caso. Nasce per la simulazione (`08_test-e-simulazione.md`) ed è riusabile in partita per riempire i posti vuoti durante i playtest solitari.

## 7. Fuori scope del prototipo

Rete/online, salvataggio a metà partita, annulla mossa, localizzazione in altre lingue, audio, build mobile. L'interfaccia a decisioni del motore rende possibili rete e annulla in futuro senza riscrivere le regole.
