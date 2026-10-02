# MareIgnoto — Documento di progetto

Prototipo digitale Unity del gioco da tavolo "Pirates of the 7 Orbs": carte e navigazione a movimento programmato simultaneo, gestione della mano, combattimento stile *Bang!*, 2–8 giocatori in hot-seat. Vince chi ha la taglia più alta a fine partita. Il prototipo include un tutorial.

- **Repo**: `55hp/MareIgnoto`. Si lavora su `develop`; `main` riceve solo milestone chiuse.
- **Fonte di verità**: il repo. Questo file descrive lo **stato** del progetto (vale il codice se divergono, e questo file si corregge nello stesso commit). Le **regole del gioco** stanno in `tech/02_regole.md` e `tech/03_contenuti.md` (vale il documento se divergono: vedi `tech/00_INDICE.md`).
- **Notion** è l'archivio di design di Franci: gli agenti non lo leggono né lo scrivono.
- **Posizione**: questa cartella sta in `Assets/` perché Bezi vede solo `Assets/`, `Packages/` e `ProjectSettings/`. Alla radice del repo ci sono solo `README.md` (rimando per GitHub) e `CLAUDE.md` (istruzioni per Claude Code, che Bezi non può leggere).
- **Regole di Bezi**: `Bezi_Rules.md`, in questa cartella.

## 1. Struttura della documentazione

| Percorso | Contenuto |
|---|---|
| `README.md` | Questo file: stato del progetto |
| `Bezi_Rules.md` | Regole per Bezi |
| `tech/` | Documentazione tecnica per funzionalità (indice: `tech/00_INDICE.md`) |
| `specs/NNNN-slug.md` | Una spec per task, dal modello `specs/_TEMPLATE.md`. Report degli agenti in coda, sotto `## Report` |

## 2. Architettura in breve

Motore delle regole in C# puro (`Assets/Game/Rules/`, nessun riferimento a Unity, testabile con `dotnet test Tools/RulesHarness`) + strato Unity per presentazione e input (`Assets/Game/Unity/`). Il motore espone una decisione in attesa e accetta risposte; UI, tutorial, bot e test usano la stessa interfaccia. Dettaglio: `tech/01_architettura.md`, `tech/04_motore.md`.

## 3. Ownership

| Attore | Possiede | Non tocca |
|---|---|---|
| **Claude Code** | Script C#, test, asmdef, harness .NET, questo README (sezioni di stato), la parte "Lato codice" delle spec | Scene, prefab, `.asset`, i report di Bezi, le regole in `tech/02` e `tech/03` |
| **Bezi** | Scene, prefab, riferimenti Inspector, creazione e valori degli asset, layout UI | Logica C#, salvo richiesta esplicita |
| **Franci** | Decisioni di design, priorità, commit/push, playtest, modifiche isolate nell'Editor (segnate `[///MANUAL_CHANGES]` nella spec), bilanciamento in `RulesConfig.asset` | — |
| **Athena** (chat) | `tech/` (contratti di design e tecnici), intestazione e obiettivi delle spec, prompt per gli agenti | Codice, scene, asset |

## 4. Stato

> Sezione mantenuta da Claude Code. Aggiornarla a ogni task chiuso.

- **02/10/2026** — Documentazione e piano di lavoro aggiunti. Il codice presente è il prototipo del 2024 (`Assets/Scripts/`, `Assets/Cards/*.cs`, singleton, griglia 49×49): **non si riusa** e verrà rimosso nella Fase 0. Nessun codice del nuovo motore ancora scritto.

## 5. Comandi

- Unity Hub, Editor `2022.3.62f3` esatto.
- Play: `Assets/Scenes/Board.unity` (avvio diretto con configurazione di test) o `Assets/Scenes/Menu.unity` — quando esisteranno.
- Test regole fuori da Unity: `dotnet test Tools/RulesHarness`.
- Test in Unity: Window → General → Test Runner → EditMode.

## 6. Aperti

- Mappa 20×20 con isole: secondo Franci esiste nel prototipo, ma sul repo non c'è (vedi `tech/05_mappa.md` §6). Da recuperare nella spec 0000.
- Le scelte marcate **[DEFAULT]** in `tech/` attendono conferma di Franci.
