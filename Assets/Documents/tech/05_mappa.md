# 05 — Mappa: celle, coordinate, zone, layout

## 1. Coordinate

- Griglia 25×25. Le **colonne** si indicano con le lettere A–Y (A = 0 ovest … Y = 24 est), le **righe** con i numeri 0–24 (0 sud … 24 nord). Una cella è "lettera + riga": B1, M0, Y12. Nel codice la cella è `(x, y)` intera con `x` = indice della lettera (A = 0) e `y` = riga. Le lettere servono a documentazione, UI e log: nel motore si usano gli indici.
- Direzioni (enum `Heading`, ordine orario partendo da Nord): `N(0,+1)`, `NE(+1,+1)`, `E(+1,0)`, `SE(+1,-1)`, `S(0,-1)`, `SO(-1,-1)`, `O(-1,0)`, `NO(-1,+1)`. L'indice 0–7 dell'enum segue questo ordine; il risultato del d8 `r` corrisponde all'indice `r-1` (R-072).
- Rotazione oraria di `n` scatti: `(indice + n) mod 8` (R-083). Direzione opposta: `(indice + 4) mod 8` (R-061, Raffica canaglia).
- Distanza: **Chebyshev** `max(|dx|, |dy|)`. "Adiacente" = distanza 1 (8 vicini).
- In Unity la cella `(x, y)` sta sul piano XZ: `world = origin + (x * cellSize, 0, y * cellSize)`. `origin` e `cellSize` appartengono alla vista (`06_presentazione.md`), non al motore.

## 2. Tipi di cella (`CellKind`)

| Tipo | Dove | Comportamento |
|---|---|---|
| `Border` | Riga 0, riga 24, colonna A, colonna Y | Terra ferma. Ospita i punti di partenza dei lati. Ferma il movimento; chi ci finisce è arenato e salta la Fase 2 (R-053, R-066, R-069). Più navi possono stare sulla stessa cella. |
| `Sea` | Il resto, salvo isole | Navigabile. Può appartenere a una zona meteo (§3) oppure essere **mare libero**, senza zona e senza meteo. |
| `Island` | Celle definite nel layout | Porto. Ferma il movimento, azioni di porto in Fase 2. Zona franca. Non appartiene a zone meteo. Vale la regola del segnalino isola (R-097). |
| `SacredIsland` | Celle definite nel layout (centro mappa) | Entrarci fa scattare la fine partita (R-140). |

Un'isola può occupare più celle: ogni cella isola ha un `islandId` (le celle con lo stesso id sono la stessa isola). Per le regole conta solo il tipo di cella e, per R-097, l'`islandId`.

## 3. Zone meteo

Una **zona** è un gruppo fisso di celle di mare, definito nel layout. Ci sono due tipi:

- **Nuvola**: forma curva e irregolare, spessore massimo 2–3 celle, partenza a livello 0.
- **Spicchio**: parte dell'anello di tempesta che circonda l'Isola Sacra, partenza a **livello 5**, scritto nel layout (campo `livelloIniziale` di ogni zona).

Ogni zona ha un **livello** intero ≥ 0 (R-081): 0 Normale, 1 Mare Mosso, ≥ 2 Tempesta. Le celle di mare che non stanno in nessuna zona sono mare libero.

**Regole di costruzione** (`Validate()` le controlla tutte):

- ogni zona è fatta di celle di mare (non isole, non cornice) ed è connessa per lati;
- due nuvole stanno a distanza Chebyshev ≥ 2 (almeno 1 cella libera tra loro);
- una nuvola sta a distanza ≥ 3 da ogni spicchio e ≥ 2 da ogni punto di partenza; può toccare un'isola;
- gli spicchi sono 8, in ordine R1…R8 attorno all'isola. Due spicchi **consecutivi** si toccano per **uno spigolo solo** e per nessun lato: il punto di contatto è un **varco**. Due spicchi non consecutivi stanno a distanza ≥ 2;
- un varco è un blocco 2×2 con due celle di tempesta in diagonale (uno spicchio per parte) e le altre due celle **di mare libero**: sono "non adiacenti" e permettono un solo passo diagonale tra loro;
- uno spicchio sta a distanza ≥ 2 da ogni cella dell'Isola Sacra (1 cella libera) e ≥ 3 da ogni altra isola.

