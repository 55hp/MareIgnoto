# 0000 — Audit del progetto locale (sola lettura)

Stato: pronta per Bezi
Commit di riferimento: quello di Bezi al momento del lavoro (da dichiarare)
Documenti: `tech/05_mappa.md`, `tech/06_presentazione.md`

## Obiettivo

Fotografare lo stato reale del progetto **locale** prima della pulizia della Fase 0, in particolare trovare la mappa 20×20 con le isole che Franci indica come già fatta e che sul repo non risulta. **Diagnosi: non modificare nulla.**

## Contratto — cosa riportare

1. Scene presenti in `Assets/` (percorso) e quale è aperta.
2. In ogni scena: c'è una griglia di gioco? Con quante celle per lato? Valore di `numberOfNodes` su `GRID_CONTROLLER` (se esiste).
3. Isole: per ogni isola presente in scena (istanze del prefab `Isola` o altri oggetti che rappresentano isole), percorso completo e posizione world. Se la griglia esiste, converti la posizione nella cella `(x, y)` secondo `tech/05_mappa.md` §1 (x 0..19 ovest→est, y 0..19 sud→nord) e indica come hai fatto la conversione (origine, passo). Se la conversione è un'inferenza, dillo.
4. Se esiste un'Isola Sacra/centrale: celle occupate.
5. Punti di partenza o spawn esistenti: celle.
6. Qualunque altro oggetto o asset che sembri far parte della "mappa nel prototipo" (ProBuilder, mesh `Isola_1..3`, terreni).
7. Se la mappa 20×20 **non** si trova: dirlo chiaramente e fermarsi lì.

## Verifica

Il report basta a compilare `MapLayout.asset` (spec 0005) senza aprire di nuovo le scene.

## Report

### Bezi — 2026-10-02 — commit 489e865

1. Scene presenti in Assets/
- Assets/Scenes/MainScene.unity (scena aperta e caricata nell'Editor).
- Assets/WorldMaterialsFree/URP_FreeDemoScene.unity.
- Assets/WorldMaterialsFree/HDRP_FreeDemoScene.unity.
- Assets/WorldMaterialsFree/FreeDemoScen_WMShader.unity.

2. Griglia di gioco e numberOfNodes
- In Assets/Scenes/MainScene.unity: e' presente il GameObject /--- CONTROLLERS ---/GRID_CONTROLLER con componente GridController. Il valore del campo numberOfNodes e' 49. I campi topLeftCorner e bottomRightCorner puntano rispettivamente a /--- MAPPA ---/TopLeftCorner (posizione locale -25, 2, 25) e a /--- MAPPA ---/BotRightCorner (posizione locale 25, 2, -25). La griglia e' quindi 49x49 nodi, non 20x20.
- Nelle scene di demo del package WorldMaterialsFree non e' presente alcuna griglia ne' componente GridController.

3. Isole presenti in scena
- In Assets/Scenes/MainScene.unity non e' presente alcuna istanza del prefab Isola (Assets/GameobjectsInScena/Isola.prefab) ne' alcun altro GameObject che rappresenti isole.
- Oggetti presenti sotto /--- MAPPA ---:
  - /--- MAPPA ---/Mare conosciuto: Transform localPosition (0, 0, 0), localScale (50, 1, 50), MeshFilter, MeshRenderer con materiale Assets/WorldMaterialsFree/Built-In_RPMaterials/Ocean.mat, BoxCollider con size (1, 1, 1). Rappresenta il piano del mare.
  - /--- MAPPA ---/Correnti forti: Transform localPosition (0, 0.2, 0), localScale (20, 0.5, 20), MeshFilter, MeshRenderer con materiale Assets/WorldMaterialsFree/Built-In_RPMaterials/Ocean 2.mat, CapsuleCollider con radius 0.5, height 2.
  - /MeshGenerator: Transform localPosition (-5.287385, -0.431102425, 3.7317996), componente MeshGenerator (meshName "Piramide", verticiDellaBase 6, altezza 2, material_placeholder Assets/WorldMaterialsFree/Built-In_RPMaterials/Islands.mat).
- Nessuna isola e' posizionata in scena; nessuna conversione in coordinate (x, y) e' possibile.

4. Isola Sacra / centrale
- Non esiste alcun GameObject o prefab di Isola Sacra o isola centrale nella scena ne' tra gli asset di progetto.

5. Punti di partenza o spawn esistenti
- Nella scena Assets/Scenes/MainScene.unity non ci sono nodi o punti di spawn istanziati nella gerarchia (vengono generati a runtime da GridController.InitGrid e SpawnPlayers).
- Il componente GameManager su /--- CONTROLLERS ---/GAME_MANAGER fa riferimento all'asset Assets/4players.asset.
- Asset Assets/2players.asset (tipo StartingGameSetupSO): player_amount 2, spawnPoints in coordinate (0, 0) e (48, 48), corrispondenti agli estremi della griglia 49x49.
- Asset Assets/4players.asset (tipo StartingGameSetupSO): playerNumber 4, spawnPoints in coordinate (1, 0), (2, 0), (3, 0), (4, 0).
- Prefab Assets/GameobjectsInScena/SpawnerNode.prefab: Transform localPosition (-25, 0, 25), localScale (0.3, 0.7, 0.3), componenti MeshFilter, MeshRenderer, SphereCollider (isTrigger true), MapNode (name "SpawnPoint", free true). Non e' istanziato in scena.

6. Altri asset collegati alla mappa nel prototipo
- Prefab Assets/GameobjectsInScena/Isola.prefab: radice Isola (Transform localPosition -0.289958, 0, -13.22, localScale 1, 1, 1) con tre figli varianti: Isola Bassa (MeshFilter Isola_3.asset), Isola Media (MeshFilter Isola_2.asset), Isola Alta (MeshFilter Isola_1.asset).
- Mesh asset procedurali in Assets/: Isola_1.asset, Isola_2.asset, Isola_3.asset, generati da IslandGenerator.cs.
- ProBuilder: presenti solo gli asset di default in Assets/ProBuilder Data/ (Default Color Palette.asset, Default Material Palette.asset), nessun oggetto o mesh ProBuilder presente in scena o nel progetto.
- Terreni: nessun Terrain GameObject o TerrainData asset presente nel progetto.

7. Esito audit mappa 20x20
- La mappa 20x20 con le isole non e' presente nel progetto locale. E' presente unicamente il prototipo legacy 2024 con griglia 49x49 (numberOfNodes = 49) priva di isole posizionate. Come da contratto di Bezi_Rules.md e tech/05_mappa.md paragrafo 6, la disposizione non viene inventata e il lavoro di audit termina qui.
