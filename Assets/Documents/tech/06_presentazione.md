# 06 — Presentazione: scena, UI, hot-seat

**[DEFAULT]** piattaforma del prototipo: PC (Editor e build Windows/macOS), mouse, landscape, risoluzione di riferimento 1920×1080. Hot-seat locale da 2 a 8 giocatori sullo stesso schermo. Grafica placeholder.

## 1. Scene

| Scena | Contenuto |
|---|---|
| `Assets/Scenes/Menu.unity` | Numero giocatori (2–8), nomi, per ogni posto Umano/Bot casuale, seed opzionale, pulsanti "Gioca" e "Tutorial" |
| `Assets/Scenes/Board.unity` | Partita. Si avvia anche direttamente da Editor con una configurazione di test (4 giocatori umani, seed fisso) per non passare dal menu a ogni Play |

Il passaggio Menu → Board porta i parametri tramite un oggetto di avvio (`GameLaunchRequest`, ScriptableObject runtime o classe statica **immutabile** impostata prima del load: Claude Code sceglie, purché non sia stato statico mutabile condiviso).

## 2. Gerarchia di `Board.unity` — contratto dei nomi

Bezi crea questi oggetti con **questi nomi esatti**; Claude Code scrive i componenti che si aspettano questa struttura tramite `[SerializeField]`.

```
/GAME_BOOTSTRAP            GameBootstrap (refs: RulesConfigAsset, MapLayoutAsset, GameFlowController)
/GAME_FLOW                 GameFlowController, EventPlayer
/BOARD
   /BOARD_Tiles            BoardView (istanzia le celle dal MapLayout: unica eccezione "generato a runtime"; mostra anche i segnalini isola, R-097)
   /BOARD_Zones            ZoneOverlayView (colore per livello meteo e linea di contorno lungo il perimetro delle celle, una forma per zona; il livello è un numero quando è ≥ 1)
   /BOARD_Ships            ShipsView (una nave per giocatore, istanziate dal prefab)
   /BOARD_Highlights       CellHighlightView (celle raggiungibili, bersagli, rotte)
/CAMERA_Rig
   /Main Camera            vista dall'alto inclinata, ortografica [DEFAULT]
/Directional Light
/UI_Canvas                 Canvas Screen Space Overlay, CanvasScaler 1920×1080, match 0.5
   /UI_TopBar              round, fase, giocatore attivo, vento (WindRoseView), Tesoro
   /UI_PlayerStrip         riepilogo pubblico di ogni giocatore (monete, segnalini, crew sopra coperta, n° carte)
   /UI_Hand                mano Pirateria del giocatore che ha il dispositivo (HandView)
   /UI_Crew                5 slot della ciurma del giocatore attivo (CrewView)
   /UI_Missions            missioni in mano del giocatore attivo (MissionsView)
   /UI_DecisionPanels      un pannello per famiglia di decisioni, tutti disattivi all'avvio
      /PANEL_Heading       scelta rotta (8 pulsanti a rosa dei venti)
      /PANEL_Offer         offerta a Gartya
      /PANEL_ChooseCards   scelta generica di N carte da un insieme (missioni, crew, scarti)
      /PANEL_TurnActions   azioni del turno in mare / in porto
      /PANEL_Target        scelta bersaglio (zona, nave, slot, direzione)
      /PANEL_Combat        duello: carte giocate, risposta, esito
      /PANEL_Dice          tiro di dado animato
      /PANEL_Confirm       conferme brevi (Medico, Vedetta, paga 5 monete)
   /UI_PassDevice          schermo pieno "Passa il dispositivo a <nome>" + pulsante "Sono io"
   /UI_Log                 log di partita (testo dagli eventi)
   /UI_EndScreen           classifica finale con dettaglio della taglia
   /UI_Tutorial            fumetto del tutorial + evidenziatore (vedi 07)
/EventSystem               StandaloneInputModule (Legacy Input Manager attivo)
```

Prefab (in `Assets/Game/Content/Prefabs/`): `Tile_Sea`, `Tile_Island`, `Tile_Sacred`, `Tile_Border`, `Ship`, `UI_Card` (usata per Pirateria, Crew e Missioni, con varianti di colore per tipo), `UI_CrewSlot`.

## 3. Hot-seat e segretezza

- Prima di ogni decisione **segreta** (`04_motore.md` §2) di un giocatore diverso dall'ultimo che ha avuto il dispositivo, `GameFlowController` mostra `UI_PassDevice` e nasconde mano, missioni e crew sotto coperta finché il giocatore non preme "Sono io".
- Le decisioni non segrete (rivelazioni, scelte di bersaglio a carte scoperte) non richiedono il passaggio.
- In Fase 1 le rotte si raccolgono una per volta, ognuna con passaggio di dispositivo; poi la rivelazione è pubblica e animata.
- Le informazioni mostrate si leggono sempre con `ViewFor(giocatoreCheHaIlDispositivo)`.
- Posti "Bot casuale": il bot risponde subito, senza passaggio di dispositivo, con un ritardo breve per rendere leggibile la scena.

## 4. Riproduzione degli eventi

- `EventPlayer` riproduce gli eventi in coda uno alla volta; ogni vista interessata reagisce (nave che avanza di una cella, dado che rotola, zona che cambia colore, monete che salgono).
- Durata breve e configurabile (velocità di riproduzione nell'Inspector di `EventPlayer`; pulsante "Salta animazioni").
- Il log testuale è sempre completo, anche se le animazioni vengono saltate.

## 5. Testi

- Tutti i testi mostrati al giocatore sono in italiano e stanno in un unico punto del codice (`UiText`, assembly Unity). Nessun testo scritto a mano nei prefab o nelle scene: nei campi TMP dei prefab si lascia un segnaposto `[ph]`.
- Nomi delle carte, effetti e testi delle missioni: da `03_contenuti.md`, in `UiText`.
- Tutto il testo UI è TextMeshPro.

## 6. Grafica

Placeholder: celle colorate (mare blu, isola sabbia, Isola Sacra oro, cornice grigio scuro), navi colorate per giocatore, zone meteo come quad semitrasparenti (Normale invisibile, Mare Mosso giallo, Tempesta rosso). Le illustrazioni del vecchio prototipo in `Assets/Art/` (capitani, crew, dorsi carta) si possono usare dove calzano (gli 8 capitani come avatar degli 8 posti), ma non sono necessarie per il prototipo giocabile.
