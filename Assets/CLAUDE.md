# CLAUDE.md

MareIgnoto — prototipo digitale del gioco da tavolo "Pirates of the 7 Orbs" (carte e navigazione a movimento simultaneo, 2–8 giocatori in hot-seat), Unity 2022.3.62f3 + URP, PC.

Documentazione di progetto: `Assets/Documents/README.md` ("il README"). Regole del gioco: `Assets/Documents/tech/02_regole.md` e `03_contenuti.md`. Contratti tecnici: `Assets/Documents/tech/` (indice in `00_INDICE.md`). **Fonte di verità: il repo.** Notion non fa parte del tuo flusso: non leggerlo, non citarlo.

Questo file contiene solo ciò che cambia *come* lavori. Per *com'è fatto* il progetto leggi il README e `tech/`: non duplicarli qui.

## Comandi

- **Test delle regole fuori da Unity**: `dotnet test Tools/RulesHarness`. Eseguilo tu, sempre, prima di dichiarare finito un task che tocca `Assets/Game/Rules/` o i suoi test. Compilazione verde e test verdi sono responsabilità tua, non di Franci.
- **Aprire**: Unity Hub, Editor `2022.3.62f3` esatto (deve combaciare con `ProjectSettings/ProjectVersion.txt`).
- **Test in Unity**: Test Runner → EditMode (`Assets/Tests/EditMode/`). Li lancia Franci; tu indica quali.
- `.sln`/`.csproj` alla radice sono generati da Unity: non modificarli. L'unico progetto .NET che possiedi è `Tools/RulesHarness/`.

## L'Editor di Franci è quasi sempre aperto

- **Mai lanciare Unity da CLI** (`-batchmode`, `-executeMethod`): conflitto con l'istanza aperta (lock di `Library`, GUID duplicati).
- **Un `.cs` nuovo non ha `.meta` finché Franci non torna sull'Editor.** Non scrivere a mano `.meta` né `.asset`/`.prefab`/`.unity` YAML. Creazione di asset e collegamenti sono passi Editor (Bezi o Franci).

## Ownership — chi tocca cosa

| Attore | Possiede | Non tocca |
|---|---|---|
| **Claude Code** (tu) | C#, test, asmdef, `Tools/RulesHarness/`, sezione Stato del README, parte "Lato codice" delle spec, questo file | Scene, prefab, `.asset`, materiali, report di Bezi, `tech/02_regole.md`, `tech/03_contenuti.md` |
| **Bezi** | Scene, prefab, Inspector, creazione e valori degli asset, layout UI | C# |
| **Franci** | Design, priorità, commit e push finali, playtest, modifiche isolate nell'Editor, bilanciamento | — |
| **Athena** | `tech/`, intestazione delle spec, prompt | Codice e asset |

**Il confine codice/scena è di Bezi.** Se uno script ha bisogno di un oggetto di scena, di un prefab o di un asset, dichiari un `[SerializeField]` e ti fermi. Il setup Editor mancante si scrive nella spec (checklist Bezi o "Per Franci"), non si compensa da codice.

