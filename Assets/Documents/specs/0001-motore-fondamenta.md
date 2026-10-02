# 0001 — Motore: fondamenta, harness, config e mappa

Stato: bozza
Commit di riferimento: da compilare
Regole coinvolte: R-001–R-038, R-150
Documenti: `tech/01_architettura.md`, `tech/02_regole.md` §1–3, `tech/03_contenuti.md`, `tech/04_motore.md`, `tech/05_mappa.md`, `tech/08_test-e-simulazione.md`

## Obiettivo

Lo scheletro del motore esiste, compila nell'harness e in Unity, e porta una partita dal setup fino alla prima decisione della Fase 1 (scelta della rotta). Esistono le classi di config e di mappa che Bezi userà per creare gli asset.

## Contratto

- Struttura cartelle e asmdef come `tech/01_architettura.md` §2 (Rules, Unity, Editor, Rules.Tests). L'asmdef Unity può essere quasi vuoto in questo task.
- `Tools/RulesHarness/` funzionante (`dotnet test Tools/RulesHarness` verde) e `bin/`/`obj/` in `.gitignore`.
- Rules: `RulesConfig` con tutti i numeri di `02`/`03` come default; tipi di carta/missione (`03` §1–3 con gli Id indicati); `MapLayout` con `Validate()`; `Heading` e utilità di coordinate (`05` §1); `IRandomSource` + implementazione seedata; `GameSession.Start` / `Pending` / `Submit` / `InitialEvents` (`04`); `GameEvent` con visibilità; stato di gioco con `ViewFor`.
- Setup completo (R-030–R-038): mazzi mescolati, navi sui punti di partenza, pescate, scelta missioni, crew iniziali sotto coperta, monete, offerta a Gartya, ordine del round 1, vento e zone iniziali. Termina con la prima `PendingDecision` di scelta rotta.
- Unity: `RulesConfigAsset` (menu `Create/MareIgnoto/Rules Config`) e `MapLayoutAsset` (menu `Create/MareIgnoto/Map Layout`) che producono gli oggetti puri. `MapLayoutAsset` ha in Inspector un pulsante o un messaggio che mostra l'esito di `Validate()` (Editor assembly).
- Il bot casuale (`04` §2, `08` §3) può già rispondere a tutte le decisioni del setup.

## Lato codice (Claude Code)

_da compilare_

## Lato Editor (Bezi) — checklist

- [ ] Creare `Assets/Game/Content/Config/RulesConfig.asset` dal menu `Create/MareIgnoto/Rules Config` (i default sono già giusti, nessun valore da cambiare).
- [ ] `MapLayout.asset`: si crea nella spec 0005, dopo l'audit 0000.

## Per Franci — modifiche isolate

- Se `dotnet` non c'è: installare .NET SDK 8.

## Verifica

- `dotnet test Tools/RulesHarness`: verde. Test minimi: setup con 2, 4, 8 giocatori; conservazione delle carte dopo il setup; determinismo (stesso seed → stessi eventi); `MapLayout.Validate()` su layout valido e su ognuno dei casi non validi di `05` §5; ordine del round 1 con pareggi di offerta.
- Unity: il progetto compila; Test Runner EditMode verde.

## Report
