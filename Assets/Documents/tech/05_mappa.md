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

I punti dipendono dal numero di giocatori: `MapLayout` contiene un **preset** per ogni N da 2 a 8, cioè una lista ordinata di N celle `Border`. Il giocatore del posto *k* parte dal *k*-esimo punto del preset (R-031).

| N | Punti (x, y) | Origine |
|---|---|---|
| 2 | (0,0), (19,19) | Franci |
| 3 | (0,0), (19,0), (9,19) | Franci |
| 4 | (0,0), (19,0), (0,19), (19,19) | Franci |
| 5 | i 4 angoli + (9,19) | **[DEFAULT]** estensione |
| 6 | i 4 angoli + (9,19), (9,0) | **[DEFAULT]** estensione |
| 7 | i 4 angoli + (9,19), (9,0), (0,9) | **[DEFAULT]** estensione |
| 8 | i 4 angoli + (9,19), (9,0), (0,9), (19,9) | **[DEFAULT]** estensione |

Gli angoli sono celle di cornice: una nave che parte da un angolo e punta lungo il bordo si ferma sulla cella di cornice successiva (arenata, R-066/R-069). È normale. Le mezzerie sono sulla riga/colonna 9 perché 20 è pari.

## 5. Il layout come dato: `MapLayoutAsset`

La mappa **non** è hardcoded. È un asset `MapLayoutAsset` (ScriptableObject, assembly `hp55games.MareIgnoto.Unity`) che il motore riceve convertito in un oggetto C# puro (`MapLayout`, assembly Rules).

Campi:

| Campo | Tipo | Note |
|---|---|---|
| `width`, `height` | int | 20, 20 |
| `islandCells` | lista di `(x, y, islandId)` | Celle isola, escluse quelle dell'Isola Sacra |
| `sacredIslandCells` | lista di `(x, y)` | Celle dell'Isola Sacra |
| `spawnPresets` | per ogni N in 2..8: lista ordinata di N `(x, y)` | Celle di cornice, valori di §4 |

Validazione (metodo `Validate()` sull'oggetto puro, usato anche da un pulsante/inspector Editor e dai test):
- dimensioni ≥ 5 e quadrate;
- isole e Isola Sacra solo dentro l'area navigabile, senza sovrapposizioni;
- almeno 1 cella Isola Sacra;
- per ogni N da 2 a 8, un preset di esattamente N punti, tutti su `Border` e distinti;
- ogni punto di partenza ha almeno una cella `Sea` adiacente.

## 6. Layout delle isole — DA DEFINIRE

Il prototipo del 2024 **non contiene** nessuna mappa 20×20: verificato sul repo (`develop` @ `489e865`: griglia 49×49, nessuna isola istanziata) e confermato da Franci il 02/10/2026. Posizioni delle isole (`x`, `y`, `islandId`) e celle dell'Isola Sacra vanno quindi **decise**.

Finché questa sezione non contiene un layout approvato da Franci, `MapLayout.asset` non può essere compilato (STEP 6a del piano). **Non inventare una disposizione.** Quando esiste, la sezione elenca le celle isola per `islandId`, le celle dell'Isola Sacra e un'immagine ASCII 20×20 per controllo.
