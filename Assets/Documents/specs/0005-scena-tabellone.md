# 0005 — Scena tabellone: mappa, navi, zone, vento

Stato: bozza
Commit di riferimento: da compilare
Documenti: `tech/05_mappa.md`, `tech/06_presentazione.md` §1–2, §4, §6

## Obiettivo

`Board.unity` mostra la mappa 25×25 dal `MapLayout.asset`, le navi, le zone meteo e il vento, e riproduce gli eventi della Fase 1 (rotte, meteo, movimento cella per cella, collisioni, Abbordaggio) da una partita giocata da soli bot casuali. Ancora nessuna UI di decisione.

## Contratto

Gerarchia e nomi: `06` §2 (parte `/GAME_BOOTSTRAP`, `/GAME_FLOW`, `/BOARD...`, `/CAMERA_Rig`, `/Directional Light`, `/UI_Canvas/UI_TopBar` col solo vento, `/UI_Canvas/UI_Log`).
Prefab: `Tile_Sea`, `Tile_Island`, `Tile_Sacred`, `Tile_Border`, `Ship` in `Assets/Game/Content/Prefabs/`.
`GameBootstrap` con "avvio diretto di test": tutti i posti bot casuali, seed nell'Inspector, numero giocatori nell'Inspector. `BoardView` mostra anche i segnalini isola (R-097) come dischi del colore del giocatore sopra la cella isola; `ZoneOverlayView` disegna ogni zona come forma con contorno lungo il perimetro delle sue celle e ne mostra il livello come numero quando è ≥ 1.

Il lavoro si divide in tre passi:
- **A (Bezi, in parallelo a 0002–0004)**: `MapLayout.asset` dal layout approvato in `05_mappa.md` §6; scena `Board.unity` con la gerarchia vuota del contratto; prefab placeholder delle tile e della nave (cubi/quad con materiali URP Lit colorati, `06` §6); camera e luce.
- **B (Claude Code, dopo 0004)**: `GameBootstrap`, `GameFlowController`, `EventPlayer`, `BoardView`, `ZoneOverlayView`, `ShipsView`, `CellHighlightView`, `WindRoseView`, `LogView`, `UiText` (prima versione).
- **C (Bezi)**: aggiunta dei componenti agli oggetti del contratto e wiring dei `[SerializeField]`.

## Lato codice (Claude Code)

_da compilare (passo B)_

## Lato Editor (Bezi) — checklist

Passo A (`MapLayout.asset` non è più un compito di Bezi: lo genera il comando Editor, vedi "Per Franci"):
- [ ] `Assets/Scenes/Board.unity` con la gerarchia del contratto (oggetti vuoti, nomi esatti).
- [ ] Prefab placeholder in `Assets/Game/Content/Prefabs/` e materiali in `Assets/Game/Content/Materials/`.
- [ ] Camera ortografica dall'alto, inclinata, che inquadra tutta la mappa **25×25** a 1920×1080 (centro della mappa in world (12, 0, 12) con celle da 1, `05` §1). La camera fatta nel passo A è tarata sulla 20×20 (centro (9.5, 0, 9.5), report di Bezi): va rifatta.
- [ ] Pipeline URP: in `ProjectSettings/GraphicsSettings.asset` `m_CustomRenderPipeline` non è assegnato (nota del report di Bezi), quindi i materiali URP Lit restano magenta. Nel progetto non esiste nessun asset di pipeline URP (verificato nel repo e nella storia): va creato (URP Asset con Universal Renderer, in `Assets/Game/Content/Rendering/`) e assegnato in Graphics e in tutti i livelli di Quality.

Passo C: _da compilare da Claude Code nel passo B_.

## Per Franci — modifiche isolate

- **`MapLayout.asset` (passo A, primo punto)**: nell'Editor eseguire **MareIgnoto > Create MapLayout v4**. Il comando crea `Assets/Game/Content/Config/MapLayout.asset` dal layout v4 (`LayoutV4`, `05` §3–§6); se il file esiste chiede prima di sovrascriverlo. Controllare che la Console dica `[MapLayout] … Validate OK`, poi committare `MapLayout.asset` e il suo `.meta`. Test Runner → EditMode: `MapLayoutV4AssetTests` non deve più risultare "Ignorato" ed essere verde.
- Aggiungere `Board.unity` alle Build Settings.

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
