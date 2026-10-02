# 05 — Mappa: celle, coordinate, zone, layout

## 1. Coordinate

- Griglia 20×20. Cella = `(x, y)` intera, `x` da 0 (ovest) a 19 (est), `y` da 0 (sud) a 19 (nord).
- Direzioni (enum `Heading`, ordine orario partendo da Nord): `N(0,+1)`, `NE(+1,+1)`, `E(+1,0)`, `SE(+1,-1)`, `S(0,-1)`, `SO(-1,-1)`, `O(-1,0)`, `NO(-1,+1)`. L'indice 0–7 dell'enum segue questo ordine; il risultato del d8 `r` corrisponde all'indice `r-1` (R-072).
- Rotazione oraria di `n` scatti: `(indice + n) mod 8` (R-083). Direzione opposta: `(indice + 4) mod 8` (R-061, Raffica canaglia).
- Distanza: **Chebyshev** `max(|dx|, |dy|)`. "Adiacente" = distanza 1 (8 vicini).
- In Unity la cella `(x, y)` sta sul piano XZ: `world = origin + (x * cellSize, 0, y * cellSize)`. `origin` e `cellSize` appartengono alla vista (`06_presentazione.md`), non al motore.

## 2. Tipi di cella (`CellKind`)

| Tipo | Dove | Comportamento |
|---|---|---|
| `Border` | Riga 0, riga 19, colonna 0, colonna 19 | Terra ferma. Punti di partenza. Ferma il movimento; chi ci finisce è arenato e salta la Fase 2 (R-053, R-066, R-069). Più navi possono stare sulla stessa cella. |
| `Sea` | Il resto, salvo isole | Navigabile. Appartiene a una zona meteo. |
| `Island` | Celle definite nel layout | Porto. Ferma il movimento, azioni di porto in Fase 2. Zona franca. Non appartiene a zone meteo. |
| `SacredIsland` | Celle definite nel layout (centro mappa) | Entrarci fa scattare la fine partita (R-140). |

Un'isola può occupare più celle: ogni cella isola ha un `islandId` (le celle con lo stesso id sono la stessa isola). Per le regole conta solo il tipo di cella; l'id serve alla presentazione e a eventuali missioni future.

## 3. Zone meteo

- Area navigabile: `x` e `y` da 1 a 18 (18×18).
- Zona = blocco 3×3: `zoneX = (x-1) / 3`, `zoneY = (y-1) / 3` (divisione intera), indice `zoneY * 6 + zoneX`. Totale 6×6 = 36 zone.
- Le celle `Island` e `SacredIsland` dentro un blocco non appartengono alla zona: una nave lì non subisce meteo (R-080, R-082).
- La dimensione della zona (3) sta in `RulesConfig`, non nel codice.

## 4. Punti di partenza

- Il layout definisce 8 celle `Border` come punti di partenza, in ordine (posto 1 → punto 1, R-031).
- Con meno di 8 giocatori si usano i primi N punti. **[DEFAULT]**: l'ordine dei punti nel layout va scelto in modo che i primi N siano ben distribuiti per ogni N (per esempio 1 e 2 su lati opposti).

## 5. Il layout come dato: `MapLayoutAsset`

La mappa **non** è hardcoded. È un asset `MapLayoutAsset` (ScriptableObject, assembly `hp55games.MareIgnoto.Unity`) che il motore riceve convertito in un oggetto C# puro (`MapLayout`, assembly Rules).

Campi:

| Campo | Tipo | Note |
|---|---|---|
| `width`, `height` | int | 20, 20 |
| `islandCells` | lista di `(x, y, islandId)` | Celle isola, escluse quelle dell'Isola Sacra |
| `sacredIslandCells` | lista di `(x, y)` | Celle dell'Isola Sacra |
| `spawnCells` | lista ordinata di `(x, y)` | 8 celle di cornice (§4) |

Validazione (metodo `Validate()` sull'oggetto puro, usato anche da un pulsante/inspector Editor e dai test):
- dimensioni ≥ 5 e quadrate;
- isole e Isola Sacra solo dentro l'area navigabile, senza sovrapposizioni;
- almeno 1 cella Isola Sacra;
- esattamente 8 punti di partenza, tutti su `Border`, distinti;
- ogni punto di partenza ha almeno una cella `Sea` adiacente.

## 6. Mappa esistente nel prototipo

Franci ha detto che mappa 20×20 e disposizione delle isole sono già fatte nel prototipo. Sul repo (`develop` @ `489e865`) **non risultano**: `GRID_CONTROLLER.numberOfNodes` vale 49 in `MainScene`, il prefab `Isola` non è istanziato in scena, gli spawn point degli asset `2players`/`4players` sono su una griglia 49×49. Probabilmente il lavoro è in locale e non pushato.

Procedura (spec 0000 e 0005): Bezi legge la mappa esistente nel progetto locale e riporta le coordinate delle isole nel sistema di §1; poi crea `MapLayout.asset` con quei valori. Se la mappa non si trova, Bezi si ferma e lo segnala: **non si inventa una disposizione**.
