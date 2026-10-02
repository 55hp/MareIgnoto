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
