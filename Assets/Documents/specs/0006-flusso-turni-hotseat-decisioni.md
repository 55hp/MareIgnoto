# 0006 — Flusso turni, hot-seat, pannelli di decisione

Stato: bozza
Commit di riferimento: da compilare
Documenti: `tech/04_motore.md` §2, `tech/06_presentazione.md` §2–3, §5

## Obiettivo

Una partita giocabile interamente da umani in hot-seat dal setup alla fine, con UI essenziale: passaggio del dispositivo per le decisioni segrete, pannelli per tutte le decisioni del motore, mano, ciurma e missioni del giocatore che ha il dispositivo, riepilogo pubblico dei giocatori.

## Contratto

Gerarchia `06` §2: `/UI_Canvas/UI_TopBar`, `UI_PlayerStrip`, `UI_Hand`, `UI_Crew`, `UI_Missions`, `UI_DecisionPanels/*`, `UI_PassDevice`. Prefab `UI_Card`, `UI_CrewSlot`.
Ogni tipo di decisione del motore ha un pannello che la gestisce; un pannello generico (`PANEL_ChooseCards`) copre tutte le scelte "N carte da un insieme".
Il lavoro è in passi come 0005: **A** Bezi (layout UI con nomi esatti e prefab, in parallelo al codice), **B** Claude Code (script), **C** Bezi (wiring).

## Lato codice (Claude Code)

_da compilare_

## Lato Editor (Bezi) — checklist

_Passo A: layout dei pannelli e prefab dal contratto. Passo C: da compilare da Claude Code._

## Verifica

- Partita completa a 3 umani in hot-seat: nessuna informazione segreta visibile al giocatore sbagliato; ogni decisione ha un pannello; la partita arriva alla fine.
- Posti misti umani/bot funzionano.

## Report
