# 0005 — Scena tabellone: mappa, navi, zone, vento

Stato: bozza
Commit di riferimento: `5013e61` (passo B)
Documenti: `tech/05_mappa.md`, `tech/06_presentazione.md` §1–2, §4, §6

## Obiettivo

`Board.unity` mostra la mappa 25×25 dal `MapLayout.asset`, le navi, le zone meteo e il vento, e riproduce gli eventi della Fase 1 (rotte, meteo, movimento cella per cella, collisioni, Abbordaggio) da una partita giocata da soli bot casuali. Ancora nessuna UI di decisione.

## Contratto

Gerarchia e nomi: `06` §2 (parte `/GAME_BOOTSTRAP`, `/GAME_FLOW`, `/BOARD...`, `/CAMERA_Rig`, `/Directional Light`, `/UI_Canvas/UI_TopBar` col solo vento, `/UI_Canvas/UI_Log`).
Prefab: `Tile_Sea`, `Tile_Island`, `Tile_Sacred`, `Tile_Border`, `Ship` in `Assets/Game/Content/Prefabs/`.
`GameBootstrap` con "avvio diretto di test": tutti i posti bot casuali, seed nell'Inspector, numero giocatori nell'Inspector. `BoardView` mostra anche i segnalini isola (R-097) come dischi del colore del giocatore sopra la cella isola; `ZoneOverlayView` disegna ogni zona, sempre, come forma con contorno lungo il perimetro delle sue celle (riempimento quasi trasparente a livello 0, colorato da livello 1) e ne mostra il livello come numero quando è ≥ 1.

Il lavoro si divide in tre passi:
- **A (Bezi, in parallelo a 0002–0004)**: `MapLayout.asset` dal layout approvato in `05_mappa.md` §6; scena `Board.unity` con la gerarchia vuota del contratto; prefab placeholder delle tile e della nave (cubi/quad con materiali URP Lit colorati, `06` §6); camera e luce.
- **B (Claude Code, dopo 0004)**: `GameBootstrap`, `GameFlowController`, `EventPlayer`, `BoardView`, `ZoneOverlayView`, `ShipsView`, `CellHighlightView`, `WindRoseView`, `LogView`, `UiText` (prima versione).
- **C (Bezi)**: aggiunta dei componenti agli oggetti del contratto e wiring dei `[SerializeField]`.

## Lato codice (Claude Code)

Passo B, commit `5013e61`. Assembly `hp55games.MareIgnoto.Unity` (cartelle `Flow/`, `Views/`), più una parte pura in Rules. Nessuna scena, prefab o asset toccati.

**Avvio e ciclo** (`Assets/Game/Unity/Flow/`)
- `GameBootstrap` (composition root, `/GAME_BOOTSTRAP`): avvio diretto di test, tutti i posti bot casuali, `playerCount` e `seed` nell'Inspector. Crea la `GameSession` da `RulesConfigAsset` + `MapLayoutAsset` (nomi dei posti da `UiText.BotName`) e la passa a `GameFlowController` con un `RandomBot` seedato. Configurazione o mappa non valide: errore in Console e componente disabilitato.
- `GameFlowController` (`/GAME_FLOW`): fa riprodurre gli eventi, poi, quando `EventPlayer` è fermo, aspetta `botDelaySeconds` e risponde con il bot. Mostra gli eventi come li vede uno spettatore (`ViewFor(-1)`: degli eventi privati solo la forma pubblica). Nessuna UI di decisione, nessun passaggio del dispositivo (non servono con soli bot).
- `EventPlayer` (`/GAME_FLOW`): coda di eventi, uno alla volta; ogni evento va a tutte le viste dell'array `views` e il successivo parte quando hanno finito. `secondsPerAnimation`, `speed`, `skipAnimations` nell'Inspector (il log resta completo anche senza animazioni).