### Elenco delle zone

| Id | Tipo | Livello iniziale | Celle | Elenco |
|---|---|---|---|---|
| R1 | spicchio | 5 | 7 | P12, P13, P14, Q13, Q14, R14, R15 |
| R2 | spicchio | 5 | 5 | N16, O15, O16, O17, P17 |
| R3 | spicchio | 5 | 7 | J17, K15, K16, K17, L15, L16, M15 |
| R4 | spicchio | 5 | 5 | H14, H15, I13, I14, J14 |
| R5 | spicchio | 5 | 7 | H10, H9, I10, I11, J10, J11, J12 |
| R6 | spicchio | 5 | 5 | J7, K7, K8, K9, L8 |
| R7 | spicchio | 5 | 7 | M9, N8, N9, O7, O8, O9, P7 |
| R8 | spicchio | 5 | 5 | P10, Q10, Q11, R10, R9 |
| N1 | nuvola | 0 | 12 | U10, U11, U12, U13, U14, U15, U9, V10, V11, V12, V13, V14 |
| N2 | nuvola | 0 | 9 | U17, U18, V17, V18, W16, W17, W18, X15, X16 |
| N3 | nuvola | 0 | 9 | P23, Q22, Q23, R20, R21, R22, S20, S21, S22 |
| N4 | nuvola | 0 | 12 | J20, K20, K21, L20, L21, M20, M21, N20, N21, O20, O21, P20 |
| N5 | nuvola | 0 | 9 | G20, G21, G22, H20, H21, H22, I22, I23, J23 |
| N6 | nuvola | 0 | 9 | B15, B16, C16, C17, C18, D17, D18, E17, E18 |
| N7 | nuvola | 0 | 12 | D10, D11, D12, D13, D14, E10, E11, E12, E13, E14, E15, E9 |
| N8 | nuvola | 0 | 9 | B8, B9, C6, C7, C8, D6, D7, E6, E7 |
| N9 | nuvola | 0 | 9 | G2, G3, G4, H2, H3, H4, I1, I2, J1 |
| N10 | nuvola | 0 | 12 | J4, K3, K4, L3, L4, M3, M4, N3, N4, O3, O4, P4 |
| N11 | nuvola | 0 | 9 | P1, Q1, Q2, R2, R3, R4, S2, S3, S4 |
| N12 | nuvola | 0 | 9 | U6, U7, V6, V7, W6, W7, W8, X8, X9 |

### Varchi (celle di mare libero in diagonale)

| Tra | Celle di tempesta in contatto | Celle libere |
|---|---|---|
| R1–R2 | P14, O15 | P15, O14 |
| R1–R8 | P12, Q11 | P11, Q12 |
| R2–R3 | N16, M15 | N15, M16 |
| R3–R4 | K15, J14 | K14, J15 |
| R4–R5 | I13, J12 | I12, J13 |
| R5–R6 | J10, K9 | J9, K10 |
| R6–R7 | L8, M9 | L9, M8 |
| R7–R8 | O9, P10 | O10, P9 |

## 4. Punti di partenza

Gli **angoli** partono in **mare** (primo anello navigabile): B1, X1, X23, B23. I **lati** partono sulla **cornice**: M0 (basso), Y12 (destra), M24 (alto), A12 (sinistra). `MapLayout` contiene un **preset** per ogni N da 2 a 8, cioè una lista ordinata di N celle. Il giocatore del posto *k* parte dal *k*-esimo punto del preset (R-031).

| N | Punti | Origine |
|---|---|---|
| 2 | B1, X23 | Franci |
| 3 | B1, X1, M24 | Franci |
| 4 | B1, X1, B23, X23 | Franci |
| 5 | B1, X1, B23, X23, M24 | **[DEFAULT]** estensione |
| 6 | B1, X1, B23, X23, M24, M0 | **[DEFAULT]** estensione |
| 7 | B1, X1, B23, X23, M24, M0, A12 | **[DEFAULT]** estensione |
| 8 | B1, X1, B23, X23, M24, M0, A12, Y12 | **[DEFAULT]** estensione |

