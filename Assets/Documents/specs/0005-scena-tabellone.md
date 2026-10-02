# 0005 — Scena tabellone: mappa, navi, zone, vento

Stato: bozza
Commit di riferimento: da compilare
Documenti: `tech/05_mappa.md`, `tech/06_presentazione.md` §1–2, §4, §6

## Obiettivo

`Board.unity` mostra la mappa 20×20 dal `MapLayout.asset`, le navi, le zone meteo e il vento, e riproduce gli eventi della Fase 1 (rotte, meteo, movimento cella per cella, collisioni, Abbordaggio) da una partita giocata da soli bot casuali. Ancora nessuna UI di decisione.

## Contratto

Gerarchia e nomi: `06` §2 (parte `/GAME_BOOTSTRAP`, `/GAME_FLOW`, `/BOARD...`, `/CAMERA_Rig`, `/Directional Light`, `/UI_Canvas/UI_TopBar` col solo vento, `/UI_Canvas/UI_Log`).
Prefab: `Tile_Sea`, `Tile_Island`, `Tile_Sacred`, `Tile_Border`, `Ship` in `Assets/Game/Content/Prefabs/`.
`GameBootstrap` con "avvio diretto di test": tutti i posti bot casuali, seed nell'Inspector, numero giocatori nell'Inspector.

Il lavoro si divide in tre passi:
- **A (Bezi, in parallelo a 0002–0004)**: `MapLayout.asset` dal layout approvato in `05_mappa.md` §6; scena `Board.unity` con la gerarchia vuota del contratto; prefab placeholder delle tile e della nave (cubi/quad con materiali URP Lit colorati, `06` §6); camera e luce.
- **B (Claude Code, dopo 0004)**: `GameBootstrap`, `GameFlowController`, `EventPlayer`, `BoardView`, `ZoneOverlayView`, `ShipsView`, `CellHighlightView`, `WindRoseView`, `LogView`, `UiText` (prima versione).
- **C (Bezi)**: aggiunta dei componenti agli oggetti del contratto e wiring dei `[SerializeField]`.

## Lato codice (Claude Code)

_da compilare (passo B)_

## Lato Editor (Bezi) — checklist

Passo A:
- [ ] `Assets/Game/Content/Config/MapLayout.asset` (`Create/MareIgnoto/Map Layout`) con le isole e l'Isola Sacra del layout approvato (`05_mappa.md` §6) e i preset di partenza di `05` §4. Esito di `Validate()` riportato.
- [ ] `Assets/Scenes/Board.unity` con la gerarchia del contratto (oggetti vuoti, nomi esatti).
- [ ] Prefab placeholder in `Assets/Game/Content/Prefabs/` e materiali in `Assets/Game/Content/Materials/`.
- [ ] Camera ortografica dall'alto, inclinata, che inquadra tutta la mappa a 1920×1080.

Passo C: _da compilare da Claude Code nel passo B_.

## Per Franci — modifiche isolate

- Aggiungere `Board.unity` alle Build Settings.

## Verifica

- Play da `Board.unity` con 4 bot: si vedono la mappa corretta, le navi sui punti di partenza, le rotte rivelate, le navi che avanzano cella per cella, le collisioni e gli Abbordaggi nel log, le zone che cambiano colore, il vento che gira.

## Report