**Viste** (`Assets/Game/Unity/Views/`, tutte derivano da `GameView`: `Initialize(stato)` all'avvio, `Play(evento, durata)` per ogni evento; reagiscono ai dati degli eventi, non allo stato corrente, che è più avanti)
- `BoardView` (`/BOARD/BOARD_Tiles`): una tile per cella dal prefab del tipo di cella, cella (x, y) in (x · `cellSize`, 0, y · `cellSize`) nello spazio dell'oggetto (`cellSize` = 1). Converte celle e spigoli in posizioni per le altre viste. Segnalini isola (R-097): un disco per giocatore dal prefab, colorato con la palette, sulla prima cella dell'isola, spostato di poco per posto; si sposta con `IslandMarkerPlacedEvent`.
- `ZoneOverlayView` (`/BOARD/BOARD_Zones`): per ogni zona un riempimento (mesh delle sue celle sul prefab con `MeshFilter`), un contorno (`LineRenderer` dal prefab, chiuso, lungo il perimetro) e il livello come testo 3D TextMeshPro. Ogni zona è sempre disegnata: a livello 0 contorno e riempimento neutro quasi trasparente (`calmColor`, default bianco al 10%), Mare Mosso giallo, Tempesta rosso; la scelta della tinta è logica pura in Rules (`ZoneAppearance`, soglie da `RulesConfig.WeatherAt`, con test). Il numero compare dal livello 1. Si aggiorna con `ZoneChangedEvent`.
- `ShipsView` (`/BOARD/BOARD_Ships`): una nave per giocatore dal prefab `Ship`, colorata con la palette; avanza una cella per `ShipMovedEvent` (animazione di `durata`), poi riposizionamenti, attraversamenti e allineamento con `MovementEndedEvent`. Più navi sulla stessa cella si dispongono in cerchio.
- `CellHighlightView` (`/BOARD/BOARD_Highlights`, minima): le rotte rivelate, un segno del colore del giocatore sulla cella verso cui punta, girato nella direzione; segue le rotazioni del meteo e sparisce quando parte il movimento.
- `WindRoseView` (`/UI_Canvas/UI_TopBar`): lancetta (Nord in alto, 45° per scatto orario) e scritta del vento.
- `LogView` (`/UI_Canvas/UI_Log`): una riga per evento significativo, le ultime `maxLines`.

**Testi e colori**
- `UiText` (`Assets/Game/Unity/UiText.cs`): tutti i testi per il giocatore, in italiano: nomi dei bot, direzioni, vento, meteo, azioni di porto, livello delle zone, righe del log.
- `PlayerPaletteAsset` (`Create/MareIgnoto/Player Palette`): gli 8 colori dei posti, unica fonte per navi, segnalini e rotte. I colori si applicano con `MaterialPropertyBlock` (`_BaseColor` e `_Color`), senza toccare i materiali.

**Geometria pura e test**: `ZoneGeometry` (`Assets/Game/Rules/Map/`, assembly Rules): contorni chiusi lungo il perimetro delle celle (antiorari all'esterno, orari attorno ai buchi, senza vertici intermedi) e la cella dove scrivere il livello (la più vicina al baricentro, sempre dentro la zona). `ZoneGeometryTests`: casi base, buco, celle che si toccano per uno spigolo, e tutte le 20 zone v4 (un contorno ciascuna, area uguale al numero di celle). `dotnet test Tools/RulesHarness`: 405 verdi.

**Regole del CLAUDE.md**: niente `Find`, `AddComponent`, `Camera.main`; gli oggetti generati (tile, navi, segnalini, zone) nascono da prefab assegnati in Inspector. Ogni riferimento mancante produce `Debug.LogError` con il nome del campo e disabilita il componente. Uniche letture di componenti a runtime: `GetComponentsInChildren<Renderer>` sulle istanze appena create (per colorarle) e `GetComponent<Renderer>` sul riempimento della zona istanziato.

## Lato Editor (Bezi) — checklist

Passo A (`MapLayout.asset` non è più un compito di Bezi: lo genera il comando Editor, vedi "Per Franci"):
- [ ] `Assets/Scenes/Board.unity` con la gerarchia del contratto (oggetti vuoti, nomi esatti).
- [ ] Prefab placeholder in `Assets/Game/Content/Prefabs/` e materiali in `Assets/Game/Content/Materials/`.
- [ ] Camera ortografica dall'alto, inclinata, che inquadra tutta la mappa **25×25** a 1920×1080 (centro della mappa in world (12, 0, 12) con celle da 1, `05` §1). La camera fatta nel passo A è tarata sulla 20×20 (centro (9.5, 0, 9.5), report di Bezi): va rifatta.
- [ ] Pipeline URP: in `ProjectSettings/GraphicsSettings.asset` `m_CustomRenderPipeline` non è assegnato (nota del report di Bezi), quindi i materiali URP Lit restano magenta. Nel progetto non esiste nessun asset di pipeline URP (verificato nel repo e nella storia): va creato (URP Asset con Universal Renderer, in `Assets/Game/Content/Rendering/`) e assegnato in Graphics e in tutti i livelli di Quality.

Passo C (wiring, una riga per collegamento: oggetto → componente → campo → cosa assegnare). Prima crea gli oggetti nuovi:
- [ ] `Assets/Game/Content/Config/PlayerPalette.asset` (`Create/MareIgnoto/Player Palette`): nasce con 8 colori, nessun valore da cambiare.
- [ ] Prefab `Assets/Game/Content/Prefabs/IslandMarker.prefab`: disco (cilindro schiacciato, circa 0.3 × 0.05 × 0.3) con materiale URP Lit neutro (il colore lo mette il codice).
- [ ] Prefab `RouteMarker.prefab`: segno piatto che punta lungo +Z (freccia o quad stretto), materiale URP Lit o Unlit neutro.
- [ ] Prefab `ZoneFill.prefab`: `MeshFilter` (mesh vuota) + `MeshRenderer` con un materiale trasparente (URP Unlit, Surface Type Transparent).
- [ ] Prefab `ZoneOutline.prefab`: `LineRenderer` con larghezza circa 0.06, `Use World Space` attivo, materiale URP Unlit; nessun punto (li mette il codice).
- [ ] Prefab `ZoneLabel.prefab`: `TextMeshPro` 3D (non UGUI), ruotato di 90° su X per leggerlo dall'alto, centrato, testo `[ph]`.
- [ ] In `/UI_Canvas/UI_TopBar`: un'immagine `WindNeedle` (la lancetta, che a rotazione 0 punta in alto) e un `TextMeshProUGUI` `WindLabel` con testo `[ph]`.
- [ ] In `/UI_Canvas/UI_Log`: un `TextMeshProUGUI` `LogText` con testo `[ph]` (allineato in basso, a capo automatico).

Collegamenti:
- [ ] `/GAME_BOOTSTRAP` → `GameBootstrap` → `rulesConfig` → `Assets/Game/Content/Config/RulesConfig.asset`
- [ ] `/GAME_BOOTSTRAP` → `GameBootstrap` → `mapLayout` → `Assets/Game/Content/Config/MapLayout.asset`
- [ ] `/GAME_BOOTSTRAP` → `GameBootstrap` → `flow` → `/GAME_FLOW` (componente `GameFlowController`)
- [ ] `/GAME_BOOTSTRAP` → `GameBootstrap` → `playerCount`, `seed` → lasciare i default (4, 1)
- [ ] `/GAME_FLOW` → `GameFlowController` → `eventPlayer` → `/GAME_FLOW` (componente `EventPlayer`)
- [ ] `/GAME_FLOW` → `EventPlayer` → `views` → 6 elementi: `/BOARD/BOARD_Tiles` (BoardView), `/BOARD/BOARD_Zones` (ZoneOverlayView), `/BOARD/BOARD_Ships` (ShipsView), `/BOARD/BOARD_Highlights` (CellHighlightView), `/UI_Canvas/UI_TopBar` (WindRoseView), `/UI_Canvas/UI_Log` (LogView)
- [ ] `/BOARD/BOARD_Tiles` → `BoardView` → `seaTilePrefab` → `Prefabs/Tile_Sea.prefab`
- [ ] `/BOARD/BOARD_Tiles` → `BoardView` → `islandTilePrefab` → `Prefabs/Tile_Island.prefab`
- [ ] `/BOARD/BOARD_Tiles` → `BoardView` → `sacredTilePrefab` → `Prefabs/Tile_Sacred.prefab`
- [ ] `/BOARD/BOARD_Tiles` → `BoardView` → `borderTilePrefab` → `Prefabs/Tile_Border.prefab`
- [ ] `/BOARD/BOARD_Tiles` → `BoardView` → `islandMarkerPrefab` → `Prefabs/IslandMarker.prefab`
- [ ] `/BOARD/BOARD_Tiles` → `BoardView` → `palette` → `Config/PlayerPalette.asset`
- [ ] `/BOARD/BOARD_Zones` → `ZoneOverlayView` → `board` → `/BOARD/BOARD_Tiles` (BoardView)
- [ ] `/BOARD/BOARD_Zones` → `ZoneOverlayView` → `fillPrefab` → `Prefabs/ZoneFill.prefab`
- [ ] `/BOARD/BOARD_Zones` → `ZoneOverlayView` → `outlinePrefab` → `Prefabs/ZoneOutline.prefab`
- [ ] `/BOARD/BOARD_Zones` → `ZoneOverlayView` → `labelPrefab` → `Prefabs/ZoneLabel.prefab`
- [ ] `/BOARD/BOARD_Ships` → `ShipsView` → `board` → `/BOARD/BOARD_Tiles` (BoardView)
- [ ] `/BOARD/BOARD_Ships` → `ShipsView` → `shipPrefab` → `Prefabs/Ship.prefab`
- [ ] `/BOARD/BOARD_Ships` → `ShipsView` → `palette` → `Config/PlayerPalette.asset`
- [ ] `/BOARD/BOARD_Highlights` → `CellHighlightView` → `board` → `/BOARD/BOARD_Tiles` (BoardView)
- [ ] `/BOARD/BOARD_Highlights` → `CellHighlightView` → `ships` → `/BOARD/BOARD_Ships` (ShipsView)
- [ ] `/BOARD/BOARD_Highlights` → `CellHighlightView` → `palette` → `Config/PlayerPalette.asset`
- [ ] `/BOARD/BOARD_Highlights` → `CellHighlightView` → `routeMarkerPrefab` → `Prefabs/RouteMarker.prefab`
- [ ] `/UI_Canvas/UI_TopBar` → `WindRoseView` → `needle` → `/UI_Canvas/UI_TopBar/WindNeedle` (RectTransform)
- [ ] `/UI_Canvas/UI_TopBar` → `WindRoseView` → `label` → `/UI_Canvas/UI_TopBar/WindLabel` (TextMeshProUGUI)
- [ ] `/UI_Canvas/UI_Log` → `LogView` → `text` → `/UI_Canvas/UI_Log/LogText` (TextMeshProUGUI)
- [ ] `/BOARD` e figli con posizione (0, 0, 0), rotazione 0 e scala 1: la cella (x, y) va in (x, 0, y).

## Per Franci — modifiche isolate

- **`MapLayout.asset` (passo A, primo punto)**: nell'Editor eseguire **MareIgnoto > Create MapLayout v4**. Il comando crea `Assets/Game/Content/Config/MapLayout.asset` dal layout v4 (`LayoutV4`, `05` §3–§6); se il file esiste chiede prima di sovrascriverlo. Controllare che la Console dica `[MapLayout] … Validate OK`, poi committare `MapLayout.asset` e il suo `.meta`. Test Runner → EditMode: `MapLayoutV4AssetTests` non deve più risultare "Ignorato" ed essere verde.
- Aggiungere `Board.unity` alle Build Settings.
- Dopo il passo B: tornare sull'Editor per i `.meta` dei file nuovi (in `Assets/Game/Unity/`: `UiText.cs`, `PlayerPaletteAsset.cs`, le cartelle `Flow/` e `Views/` con i loro file; in Rules `Map/ZoneGeometry.cs`; nei test `ZoneGeometryTests.cs`) e committarli.

## Verifica

- Play da `Board.unity` con 4 bot: si vedono la mappa corretta, le navi sui punti di partenza, le rotte rivelate, le navi che avanzano cella per cella, le collisioni e gli Abbordaggi nel log, le zone che cambiano colore, il vento che gira.

## Report

### Bezi — 2026-10-02 — commit 311887f

1. Assets/Game/Content/Config/RulesConfig.asset creato con successo con i valori di default tramite ScriptableObject di tipo hp55games.MareIgnoto.Unity.RulesConfigAsset. Valori verificati: Pirate Cards con 13 voci, Missions con 17 voci, Poker Scores con 10 voci, Starting Coins pari a 10, Min Players 2, Max Players 8, Min Map Size 5, Zone Size 3.

2. Assets/Game/Content/Config/MapLayout.asset: verificata la sezione 6 di tech/05_mappa.md ("Layout delle isole — DA DEFINIRE"). La sezione non contiene il layout approvato per le celle delle isole ne' per l'Isola Sacra. Come prescritto dalle istruzioni ("if §6 still says DA DEFINIRE, stop and report") e in conformita' con tech/05_mappa.md paragrafo 6 e Bezi_Rules.md, non e' stata inventata alcuna disposizione e la creazione di MapLayout.asset e' stata interrotta. Di conseguenza nessun esito di Validate() e' generabile per MapLayout.asset fino alla definizione del layout da parte di Franci.

### Bezi — 2026-10-02 — commit 311887f

Passo A, scheletro scena Board.unity completato. Creata la scena Assets/Scenes/Board.unity contenente la gerarchia definita in tech/06_presentazione.md paragrafo 2, limitata agli oggetti previsti dal Contratto di spec 0005 (oggetti vuoti, nomi esatti, nessun componente logico di gameplay):

- /GAME_BOOTSTRAP: GameObject vuoto, Transform localPosition (0, 0, 0), localRotation (0, 0, 0), localScale (1, 1, 1).
- /GAME_FLOW: GameObject vuoto, Transform localPosition (0, 0, 0), localRotation (0, 0, 0), localScale (1, 1, 1).
- /BOARD: GameObject vuoto, Transform localPosition (0, 0, 0), localRotation (0, 0, 0), localScale (1, 1, 1).
  - /BOARD/BOARD_Tiles: GameObject vuoto, Transform localPosition (0, 0, 0), localRotation (0, 0, 0), localScale (1, 1, 1).
  - /BOARD/BOARD_Zones: GameObject vuoto, Transform localPosition (0, 0, 0), localRotation (0, 0, 0), localScale (1, 1, 1).
  - /BOARD/BOARD_Ships: GameObject vuoto, Transform localPosition (0, 0, 0), localRotation (0, 0, 0), localScale (1, 1, 1).
  - /BOARD/BOARD_Highlights: GameObject vuoto, Transform localPosition (0, 0, 0), localRotation (0, 0, 0), localScale (1, 1, 1).
- /CAMERA_Rig: GameObject vuoto, Transform localPosition (0, 0, 0), localRotation (0, 0, 0), localScale (1, 1, 1).
  - /CAMERA_Rig/Main Camera: tag MainCamera. Transform localPosition (9.5, 20, -2.05), localRotation (60, 0, 0), localScale (1, 1, 1). Componente UnityEngine.Camera con orthographic true, orthographicSize 11, nearClipPlane 0.3, farClipPlane 100, clearFlags Skybox, cullingMask Everything. Componente UnityEngine.AudioListener enabled true. L inquadratura punta al centro della mappa 20x20 a coordinate world (9.5, 0, 9.5) con inclinazione di 60 gradi dall alto e inquadra l intera mappa 20x20 nel formato di riferimento 1920x1080.
- /Directional Light: Transform localPosition (0, 10, 0), localRotation (50, -30, 0), localScale (1, 1, 1). Componente UnityEngine.Light con type Directional, color (1, 0.956, 0.839, 1), intensity 1, shadows Soft.
- /UI_Canvas: RectTransform sizeDelta (1920, 1080). Componente UnityEngine.Canvas con renderMode ScreenSpaceOverlay, pixelPerfect false. Componente UnityEngine.UI.CanvasScaler con uiScaleMode ScaleWithScreenSize, referenceResolution (1920, 1080), screenMatchMode MatchWidthOrHeight, matchWidthOrHeight 0.5. Componente UnityEngine.UI.GraphicRaycaster con ignoreReversedGraphics true, blockingObjects None.
  - /UI_Canvas/UI_TopBar: GameObject vuoto con RectTransform.
  - /UI_Canvas/UI_Log: GameObject vuoto con RectTransform.
- /EventSystem: Transform localPosition (0, 0, 0). Componente UnityEngine.EventSystems.EventSystem con sendNavigationEvents true. Componente UnityEngine.EventSystems.StandaloneInputModule con inputActionsPerSecond 10, repeatDelay 0.5, horizontalAxis Horizontal, verticalAxis Vertical, submitButton Submit, cancelButton Cancel.

### Bezi — 2026-10-02 — commit 311887f

Passo A, prefab placeholder e materiali URP Lit completati. Creati i 5 materiali URP Lit in Assets/Game/Content/Materials/ e i 5 prefab in Assets/Game/Content/Prefabs/:

Materiali creati con shader Universal Render Pipeline/Lit:
- Assets/Game/Content/Materials/Mat_Tile_Sea.mat: _BaseColor (0.12, 0.42, 0.75, 1.0) mare blu, _Smoothness 0.5.
- Assets/Game/Content/Materials/Mat_Tile_Island.mat: _BaseColor (0.85, 0.74, 0.53, 1.0) isola sabbia, _Smoothness 0.1.
- Assets/Game/Content/Materials/Mat_Tile_Sacred.mat: _BaseColor (0.95, 0.78, 0.18, 1.0) Isola Sacra oro, _Metallic 0.7, _Smoothness 0.6.
- Assets/Game/Content/Materials/Mat_Tile_Border.mat: _BaseColor (0.22, 0.22, 0.24, 1.0) cornice grigio scuro, _Smoothness 0.2.
- Assets/Game/Content/Materials/Mat_Ship.mat: _BaseColor (0.85, 0.85, 0.85, 1.0) colore neutro (tinteggiabile via codice per giocatore), _Smoothness 0.3.

Prefab creati (nessuno script associato):
- Assets/Game/Content/Prefabs/Tile_Sea.prefab: radice Tile_Sea, Transform localScale (1, 0.05, 1), MeshFilter (Cube), BoxCollider size (1, 1, 1), MeshRenderer con Mat_Tile_Sea.mat.
- Assets/Game/Content/Prefabs/Tile_Island.prefab: radice Tile_Island, Transform localScale (1, 0.05, 1), MeshFilter (Cube), BoxCollider size (1, 1, 1), MeshRenderer con Mat_Tile_Island.mat.
- Assets/Game/Content/Prefabs/Tile_Sacred.prefab: radice Tile_Sacred, Transform localScale (1, 0.05, 1), MeshFilter (Cube), BoxCollider size (1, 1, 1), MeshRenderer con Mat_Tile_Sacred.mat.
- Assets/Game/Content/Prefabs/Tile_Border.prefab: radice Tile_Border, Transform localScale (1, 0.05, 1), MeshFilter (Cube), BoxCollider size (1, 1, 1), MeshRenderer con Mat_Tile_Border.mat.
- Assets/Game/Content/Prefabs/Ship.prefab: radice Ship con Transform localPosition (0, 0, 0), localRotation (0, 0, 0), localScale (1, 1, 1) (pivot alla base). Figlio /Ship/Visual con Transform localPosition (0, 0.25, 0), localScale (0.4, 0.5, 0.7), MeshFilter (Cube), BoxCollider size (1, 1, 1), MeshRenderer con Mat_Ship.mat.

Nota a margine (valore non coperto dal task): in ProjectSettings/GraphicsSettings.asset il campo m_CustomRenderPipeline risulta attualmente non assegnato (fileID: 0, Built-in active), pertanto i materiali URP Lit vengono renderizzati in magenta nelle anteprime isolate finche l asset pipeline URP non viene assegnato alla configurazione grafica del progetto.

### Claude Code — 2026-10-03 — commit fea3daf

Passo A, primo punto: il generatore di `MapLayout.asset` (l'asset non l'ho creato: nasce quando Franci esegue il comando).

- **Dati**: `LayoutV4` è passato da `Assets/Tests/EditMode/Rules/` all'assembly Rules (`Assets/Game/Rules/Map/LayoutV4.cs`, pubblico, namespace `hp55games.MareIgnoto.Rules.Map`), spostato con il suo `.meta` (stesso GUID). È l'unica copia del layout v4: la usano i test delle regole e il generatore; la partita legge `MapLayout.asset`.
- **Comando** `MareIgnoto/Create MapLayout v4` (`Assets/Game/Editor/MapLayoutV4Generator.cs`): costruisce il layout da `LayoutV4.Create()` (isole con id, Isola Sacra, zone con tipo e livello iniziale, preset per N = 2..8) e lo salva in `Assets/Game/Content/Config/MapLayout.asset`. Se l'asset esiste chiede conferma e lo sovrascrive sul posto (stesso GUID, con Undo); se a quel percorso c'è un file di altro tipo si ferma con un errore. Poi esegue `Validate()` con `RulesConfig.asset` (o i default se manca) e scrive in Console `Validate OK`, oppure un errore con i primi 10 problemi. Per scrivere l'asset ho aggiunto `MapLayoutAsset.EditorReplaceLayout`, compilato solo nell'Editor (`#if UNITY_EDITOR`).
- **Test EditMode** (`Assets/Tests/EditMode/Unity/MapLayoutV4AssetTests.cs`): carica l'asset, controlla `Validate()` e lo confronta con `LayoutV4` cella per cella (tipo, id isola, zona), poi ordine, tipo e livello iniziale delle zone e i preset per N = 2..8. Senza asset è ignorato con il messaggio "eseguire MareIgnoto > Create MapLayout v4". Il test già esistente `ProjectMapLayoutAssetIsValidIfPresent` resta.
- **Spec**: tolto dalla checklist di Bezi il punto su `MapLayout.asset`, sostituito dal passo "Per Franci". Nella checklist non c'erano voci separate per la camera 25×25 e per la pipeline URP: ho aggiornato la voce della camera (era per la mappa senza dimensioni; quella fatta da Bezi è tarata sulla 20×20) e aggiunto la voce URP dalla nota del report di Bezi.
- `dotnet test Tools/RulesHarness` verde, 398 test (i test Rules usano `LayoutV4` dal nuovo posto).

**Non verificato**: il codice Unity (generatore, `EditorReplaceLayout`, test EditMode) non si può compilare qui. Da controllare in Console dopo l'import:
- nessun errore di compilazione in `hp55games.MareIgnoto.Editor`, `hp55games.MareIgnoto.Unity` e `hp55games.MareIgnoto.Unity.Tests`;
- il menu **MareIgnoto > Create MapLayout v4** compare nella barra dei menu;
- dopo averlo eseguito: il messaggio `[MapLayout] Assets/Game/Content/Config/MapLayout.asset creato dal layout v4: Validate OK (con Assets/Game/Content/Config/RulesConfig.asset).`, nessun errore rosso;
- `.meta` nuovi da committare: `MapLayoutV4Generator.cs`, `MapLayoutV4AssetTests.cs` e, dopo il comando, `MapLayout.asset`.

### Claude Code — 2026-10-03 — commit 5013e61

Parte A applicata parola per parola in un commit a parte (`ce4dcbd`, "docs: patch Athena (viste tabellone, URP)"). Passo B scritto: dettaglio in "Lato codice", wiring per Bezi in "Passo C". Nessuna scena, prefab o asset toccati; `Assets/Scenes/Board.unity` risulta modificato nella working copy ma non è mio e l'ho lasciato fuori dal commit.

**Non compilato in Unity.** Qui non posso compilare il codice Unity. Ho compilato gli assembly Unity ed Editor e i test Unity contro stub delle API usate (UnityEngine, TMPro, UnityEditor, NUnit), scritti da me in un progetto temporaneo fuori dal repo: 0 errori, 0 avvisi. Il controllo riguarda solo sintassi e tipi, e copre anche il comando `Create MapLayout v4` della voce precedente. La parte pura (`ZoneGeometry`) è testata nell'harness: 405 test verdi.

Da controllare in Console (Franci):
- nessun errore di compilazione in `hp55games.MareIgnoto.Unity`, `hp55games.MareIgnoto.Editor`, `hp55games.MareIgnoto.Unity.Tests`;
- Test Runner → EditMode: `hp55games.MareIgnoto.Rules.Tests` verde (compresi `ZoneGeometryTests`);
- dopo il wiring di Bezi, Play da `Board.unity`: nessun `[NomeComponente] … manca il riferimento …` in rosso; se compare, il campo indicato non è collegato;
- in Play: 25×25 tile, 4 navi colorate sui punti di partenza, l'anello delle 8 zone rosse con il numero 5, il log che si riempie, la lancetta del vento che gira.

#### Note

- Le navi si muovono una alla volta, un passo per evento (gli eventi di movimento sono uno per nave per passo, `04` §3). Se si vuole il passo simultaneo a vista, `EventPlayer` può raggruppare gli `ShipMovedEvent` dello stesso passo: è una modifica solo di presentazione.
- Il livello delle zone si legge da `RulesConfig.WeatherAt`, quindi le soglie restano solo nella config.
- La camera (passo A, Bezi) è ancora tarata sulla 20×20; con la mappa 25×25 il centro è in (12, 0, 12).

### Bezi — 2026-10-03 — commit 5013e61

Passo A, completamento dei due micro-passi rimanenti (camera 25x25 e pipeline URP).

1. Camera:
Valori iniziali letti in Assets/Scenes/Board.unity su /CAMERA_Rig/Main Camera:
- Transform localPosition: (9.5, 20, -2.05), localRotation: (60, 0, 0), localScale: (1, 1, 1).
- UnityEngine.Camera: orthographic true, orthographicSize 11, nearClipPlane 0.3, farClipPlane 100, clearFlags Skybox, cullingMask Everything.
- Genitore /CAMERA_Rig: Transform localPosition (0, 0, 0), localRotation (0, 0, 0), localScale (1, 1, 1).
Inquadratura precedente tarata sulla vecchia mappa 20x20 con centro a coordinate world (9.5, 0, 9.5).

Valori finali impostati per la mappa 25x25 (celle 0..24 a world x,0,y, centro world (12, 0, 12)):
- Transform localPosition: (12, 20, 0.453), localRotation: (60, 0, 0), localScale: (1, 1, 1).
- UnityEngine.Camera: orthographic true, orthographicSize 12.
Verifica in Game view a 1920x1080: con inclinazione a 60 gradi, l asse ottico interseca esattamente il piano Y=0 a coordinate (12, 0, 12). Con orthographicSize 12, l estensione verticale visibile sul piano XZ copre da Z = -1.86 a Z = 25.86, includendo la cornice esterna delle celle (Z da -0.5 a 24.5) con un margine di circa 1 cella (1.35 celle, pari a circa 46 pixel dal bordo schermo superiore e inferiore). L estensione orizzontale copre da X = -9.33 a X = 33.33. Tutti e quattro gli angoli della mappa (0,0), (24,0), (0,24), (24,24) e la relativa cornice risultano interamente visibili nella Game view.

2. Pipeline URP:
Creati i seguenti asset in Assets/Game/Content/Rendering/:
- Assets/Game/Content/Rendering/UniversalRenderer.asset (ScriptableRendererData di tipo UniversalRendererData con PostProcessData di default).
- Assets/Game/Content/Rendering/UniversalRenderPipelineAsset.asset (UniversalRenderPipelineAsset configurato con UniversalRenderer.asset).

Impostazioni modificate nel progetto:
- ProjectSettings/GraphicsSettings.asset: campo m_CustomRenderPipeline impostato con riferimento a Assets/Game/Content/Rendering/UniversalRenderPipelineAsset.asset (guid e589ad1fd49f1ae4f81dcaf6ea7e05f9).
- ProjectSettings/QualitySettings.asset: campo customRenderPipeline impostato con riferimento a Assets/Game/Content/Rendering/UniversalRenderPipelineAsset.asset su tutti i 6 livelli di qualita (Very Low, Low, Medium, High, Very High, Ultra).

Verifica rendering:
- I 5 materiali in Assets/Game/Content/Materials/ (Mat_Tile_Sea.mat, Mat_Tile_Island.mat, Mat_Tile_Sacred.mat, Mat_Tile_Border.mat, Mat_Ship.mat) sono stati verificati in anteprima isolata: tutti mostrano correttamente i rispettivi colori PBR (blu mare, sabbia isola, oro metallico Isola Sacra, grigio scuro cornice, neutro nave) e non piu il magenta di fallback.
- I 5 prefab in Assets/Game/Content/Prefabs/ (Tile_Sea.prefab, Tile_Island.prefab, Tile_Sacred.prefab, Tile_Border.prefab, Ship.prefab) sono stati verificati in anteprima isolata: tutti mostrano correttamente i propri materiali e colori e non piu il magenta di fallback.

### Bezi — 2026-10-03 — commit 5013e61

Passo C, parte 1: creazione oggetti nuovi (asset, materiali URP neutri e prefab in Assets/Game/Content/). Nessuna modifica alla scena Board.unity e nessun codice C# aggiunto.

1. ScriptableObject PlayerPalette.asset:
- Creato Assets/Game/Content/Config/PlayerPalette.asset di tipo hp55games.MareIgnoto.Unity.PlayerPaletteAsset tramite menu Create/MareIgnoto/Player Palette.
- Contiene gli 8 colori di default per i posti giocatori:
  - Posto 0 (rosso): (0.86, 0.20, 0.18, 1.0)
  - Posto 1 (blu): (0.20, 0.45, 0.90, 1.0)
  - Posto 2 (verde): (0.20, 0.70, 0.30, 1.0)
  - Posto 3 (giallo): (0.95, 0.80, 0.15, 1.0)
  - Posto 4 (viola): (0.60, 0.30, 0.80, 1.0)
  - Posto 5 (arancio): (0.95, 0.55, 0.15, 1.0)
  - Posto 6 (ciano): (0.20, 0.80, 0.80, 1.0)
  - Posto 7 (rosa): (0.90, 0.40, 0.70, 1.0)

2. Materiali URP neutri creati in Assets/Game/Content/Materials/:
- Assets/Game/Content/Materials/Mat_IslandMarker.mat: shader Universal Render Pipeline/Lit, _BaseColor (0.85, 0.85, 0.85, 1.0), _Smoothness 0.3.
- Assets/Game/Content/Materials/Mat_RouteMarker.mat: shader Universal Render Pipeline/Unlit, _BaseColor (0.9, 0.9, 0.9, 1.0).
- Assets/Game/Content/Materials/Mat_ZoneFill.mat: shader Universal Render Pipeline/Unlit, superficie Transparent (renderQueue 3000, _Surface 1.0, _SrcBlend SrcAlpha, _DstBlend OneMinusSrcAlpha, _ZWrite 0, keyword _SURFACE_TYPE_TRANSPARENT, tag RenderType Transparent), _BaseColor (1.0, 1.0, 1.0, 0.4).
- Assets/Game/Content/Materials/Mat_ZoneOutline.mat: shader Universal Render Pipeline/Unlit, _BaseColor (1.0, 1.0, 1.0, 1.0).

3. Prefab creati in Assets/Game/Content/Prefabs/ (senza script custom associati):
- Assets/Game/Content/Prefabs/IslandMarker.prefab: radice IslandMarker con Transform localPosition (0, 0, 0), localRotation (0, 0, 0), localScale (0.3, 0.025, 0.3) (disco cilindrico di diametro 0.3 e altezza 0.05). MeshFilter con Cylinder (Library/unity default resources) e MeshRenderer con Mat_IslandMarker.mat.
- Assets/Game/Content/Prefabs/RouteMarker.prefab: radice RouteMarker con Transform localPosition (0, 0, 0), localRotation (0, 0, 0), localScale (0.2, 0.02, 0.6) (segno piatto orientato lungo l asse +Z). MeshFilter con Cube (Library/unity default resources) e MeshRenderer con Mat_RouteMarker.mat.
- Assets/Game/Content/Prefabs/ZoneFill.prefab: radice ZoneFill con Transform localPosition (0, 0, 0), localRotation (0, 0, 0), localScale (1, 1, 1). MeshFilter con sharedMesh nullo (mesh assegnata dal codice a runtime) e MeshRenderer con Mat_ZoneFill.mat.
- Assets/Game/Content/Prefabs/ZoneOutline.prefab: radice ZoneOutline con Transform localPosition (0, 0, 0), localRotation (0, 0, 0), localScale (1, 1, 1). LineRenderer con larghezza 0.06 (widthCurve costante a 0.06), useWorldSpace true, positionCount 0 (nessun punto impostato), materiale Mat_ZoneOutline.mat.
- Assets/Game/Content/Prefabs/ZoneLabel.prefab: radice ZoneLabel con Transform localPosition (0, 0, 0), localRotation (90, 0, 0) (ruotato di 90 gradi sull asse X per lettura dall alto), localScale (1, 1, 1). Componente TMPro.TextMeshPro 3D (non UGUI) con text [ph], alignment Center, fontSize 4, font LiberationSans SDF.

### Bezi — 2026-10-03 — commit 5013e61

Passo C, parte 2: oggetti UI in Assets/Scenes/Board.unity (/UI_Canvas/UI_TopBar e /UI_Canvas/UI_Log).

1. Stato iniziale rilevato (lettura prima della modifica):
- /UI_Canvas/UI_TopBar: 0 figli (childCount = 0). RectTransform con anchorMin (0.5, 0.5), anchorMax (0.5, 0.5), pivot (0.5, 0.5), anchoredPosition (0, 0), sizeDelta (100, 100).
- /UI_Canvas/UI_Log: 0 figli (childCount = 0). RectTransform con anchorMin (0.5, 0.5), anchorMax (0.5, 0.5), pivot (0.5, 0.5), anchoredPosition (0, 0), sizeDelta (100, 100).

2. Dimensionamento e ancoraggio dei contenitori per Canvas 1920x1080:
- /UI_Canvas/UI_TopBar: barra superiore a estensione orizzontale completa. RectTransform con anchorMin (0, 1), anchorMax (1, 1), pivot (0.5, 1), anchoredPosition (0, 0), sizeDelta (0, 60).
- /UI_Canvas/UI_Log: pannello di log in basso a destra. RectTransform con anchorMin (1, 0), anchorMax (1, 0), pivot (1, 0), anchoredPosition (-20, 20), sizeDelta (420, 280).

3. Oggetti creati in /UI_Canvas/UI_TopBar:
- /UI_Canvas/UI_TopBar/WindNeedle: RectTransform con anchorMin (0.5, 0.5), anchorMax (0.5, 0.5), pivot (0.5, 0.5), anchoredPosition (-80, 0), sizeDelta (40, 40), localEulerAngles (0, 0, 0). Componente UnityEngine.UI.Image con sprite Assets/Art/UI/WindNeedle.png (lancetta che a rotazione 0 punta verso l alto / Nord, con punta rossa a Nord e base argento a Sud), preserveAspect true.
- /UI_Canvas/UI_TopBar/WindLabel: RectTransform con anchorMin (0.5, 0.5), anchorMax (0.5, 0.5), pivot (0, 0.5), anchoredPosition (-50, 0), sizeDelta (200, 40). Componente TMPro.TextMeshProUGUI con text [ph], fontSize 24, font LiberationSans SDF, horizontalAlignment Left, verticalAlignment Middle.

4. Oggetti creati in /UI_Canvas/UI_Log:
- /UI_Canvas/UI_Log/LogText: RectTransform ad aggancio completo al genitore log, anchorMin (0, 0), anchorMax (1, 1), pivot (0, 0), anchoredPosition (0, 0), sizeDelta (0, 0). Componente TMPro.TextMeshProUGUI con text [ph], fontSize 16, font LiberationSans SDF, horizontalAlignment Left, verticalAlignment Bottom (allineato in basso), wordWrapping abilitato (auto-wrapping attivo), overflowMode Overflow.

### Bezi — 2026-10-03 — commit 5013e61

Passo C, parte 3: aggiunta componenti e collegamenti (wiring) nella scena Assets/Scenes/Board.unity. Tutti i componenti e i campi elencati nella checklist esistono e sono stati collegati con successo uno alla volta senza modificare valori su prefab o asset e senza avviare il Play mode.

Elenco finale dei componenti e valore di ciascun campo:

1. /GAME_BOOTSTRAP:
- hp55games.MareIgnoto.Unity.Flow.GameBootstrap:
  - rulesConfig: Assets/Game/Content/Config/RulesConfig.asset
  - mapLayout: Assets/Game/Content/Config/MapLayout.asset
  - flow: /GAME_FLOW (componente hp55games.MareIgnoto.Unity.Flow.GameFlowController)
  - playerCount: 4
  - seed: 1

2. /GAME_FLOW:
- hp55games.MareIgnoto.Unity.Flow.GameFlowController:
  - eventPlayer: /GAME_FLOW (componente hp55games.MareIgnoto.Unity.Flow.EventPlayer)
  - botDelaySeconds: 0.3
- hp55games.MareIgnoto.Unity.Flow.EventPlayer:
  - views (array di 6 elementi ordinati):
    - [0]: /BOARD/BOARD_Tiles (componente hp55games.MareIgnoto.Unity.Views.BoardView)
    - [1]: /BOARD/BOARD_Zones (componente hp55games.MareIgnoto.Unity.Views.ZoneOverlayView)
    - [2]: /BOARD/BOARD_Ships (componente hp55games.MareIgnoto.Unity.Views.ShipsView)
    - [3]: /BOARD/BOARD_Highlights (componente hp55games.MareIgnoto.Unity.Views.CellHighlightView)
    - [4]: /UI_Canvas/UI_TopBar (componente hp55games.MareIgnoto.Unity.Views.WindRoseView)
    - [5]: /UI_Canvas/UI_Log (componente hp55games.MareIgnoto.Unity.Views.LogView)
  - secondsPerAnimation: 0.25
  - speed: 1
  - skipAnimations: false

3. /BOARD/BOARD_Tiles:
- hp55games.MareIgnoto.Unity.Views.BoardView:
  - seaTilePrefab: Assets/Game/Content/Prefabs/Tile_Sea.prefab
  - islandTilePrefab: Assets/Game/Content/Prefabs/Tile_Island.prefab
  - sacredTilePrefab: Assets/Game/Content/Prefabs/Tile_Sacred.prefab
  - borderTilePrefab: Assets/Game/Content/Prefabs/Tile_Border.prefab
  - islandMarkerPrefab: Assets/Game/Content/Prefabs/IslandMarker.prefab
  - palette: Assets/Game/Content/Config/PlayerPalette.asset
  - markerHeight: 0.1
  - markerSpread: 0.25
  - cellSize: 1

4. /BOARD/BOARD_Zones:
- hp55games.MareIgnoto.Unity.Views.ZoneOverlayView:
  - board: /BOARD/BOARD_Tiles (componente hp55games.MareIgnoto.Unity.Views.BoardView)
  - fillPrefab: Assets/Game/Content/Prefabs/ZoneFill.prefab (componente UnityEngine.MeshFilter)
  - outlinePrefab: Assets/Game/Content/Prefabs/ZoneOutline.prefab (componente UnityEngine.LineRenderer)
  - labelPrefab: Assets/Game/Content/Prefabs/ZoneLabel.prefab (componente TMPro.TextMeshPro)
  - roughSeaColor: (1.0, 0.85, 0.1, 0.35)
  - stormColor: (0.9, 0.15, 0.1, 0.45)
  - fillHeight: 0.04
  - outlineHeight: 0.05
  - labelHeight: 0.08

5. /BOARD/BOARD_Ships:
- hp55games.MareIgnoto.Unity.Views.ShipsView:
  - board: /BOARD/BOARD_Tiles (componente hp55games.MareIgnoto.Unity.Views.BoardView)
  - shipPrefab: Assets/Game/Content/Prefabs/Ship.prefab
  - palette: Assets/Game/Content/Config/PlayerPalette.asset
  - sharedCellSpread: 0.22

6. /BOARD/BOARD_Highlights:
- hp55games.MareIgnoto.Unity.Views.CellHighlightView:
  - board: /BOARD/BOARD_Tiles (componente hp55games.MareIgnoto.Unity.Views.BoardView)
  - ships: /BOARD/BOARD_Ships (componente hp55games.MareIgnoto.Unity.Views.ShipsView)
  - palette: Assets/Game/Content/Config/PlayerPalette.asset
  - routeMarkerPrefab: Assets/Game/Content/Prefabs/RouteMarker.prefab
  - height: 0.07

7. /UI_Canvas/UI_TopBar:
- hp55games.MareIgnoto.Unity.Views.WindRoseView:
  - needle: /UI_Canvas/UI_TopBar/WindNeedle (componente UnityEngine.RectTransform)
  - label: /UI_Canvas/UI_TopBar/WindLabel (componente TMPro.TextMeshProUGUI)

8. /UI_Canvas/UI_Log:
- hp55games.MareIgnoto.Unity.Views.LogView:
  - text: /UI_Canvas/UI_Log/LogText (componente TMPro.TextMeshProUGUI)
  - maxLines: 40

9. Verifica trasformazioni gerarchia /BOARD:
- /BOARD: localPosition (0, 0, 0), localRotation (0, 0, 0), localScale (1, 1, 1)
- /BOARD/BOARD_Tiles: localPosition (0, 0, 0), localRotation (0, 0, 0), localScale (1, 1, 1)
- /BOARD/BOARD_Zones: localPosition (0, 0, 0), localRotation (0, 0, 0), localScale (1, 1, 1)
- /BOARD/BOARD_Ships: localPosition (0, 0, 0), localRotation (0, 0, 0), localScale (1, 1, 1)
- /BOARD/BOARD_Highlights: localPosition (0, 0, 0), localRotation (0, 0, 0), localScale (1, 1, 1)

### Claude Code — 2026-10-03 — commit b49c877

Zone sempre visibili. Parte A in un commit a parte (`3e33a3d`, "docs: patch Athena (zone sempre visibili)"): le due sottostringhe combaciavano.

- `ZoneOverlayView` disegna sempre ogni zona. A livello 0: contorno visibile e riempimento neutro quasi trasparente, nel campo nuovo `calmColor` (default (1, 1, 1, 0.10); serve solo l'inizializzatore, niente wiring). Livello 1 giallo e livello ≥ 2 rosso come prima. Il numero del livello si attiva solo dal livello 1 (prima spariva insieme alla zona).
- La scelta della tinta e del numero è logica pura in Rules: `ZoneAppearance.TintFor(livello, config)` (`Calm`, `RoughSea`, `Storm`, con le soglie di `RulesConfig.WeatherAt`) e `ZoneAppearance.ShowsLevel(livello)`. `ZoneAppearanceTests`: livelli 0, 1, 2, 5 e soglie diverse dalla config. Verificato che il test morde (numero da livello 0: rosso). `dotnet test Tools/RulesHarness`: 410 verdi.
- Nessuna scena, prefab o asset toccati. "Lato codice" aggiornato.

**Non compilato in Unity**: compilato solo contro gli stub delle API Unity (0 errori, 0 avvisi). Da controllare in Console e in Play (Franci):
- nessun errore di compilazione in `hp55games.MareIgnoto.Unity`;
- `.meta` nuovi da committare: `Assets/Game/Rules/Map/ZoneAppearance.cs`, `Assets/Tests/EditMode/Rules/ZoneAppearanceTests.cs`;
- in Play: tutte le 20 zone hanno il contorno; le 12 nuvole a livello 0 hanno il riempimento appena visibile e nessun numero, gli 8 spicchi sono rossi col numero 5;
- se il riempimento a livello 0 non si vede o è troppo forte, si regola `calmColor` su `/BOARD/BOARD_Zones` (il materiale del prefab `ZoneFill` deve essere trasparente).
