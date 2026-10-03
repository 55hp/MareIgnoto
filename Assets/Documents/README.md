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

- **03/10/2026** — **Emendamento 0002/0004** (registro di `tech/00` del 02/10 (4)): le missioni di stato si completano solo quando la condizione diventa vera dopo la pesca (R-131); un Abbordaggio che riposiziona una nave sull'Isola Sacra le fa prendere il Tesoro e chiude la partita, dopo ogni arrivo in movimento (R-073a, R-140, R-141); le carte Meteo del round di cortesia si pagano ma le monete escono dal gioco (R-142); il Jolly accanto al Nostromo è un secondo Nostromo, e dimezza solo se fa parte della combinazione (R-146). `dotnet test Tools/RulesHarness`: 360 test verdi. Gate rilanciato: 1000 seed × 2/4/8 senza eccezioni né violazioni (riepilogo nel report della spec 0004).

- **03/10/2026** — **Spec 0004 (missioni, fine partita, punteggio, simulazione) chiusa lato codice: le partite finiscono.** Missioni Corsaro con completamento immediato (Olandese Volante a fine partita), Isola Sacra con Tesoro e round di cortesia, punteggio con poker, Jolly e Nostromo, `GameResult` con classifica, vincitori e dettaglio della taglia per giocatore, `SimulationRunner` con report testuale. Corretti prima Medico (vale anche per il Nostromo, solo per le perdite sopra coperta) e blocco del Nostromo (unica via verso il basso: il Medico). `dotnet test Tools/RulesHarness`: 354 test verdi. **Gate di Fase 1 verde**: 1000 seed × 2/4/8 giocatori senza eccezioni né violazioni di invarianti (`Simulation1000Seeds`, `[Explicit]`; 88 partite su 3000 fermate da `maxRounds` 500, quasi tutte a 2 giocatori, elencate per seed nel report della spec 0004). Le statistiche sono sulla mappa di test, perché `MapLayout.asset` non esiste ancora. Domande sulle regole nel report della 0004. Da verificare in Unity (Test Runner EditMode) dopo l'import dei `.meta`.

- **02/10/2026** — **Spec 0003 (Fase 2: porti, carte, combattimento) chiusa lato codice.** Il turno di Fase 2 è completo: porti con le 5 azioni e il Cuoco, Vedetta, turno in mare (pesca, oppure swap/attacco/carte con il Bucaniere), tutte le carte Pirateria di `tech/03_contenuti.md` §2, combattimento con botta e risposta, Cannoniere, Falconet, Saker, Culverin, Nostromo, Arrembaggio!, Quartiermastro, Medico e Jolly. Manca la 0004: missioni, fine partita con l'Isola Sacra, punteggio; finché non c'è, la partita finisce solo per `maxRounds`. `dotnet test Tools/RulesHarness`: 280 test verdi; simulazione con bot 60 round × 2/4/8 verde. Domande sulle regole nei report delle spec 0002 e 0003. Da verificare in Unity (Test Runner EditMode) dopo l'import dei `.meta`.

- **02/10/2026** — **Spec 0002 (round, meteo, movimento, Abbordaggio) chiusa lato codice.** Il motore gioca round completi: rotte segrete, meteo per zona (Navigatore, Timoniere, Vento in Poppa), movimento simultaneo cella per cella con collisioni e attraversamenti, Abbordaggi fortuiti a 2 e 3+ navi con catene e Medico, ordine di turno dal round 2 (R-050/R-051). In Fase 2 c'è solo l'ordine, la cornice (R-053) e il Mozzo (R-052): poi ognuno passa. Le azioni del turno arrivano con la 0003. La partita finisce solo per `maxRounds` (R-150) finché la 0004 non porta l'Isola Sacra. `dotnet test Tools/RulesHarness`: 190 test verdi; simulazione con bot 200 round × 2/4/8 verde, anche sui 1000 seed di `tech/08` §3. Domande aperte sulle regole nel report della spec 0002. Da verificare in Unity (Test Runner EditMode) dopo l'import dei `.meta`.

- **02/10/2026** — **Spec 0001 emendata** con le risposte di Franci: il mazzo Corsaro non si rimescola (R-009), i punti di partenza sono preset per N=2..8 in `MapLayout` con i valori di `tech/05_mappa.md` §4 (R-031), il vento iniziale si tira col d8 (R-038), le missioni scartate restano coperte (R-130). `dotnet test Tools/RulesHarness`: 124 test verdi. Il resto dello stato è invariato (voce sotto). `MapLayout.asset` resta bloccato finché il layout delle isole non è deciso.

- **02/10/2026** — **Spec 0001 (motore: fondamenta) chiusa lato codice**, in attesa di Bezi (`RulesConfig.asset`) e della verifica di Franci in Unity. Esistono: assembly `Rules` (config, carte, mappa, casualità seedata, decisioni, eventi con visibilità, `GameSession`), il setup completo R-030–R-038 e la scelta/rivelazione delle rotte di Fase 1, `RulesConfigAsset`/`MapLayoutAsset` con inspector di validazione, bot casuale e `Tools/RulesHarness` (`dotnet test Tools/RulesHarness`: 112 test verdi; il .NET SDK 8 è installato sul PC di Franci). Provvisorio: dopo la rivelazione delle rotte del round 1 il motore si ferma (`Pending` nullo senza partita finita) fino alla spec 0002. Il codice Unity non è ancora stato compilato in Unity (solo contro stub). Ancora da fare: 0002 (movimento e meteo), 0003 (Fase 2), 0004 (missioni e fine partita), poi le scene.
- **02/10/2026** — Documentazione e piano di lavoro aggiunti. Il codice presente è il prototipo del 2024 (`Assets/Scripts/`, `Assets/Cards/*.cs`, singleton, griglia 49×49): **non si riusa** e verrà rimosso nella Fase 0.

## 5. Comandi

- Unity Hub, Editor `2022.3.62f3` esatto.
- Play: `Assets/Scenes/Board.unity` (avvio diretto con configurazione di test) o `Assets/Scenes/Menu.unity` — quando esisteranno.
- Test regole fuori da Unity: `dotnet test Tools/RulesHarness`.
- Test in Unity: Window → General → Test Runner → EditMode.

## 6. Aperti

- Mappa 25×25: layout v4 approvato il 03/10/2026 (`tech/05_mappa.md` §6). Il motore va ancora allineato; `MapLayout.asset` si crea nella spec 0005, passo A.
- Le scelte marcate **[DEFAULT]** in `tech/` attendono conferma di Franci.
