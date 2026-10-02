# 0007 — Combattimento, dadi, porto, schermata finale, menu

Stato: bozza
Commit di riferimento: da compilare
Documenti: `tech/06_presentazione.md`, `tech/03_contenuti.md` §4

## Obiettivo

Rifinire la leggibilità dei momenti chiave: `PANEL_Combat` con il botta e risposta carta per carta, `PANEL_Dice` animato, turno di porto chiaro (costi effettivi col Cuoco), `UI_EndScreen` con il dettaglio della taglia (`GameResult`), scena `Menu.unity` (giocatori, nomi, umano/bot, seed, Gioca, Tutorial).

## Contratto

Nomi `06` §1–2. Passi A/B/C come 0005.

## Verifica

- Una battaglia con 3+ scambi è comprensibile senza leggere il log.
- La schermata finale mostra tutte le voci di R-143 per ogni giocatore.
- Menu → Board → fine partita → Menu funziona.

## Report