Nel report distingui:
- **per Bezi**: wiring, gerarchie di scena, prefab, creazione e assegnazione di asset che fanno parte di un lavoro Editor vero;
- **per Franci**: una singola modifica isolata (un valore, un'assegnazione, una rimozione). Bezi per regola li rimanda a Franci.

### Spec e report

- Spec: `Assets/Documents/specs/NNNN-slug.md`, una per task. Athena ne scrive intestazione, obiettivo e contratto; tu compili "Lato codice", la checklist Bezi e la sezione "Per Franci" con lo stato reale, e il commit di riferimento.
- Report: in coda, sotto `## Report`, voci append-only. La tua intestazione: `### Claude Code — AAAA-MM-GG — commit abc1234`. Non modifichi mai le voci di Bezi o di Franci.
- **Domande sulle regole**: se `02`/`03` non coprono un caso, non inventare. Implementa il resto, lascia un `// TODO R-xxx: <domanda>` nel punto esatto, e scrivi la domanda nel report sotto `#### Domande`. Le voci **[DEFAULT]** in `tech/` sono decisioni valide: implementale come scritte.
- Bezi non vede il lavoro non committato né la radice del repo. Se un suo report contraddice il disco, il report è vecchio: controlla il commit che dichiara prima di agire.

## Regole che cambiano cosa scrivi

1. **Assembly Rules puro**: `hp55games.MareIgnoto.Rules` ha `noEngineReferences: true`. Niente `UnityEngine`, niente `System.Random`, niente `DateTime.Now`, niente I/O. Tutta la casualità passa da `IRandomSource`. Deterministico: stesso seed + stesse risposte = stessi eventi.
2. **C# 9 al massimo** in tutto il progetto (versione di Unity 2022.3): niente file-scoped namespace, `global using`, `record struct`, `required`, raw string literal, `init` è ammesso. API compatibili con .NET Standard 2.1.
3. **La presentazione non modifica lo stato.** Legge `IReadOnlyGameState` (con `ViewFor(player)` per le informazioni segrete) e gli eventi; agisce solo con `GameSession.Submit`.
4. **Ogni decisione espone le sue opzioni legali** (servono a UI, tutorial e bot casuale).
5. **Numeri di regola solo in `RulesConfig`.** Nessuna costante di gioco nel codice degli effetti, nella UI o nei test (i test leggono da `RulesConfig`).
6. **Vietato collegare a runtime oggetti autorabili**: niente `AddComponent`, `FindObjectOfType`, `GameObject.Find`, `transform.Find`, `Camera.main` per trovare componenti. Eccezione: oggetti generati per natura dai dati (celle della mappa, navi, carte in mano), istanziati da prefab assegnati in Inspector.
7. **Riferimento nullo**: `Debug.LogError` con cosa manca e dove, poi il componente si disabilita. Mai ricostruire da codice layout o valori che stanno nel prefab.
8. **Una sola fonte di verità per ogni valore**: nel prefab/scena **oppure** in un asset di config, mai in entrambi. Il codice non sovrascrive valori autorati.
9. **Niente singleton né stato statico mutabile.** Composition root unico: `GameBootstrap`.
10. **Testi per il giocatore**: italiano, tutti in `UiText` (assembly Unity). Mai testo hardcoded nei componenti o nei prefab. Sempre TextMeshPro.
11. **Input**: solo Legacy Input Manager (`activeInputHandler: 0`), `StandaloneInputModule` sull'EventSystem.
12. **Commit locali; push solo su conferma di Franci.** Se il task coinvolge anche Bezi, si pusha quando sono pronte entrambe le parti. Committa solo i file del tuo task (`git add` per percorso, mai `git add -A`): Franci e Bezi lavorano sulla stessa working copy.
13. **Nessun riferimento a file che non esistono nel repo.**
14. **Codice del 2024** (`Assets/Scripts/`, `Assets/Cards/*.cs`): non usarlo, non estenderlo, non correggerlo. Viene rimosso da Franci nella Fase 0. Se finché esiste genera errori che ti bloccano, segnalalo.
15. Namespace = nome dell'assembly (`hp55games.MareIgnoto.Rules`, `hp55games.MareIgnoto.Unity`, ...).

## Prompt e dimensione dei task

I prompt di Franci ti danno obiettivo, vincoli e riferimenti: le scelte di implementazione interne sono tue. Puoi prendere una feature intera in un prompt. Se a metà task scopri che serve lavoro Editor, non fermarti: completa il codice, esponi i `[SerializeField]`, e scrivi la checklist per Bezi nella spec.

## Trappole note

- **Mappa**: sul repo la griglia del 2024 è 49×49 (`GRID_CONTROLLER.numberOfNodes` in `MainScene`). La mappa vera è 20×20 e arriva da `MapLayout.asset` (`tech/05_mappa.md`). Non leggere niente dalla vecchia scena.
- **`2players.asset`** ha il campo `player_amount`, che non esiste più nello script: asset del 2024, da ignorare.
- **Build Settings vuote**: le scene vanno aggiunte da Franci/Bezi quando esistono.

## Lingua

Documentazione e commenti in italiano; identificatori del codice in inglese. Adegua il file che modifichi.
