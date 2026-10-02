# 0002 — Round, Fase 1: meteo, movimento, Abbordaggio fortuito

Stato: bozza
Commit di riferimento: da compilare
Regole coinvolte: R-040–R-045, R-060–R-073d, R-080–R-087, R-050–R-053 (ordine turno e cornice)
Documenti: `tech/02_regole.md` §4–6, `tech/04_motore.md`, `tech/05_mappa.md`

## Obiettivo

Il motore gioca round completi di sola Fase 1 + ordine della Fase 2: rotte segrete, meteo, movimento simultaneo cella per cella con collisioni e attraversamenti, Abbordaggi fortuiti a 2 e a 3+ navi, ordine turno dal round 2. La Fase 2 in questo task può essere un passaggio vuoto (ogni giocatore "passa"), da riempire in 0003.

## Contratto

- Eventi di movimento per passo (`04` §3), così la vista può animare.
- Le carte crew che toccano questa parte (Timoniere, Navigatore, Medico, Jolly che le duplica) funzionano già qui.
- Vento in Poppa e lo stato "Svago" (R-045) esistono come stato del giocatore, anche se le carte/azioni che li attivano arrivano in 0003: i test possono impostarli direttamente.

## Lato codice (Claude Code)

_da compilare_

## Verifica

- `dotnet test Tools/RulesHarness` verde, con test per ogni caso limite di `08` §2 (movimento, Abbordaggio, meteo, ordine turno).
- La simulazione con bot casuale gioca 200 round × 2/4/8 giocatori senza eccezioni né violazioni di invarianti.

## Report