Proprietà verificate sul layout v4 (valgono per ogni N da 2 a 8): ogni punto di partenza dista **11** celle dall'Isola Sacra e ha le due isole più vicine a **4 e 4** celle. Gli angoli hanno 3 rotte che portano in mare (N, NE, E per B1); i lati ne hanno 3.

## 5. Il layout come dato: `MapLayoutAsset`

La mappa **non** è hardcoded. È un asset `MapLayoutAsset` (ScriptableObject, assembly `hp55games.MareIgnoto.Unity`) che il motore riceve convertito in un oggetto C# puro (`MapLayout`, assembly Rules).

Campi:

| Campo | Tipo | Note |
|---|---|---|
| `width`, `height` | int | 25, 25 |
| `islandCells` | lista di `(x, y, islandId)` | Celle isola, escluse quelle dell'Isola Sacra |
| `sacredIslandCells` | lista di `(x, y)` | Celle dell'Isola Sacra |
| `zones` | lista di `(id, tipo, livelloIniziale, celle)` | Nuvole e spicchi, come in §3 |
| `spawnPresets` | per ogni N in 2..8: lista ordinata di N `(x, y)` | Celle di §4 (angoli di mare, lati di cornice) |

Validazione (`Validate()` sull'oggetto puro, usato anche da un pulsante/inspector Editor e dai test):

- dimensioni ≥ 5 e quadrate;
- isole e Isola Sacra solo dentro l'area navigabile, senza sovrapposizioni; almeno 1 cella Isola Sacra;
- tutte le regole di costruzione delle zone di §3; nessuna cella in due zone;
- per ogni N da 2 a 8, un preset di esattamente N punti distinti, ciascuno su `Border` oppure su `Sea` non appartenente a una zona e non adiacente a una cella isola.

## 6. Layout approvato (v4, 25×25)

Approvato da Franci il 03/10/2026. Simmetria: isole, nuvole, Isola Sacra e punti di partenza sono invarianti per rotazione di 90° e per specchio; l'anello ha solo la rotazione di 90° (i varchi sugli assi sono "a girandola").

`#` cornice, `.` mare libero, `1`–`8` spicchi R1–R8, `a`–`l` nuvole N1–N12, `I` isola, `S` Isola Sacra, `o` punto di partenza.

```
   ABCDEFGHIJKLMNOPQRSTUVWXY
24 ############o############
23 #o......ee.....cc......o#
22 #....Ieee.......cccI....#
21 #.....eeI.ddddd.Icc.....#
20 #.....ee.ddddddd.cc.....#
19 #.I...................I.#
18 #.fff...............bbb.#
17 #.fff....33...22....bbb.#
16 #ffI......33.22......Ibb#
15 #f..g..4..333.2..1..a..b#
14 #..gg..444.....111..aa..#
13 #..gg...4...S..11...aa..#
12 o..gg....5.SSS.1....aa..o
11 #..gg...55..S...8...aa..#
10 #..gg..555.....888..aa..#
 9 #h..g..5..6.777..8..a..l#
 8 #hhI......66.77......Ill#
 7 #.hhh....66...77....lll.#
 6 #.hhh...............lll.#
 5 #.I...................I.#
 4 #.....ii.jjjjjjj.kk.....#
 3 #.....iiI.jjjjj.Ikk.....#
 2 #....Iiii.......kkkI....#
 1 #o......ii.....kk......o#
 0 ############o############
```

- **Isola Sacra** (croce, 5 celle): M12, L12, N12, M11, M13.
- **Isole** (16, `islandId` = numero): 1=C5, 2=C19, 3=D8, 4=D16, 5=F2, 6=F22, 7=I3, 8=I21, 9=Q3, 10=Q21, 11=T2, 12=T22, 13=V8, 14=V16, 15=W5, 16=W19.
- **Distanze**: dall'Isola Sacra 11 per tutti i punti di partenza; due isole più vicine a 4 e 4.
- **Rotte sicure** (senza celle di tempesta, 11 passi): angoli 5 su 23 rotte minime; lati 4 su 25.653. Verso le due isole più vicine: angoli 12 su 12, lati 3 su 3.
- **Copertura**: 168 celle di zona (48 di anello, 120 di nuvole) su 508 celle di mare, il 33%.
- **Scostamenti noti**: lo spessore massimo dell'anello è 3 (non 4–5); le nuvole possono toccare le isole.
