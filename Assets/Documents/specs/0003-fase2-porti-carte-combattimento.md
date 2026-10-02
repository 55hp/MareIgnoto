# 0003 — Fase 2: turno in mare, porti, carte Pirateria, combattimento

Stato: bozza
Commit di riferimento: da compilare
Regole coinvolte: R-010–R-023, R-050–R-058, R-090–R-123
Documenti: `tech/02_regole.md` §2, §4, §7; `tech/03_contenuti.md` §1–2

## Obiettivo

La Fase 2 è completa: turno in mare (pesca 2 oppure swap/attacco/carta), turno in porto (5 azioni), Vedetta, tutte le carte Pirateria (battaglia, economia, Meteo), il combattimento completo (duello Bordata!/Parlè!, Cannoniere, Falconet, Saker, Culverin, Nostromo, Arrembaggio!, Quartiermastro) e tutti gli effetti crew sopra coperta, Jolly compreso.

## Contratto

- Ogni decisione del combattimento ha proprietario e segretezza corretti (`04` §2): il difensore risponde con il dispositivo in mano.
- Eventi di combattimento sufficienti a mostrare il botta e risposta carta per carta.

## Lato codice (Claude Code)

_da compilare_

## Verifica

- `dotnet test Tools/RulesHarness` verde, con test per ogni carta e ogni effetto crew, più i casi di `08` §2 (combattimento, Medico, Jolly).
- Simulazione: partite di 60 round × 2/4/8 giocatori senza eccezioni né violazioni.

## Report
