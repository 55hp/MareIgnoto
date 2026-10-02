# 0004 — Missioni, fine partita, punteggio, simulazione

Stato: bozza
Commit di riferimento: da compilare
Regole coinvolte: R-130–R-147, R-150
Documenti: `tech/02_regole.md` §8–10, `tech/03_contenuti.md` §3–4, `tech/08_test-e-simulazione.md`

## Obiettivo

Partite complete dall'inizio alla fine: missioni Corsaro con tracciamento e completamento immediato, Isola Sacra e Tesoro, round di cortesia, punteggio finale con poker, Jolly e Nostromo, `GameResult` con il dettaglio della taglia per giocatore. `SimulationRunner` completo.

## Contratto

- `GameResult`: classifica, vincitore/i, e per ogni giocatore il dettaglio delle fonti (segnalini da battaglie, missioni, Tesoro; monete/10; missioni incomplete; poker con combinazione riconosciuta) — serve alla schermata finale e al tutorial.
- `SimulationRunner` come `08` §3, con report testuale.

## Lato codice (Claude Code)

_da compilare_

## Verifica

- `dotnet test Tools/RulesHarness` verde, con test di ogni missione (completata e no), R-131, ogni combinazione poker, Jolly, Nostromo, parità (R-147).
- **Gate di Fase 1 del piano**: simulazione di 1000 partite (seed 1–1000, 2/4/8 giocatori) senza eccezioni né violazioni di invarianti. Allegare al report il riepilogo statistico.

## Report
