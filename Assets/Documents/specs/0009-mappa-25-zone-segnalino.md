# 0009 — Mappa 25×25, zone meteo a livelli, segnalino isola

Stato: pronta per Claude Code
Commit di riferimento: `c20ab63`
Regole coinvolte: R-002, R-031, R-038, R-045, R-053, R-058, R-069, R-073a, R-080–R-088, R-091, R-097; `03` (Invocazione, Ira, Favore, Gamba di legno)
Documenti: `tech/05_mappa.md` (nuova versione, §A1), `tech/02_regole.md`, `tech/03_contenuti.md`

Un file per task. Sopra `## Report` scrivono Athena (intestazione, Obiettivo, Parte A) e Claude Code (Parte B, Per Franci). Sotto `## Report` ogni attore aggiunge voci in coda.

## Obiettivo

Portare nel progetto la mappa 25×25 approvata (layout v4), le zone meteo a livelli (nuvole e anello di 8 spicchi attorno all'Isola Sacra) e la nuova regola del segnalino isola. Prima si aggiornano i documenti (Parte A, testo esatto di Athena), poi il motore (Parte B).

## Parte A — Patch di documentazione (testo esatto)

Regole di applicazione (vedi `CLAUDE.md`, "Patch di documentazione da Athena"): applica parola per parola, senza riformulare. Se un file, una riga o una sottostringa non esiste o è diversa da come la descrivo, fermati e riportalo. Un commit a parte "docs: patch Athena (mappa 25x25, zone, R-097)", solo documenti. Mostra `git diff --stat`.

### A1. `Assets/Documents/tech/05_mappa.md`

Sostituisci l'INTERO contenuto del file con il testo tra i marcatori (marcatori esclusi).

<<<INIZIO 05_mappa.md>>>
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
- **Spicchio**: parte dell'anello di tempesta che circonda l'Isola Sacra, partenza a **livello 5** (`ringInitialLevel` in `RulesConfig`).

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
<<<FINE 05_mappa.md>>>

### A2. `tech/02_regole.md`

Sostituisci ogni riga di tabella la cui prima cella è l'ID indicato con esattamente questa riga:

| R-002 | Mappa: griglia quadrata 25×25. Prima/ultima riga e prima/ultima colonna sono **cornice** (terra ferma). Area navigabile 23×23. Le colonne si indicano con lettere (A–Y), le righe con numeri (0–24): B1, M0, Y12. Dettaglio celle, zone e coordinate: `05_mappa.md`. |

| R-031 | Ogni nave parte da un punto di partenza: gli angoli sono celle di mare (B1, X1, X23, B23), i lati sono celle di cornice (M0, Y12, M24, A12). I punti dipendono dal numero di giocatori (preset in `MapLayout`, `05_mappa.md` §4); il giocatore del posto *k* prende il *k*-esimo punto del preset. |

| R-038 | Vento iniziale: si tira 1d8 (evento di dado) e la lancetta si porta a *r* scatti in senso orario da Nord (8 = Nord). Tutte le zone meteo partono a livello 0, tranne gli spicchi dell'anello centrale, che partono a livello 5 (R-081). |

| R-053 | Nave sulla **cornice** o su un'isola con il proprio segnalino (R-097): il turno del giocatore termina subito. |

| R-069 | Una nave che finisce sulla cornice, o su un'isola con il proprio segnalino (R-097), si è **arenata** (serve alla missione "Gamba di legno"). |

| R-080 | Il meteo è una proprietà delle **zone**: gruppi fissi di celle di mare definiti nel layout (`05_mappa.md` §3), cioè le nuvole (N1–N12) e gli spicchi dell'anello centrale (R1–R8). Le celle di mare che non appartengono a nessuna zona sono mare libero: non hanno meteo e non cambiano mai. Le celle isola non appartengono a nessuna zona. |

| R-081 | Ogni zona ha un **livello** intero ≥ 0: livello 0 = **Normale**, livello 1 = **Mare Mosso**, livello ≥ 2 = **Tempesta** (l'effetto è sempre quello di Tempesta, anche a livello 5). Nessun decadimento automatico: una zona cambia livello solo per una carta Meteo. Gli spicchi dell'anello partono a livello 5, le nuvole a 0. |

| R-082 | Il meteo si applica nella Fase 1 (R-042), alla zona in cui la nave si trova prima di muoversi. Navi su isole, cornice o mare libero non subiscono il meteo. |

| R-085 | **Navigatore** (rango 6) sopra coperta: −1 livello percepito per copia: per decidere l'effetto si usa il livello della zona meno 1 per ogni copia (minimo 0). Una zona a livello 2 vale quindi Mare Mosso, una a livello 5 resta Tempesta. |

Inserisci come riga NUOVA subito dopo la riga R-087 (stessa tabella):

| R-088 | Effetto delle carte Meteo sul livello (`03` §2.2): Invocazione porta la zona a livello 1 solo se è a livello 0; Ira la porta a livello 2 solo se è a livello 0 o 1; negli altri casi la carta è sprecata (si paga comunque). Favore abbassa il livello di 1 (minimo 0). Supplica, Raffica e Vento in Poppa non agiscono sui livelli. |

Inserisci come riga NUOVA subito dopo la riga R-096:

| R-097 | **Segnalino isola**: ogni giocatore ha un solo segnalino. Quando compie un'azione di porto (R-054, anche tramite Vedetta, R-058) lo mette sull'isola in cui si trova, togliendolo da quella dove era. Se la nave **arriva** su un'isola con il proprio segnalino (per movimento o per riposizionamento di un Abbordaggio, R-073a), quell'isola vale per lei come terra ferma, come la cornice: il turno termina subito (R-053) e si è arenata (R-069). Una nave che **resta** sull'isola (velocità 0, Svago) non arriva: può agire. Se lascia l'isola e vi ritorna, arriva di nuovo: non può agire, quindi non può usare nemmeno lo Svago. Se arriva e non agisce, non mette il segnalino. Il segnalino non limita le altre navi. |

### A3. `tech/03_contenuti.md`

Sostituisci ogni riga di tabella la cui prima cella è l'Id indicato con esattamente questa riga:

| `InvocazioneGartya` | Invocazione di Gartya | 5 | 6 | Porta la zona scelta a livello 1, solo se è a livello 0 (R-088) |

| `IraGartya` | Ira di Gartya | 10 | 3 | Porta la zona scelta a livello 2, solo se è a livello 0 o 1 (R-088) |

| `FavoreGartya` | Favore di Gartya | 2 | 5 | Abbassa di 1 il livello della zona scelta, minimo 0 (R-088) |

| `GambaDiLegno` | Gamba di legno | Si è arenato 3 volte su 3 celle diverse, di cornice o di isola con il proprio segnalino (R-069, R-097) | 3 | Immediata | insieme delle celle di arenamento |

### A4. Sostituzioni esatte di sottostringa (ognuna deve combaciare una sola volta)

- `tech/01_architettura.md`: "dimensione zona, `maxRounds`" → "livello iniziale degli spicchi (`ringInitialLevel`), `maxRounds`"
- `tech/06_presentazione.md`: "ZoneOverlayView (colore per stato meteo, una quad per zona)" → "ZoneOverlayView (colore per livello meteo e linea di contorno lungo il perimetro delle celle, una forma per zona)"
- `specs/0005-scena-tabellone.md`: "mostra la mappa 20×20 dal" → "mostra la mappa 25×25 dal"
- `Bezi_Rules.md`: "Coordinates: `x` 0..19 west→east, `y` 0..19 south→north (`tech/05_mappa.md` §1)." → "Coordinates: columns are letters A..Y (A = 0, west→east), rows are numbers 0..24 (south→north); cell B1 = column B, row 1 (`tech/05_mappa.md` §1)."
- `README.md` (in `Assets/Documents`): "- Mappa 20×20 con isole: non esiste, il layout va deciso (`tech/05_mappa.md` §6). Blocca la creazione di `MapLayout.asset`." → "- Mappa 25×25: layout v4 approvato il 03/10/2026 (`tech/05_mappa.md` §6). Il motore va ancora allineato; `MapLayout.asset` si crea nella spec 0005, passo A."

### A5. `tech/00_INDICE.md`

Aggiungi in fondo alla tabella "Registro modifiche":

| 03/10/2026 | **Mappa 25×25** con layout v4 approvato (`05` §6). **Zone** = nuvole e spicchi dell'anello centrale, con livelli (R-080, R-081, R-085, R-088); l'anello parte a livello 5. **R-002/R-031/R-038**: nuova mappa, spawn (angoli in mare, lati su cornice), livelli iniziali. **R-097**: segnalino isola; **R-053/R-069** aggiornate. **`03`**: Invocazione, Ira, Favore, Gamba di legno | `02`, `03`, `05` |

## Parte B — Lato codice (Claude Code)

Leggi prima `tech/05_mappa.md` e le righe cambiate di `02` e `03`.

1. **MapLayout** (R-002, `05` §1–5): griglia 25×25; nuovo campo `zones` (id, tipo, livello iniziale, celle); `spawnPresets` come in §4 (le celle degli angoli sono Sea, quelle dei lati sono Border); `Validate()` come in §5. Togli il calcolo delle zone 3×3 e qualunque impostazione della dimensione della zona; aggiungi `RulesConfig.ringInitialLevel` = 5. Le lettere delle colonne servono solo per visualizzazione e log. Metti il layout v4 in UN solo posto come dato di test (costruito dagli elenchi di `05` §3 e §6, senza duplicati) e verifica che superi `Validate()` e che valgano le proprietà di §6: ogni punto di partenza a distanza Chebyshev 11 dall'Isola Sacra e con le due isole più vicine a 4 e 4; simmetria di rotazione di isole, nuvole, Isola Sacra e punti di partenza.
2. **Zone e livelli** (R-080–R-088): lo stato di una zona è un livello intero; spicchi a 5, nuvole a 0; livello 0 Normale, 1 Mare Mosso, ≥ 2 Tempesta; una nave su isola, cornice o mare libero non subisce meteo; Navigatore come R-085; effetti delle carte Meteo come R-088 (una carta sprecata si paga comunque); Favore si ferma a 0; eventi di cambio livello; le carte Meteo bersagliano una zona per id. Aggiorna i test del meteo che assumevano i blocchi 3×3.
3. **Setup** (R-031): le navi partono dalle celle del preset del numero di giocatori. Gli angoli sono in mare, quindi vento e meteo valgono per loro dal round 1; i lati sono sulla cornice e non subiscono né l'uno né l'altro.
4. **Segnalino isola** (R-097, R-053, R-069, R-091, R-045, R-073a, R-058): un segnalino per giocatore, messo quando il giocatore compie un'azione di porto (anche con la Vedetta), togliendo il precedente. Arrivare su un'isola con il proprio segnalino (per movimento o per riposizionamento da Abbordaggio) vale come la cornice per quel giocatore: il turno di Fase 2 termina e conta come arenarsi (Gamba di legno conta celle distinte, di cornice o di queste isole). Restare sull'isola (velocità 0, Svago) non è un arrivo; lasciarla e tornarci sì, e allora non si può usare nemmeno lo Svago. La decisione di porto deve offrire l'opzione "nessuna azione", che non mette il segnalino. Il segnalino non riguarda gli altri giocatori.
5. **Test e gate**: aggiorna i test che assumevano 20×20, zone 3×3 o altri spawn, e aggiungi test per i punti 1–4. Rilancia il gate da 1000 seed (2/4/8 giocatori) sul layout v4 e incolla il riepilogo: partite fermate da `maxRounds` con i seed, round minimo/mediano/massimo e cosa è cambiato rispetto ai numeri sulla 20×20.

Se una regola non è chiara, non inventare: lascia un TODO con l'id della regola e scrivi la domanda sotto "#### Domande".

### Fatto (Claude Code)

Commit `c20ab63`. `dotnet test Tools/RulesHarness`: **393 verdi**, più 1 `[Explicit]` (il gate).

1. **MapLayout**: 25×25 di default; nuovo campo `zones` (`LayoutZone`: id, `ZoneKind` Cloud/RingSlice, livello iniziale, celle); `spawnPresets` senza default nel codice (il layout v4 si inserisce come dato). `Validate()` controlla tutto `05` §3 e §5, con codici nuovi (`ZoneCellNotSea`, `ZoneOverlap`, `ZoneNotConnected`, `ZoneIdInvalid`, `ZoneInitialLevel`, `ZoneSpacing`, `RingSliceCount`, `RingContact`, `RingGap`, `SpawnNotAllowed` al posto di `SpawnNotOnBorder`). Le distanze di costruzione sono in `RulesConfig` (`cloudMinSpacing`, `cloudRingMinDistance`, `cloudSpawnMinDistance`, `ringSliceCount`, `ringNonConsecutiveMinDistance`, `ringSacredMinDistance`, `ringIslandMinDistance`). Tolto `zoneSize` e il calcolo dei blocchi 3×3; aggiunto `ringInitialLevel = 5`. `Coord.Name`/`Coord.Parse` per i nomi di cella ("B1"), solo per log, UI e test. Il layout v4 sta in un solo posto, `Tests/EditMode/Rules/LayoutV4.cs`, generato dagli elenchi di `05` §3 e §6 (prima ho verificato che tabella e disegno ASCII di `05` coincidano cella per cella).
2. **Zone e livelli**: `GameState.ZoneLevels` (interi) e `IReadOnlyGameState.ZoneLevels`; `RulesConfig.WeatherAt(level)` dà l'effetto (0 Normale, 1 Mare Mosso, ≥ 2 Tempesta; soglie `roughSeaLevel`, `stormLevel`). Spicchi a 5, nuvole a 0, mare libero senza meteo. Navigatore: livello − 1 per copia, minimo 0 (R-085). Carte Meteo per id di zona (`ZoneOption.ZoneId`) con R-088 (`invocationLevel`, `wrathLevel`, `favorLevelDrop`); `ZoneChangedEvent` ha id, livello di partenza e di arrivo e `Changed` (falso per una carta sprecata, pagata comunque). `ZoneSetup` del tutorial è ora (id, livello).
3. **Setup**: invariato nel flusso, i preset sono quelli di `05` §4. Gli angoli sono in mare: il vento vale dal round 1 (test `CornersStartAtSeaWithWindAndSidesOnTheBorderWithout_R031_R062`); il meteo no, perché gli angoli sono fuori dalle zone (lo impone `Validate()`).
4. **Segnalino isola**: `IslandMarker` e `ArrivedOnOwnIsland` per giocatore (pubblici). Un'azione di porto mette il segnalino (`IslandMarkerPlacedEvent`), l'opzione nuova `PortAction.None` no. Arrivare (movimento o riposizionamento da Abbordaggio) sull'isola del proprio segnalino: `ShipStrandedEvent` (conta per Gamba di legno) e in Fase 2 `TurnSkippedEvent` con motivo `OwnIslandMarker`, senza Mozzo. Restare (Svago, velocità 0) non è arrivare. Con la Vedetta il segnalino va sull'isola dove si attracca; in Svago sull'isola del segnalino già messo.
5. **Test**: aggiornati tutti quelli che usavano la 20×20, le zone 3×3 o i vecchi spawn (posizioni portate sul layout v4); nuovi `MapTests` (Validate su ogni regola, proprietà di `05` §6: distanze 11 e 4/4, simmetrie, conteggi 168/48/120/508), `WeatherTests` (livelli, anello a 5, Navigatore), `IslandMarkerTests`, `SpawnTests`. Verificato che mordono: arrivo sull'isola del segnalino ignorato (3 rossi), segnalino messo anche con "nessuna azione" (1), Ira che porta sempre a 2 (1), Navigatore ignorato (3), varco non controllato (1).

## Lato Editor (Bezi) — checklist

Nessuna in questo task. `MapLayout.asset` si crea nella spec 0005, passo A, dai dati di `05` §3–§6.

## Per Franci — modifiche isolate

- Dopo il task: aprire l'Editor per i `.meta` dei file nuovi, controllare la Console e lanciare il Test Runner EditMode.
  - File nuovi: `Assets/Tests/EditMode/Rules/LayoutV4.cs`, `Assets/Tests/EditMode/Rules/IslandMarkerTests.cs`.
  - `RulesConfig.asset`: `zoneSize` non esiste più (Unity lo ignora) e i campi nuovi prendono i default al caricamento. Facoltativo: aprirlo e salvarlo, così i valori nuovi finiscono anche nel file.
  - `AssetConversionTests` (Unity) è cambiato: un `MapLayoutAsset` nuovo è 25×25 e vuoto, quindi non valido finché non si inserisce il layout v4 (spec 0005, passo A).

## Verifica

- Parte A: `git diff --stat` mostra solo documenti.
- Parte B: `dotnet test Tools/RulesHarness` verde; gate da 1000 seed senza eccezioni né violazioni di invarianti; il layout v4 supera `Validate()`.

## Report

<!-- Voci in coda. Intestazione: ### Claude Code — AAAA-MM-GG — commit abc1234 -->

### Claude Code — 2026-10-03 — commit c20ab63

Parte A applicata parola per parola in un commit a parte (`9f74e30`, "docs: patch Athena (mappa 25x25, zone, R-097)"): tutte le righe e sottostringhe combaciavano; R-088 e R-097 inserite dopo R-087 e R-096. Nello stesso commit ho aggiunto al repo questa spec, che era non tracciata (con il `.meta` generato da Unity). Parte B: dettaglio in "Fatto (Claude Code)" sopra.

**Gate** (`Simulation1000Seeds`, seed 1–1000 × 2/4/8, `maxRounds` 500, layout v4, 1 min 22 s): **nessuna eccezione, nessuna violazione di invarianti**.

```
Simulazione: seed 1-1000, maxRounds 500, bot casuale (seed del bot = seed della partita)

2 giocatori: 1000 partite, 842 finite con l'Isola Sacra, 158 interrotte da maxRounds, 0 errori
  round (partite finite): min 15, media 198.79, mediana 174, max 498
  interrotte da maxRounds, seed: 10, 11, 17, 24, 32, 56, 58, 79, 82, 83, 85, 91, 93, 96, 116, 117, 118, 120, 133, 151, 152, 161, 165, 166, 177, 197, 209, 210, 211, 215, 216, 224, 227, 228, 229, 230, 245, 248, 249, 250, 251, 255, 279, 287, 291, 297, 303, 304, 328, 330, 339, 340, 341, 343, 344, 348, 349, 354, 356, 368, 374, 383, 399, 407, 414, 418, 425, 427, 433, 445, 454, 461, 468, 482, 491, 492, 501, 504, 510, 512, 516, 520, 532, 544, 546, 549, 568, 573, 576, 587, 589, 596, 626, 669, 671, 673, 680, 681, 694, 699, 700, 701, 709, 712, 713, 717, 728, 735, 736, 749, 750, 760, 766, 768, 769, 782, 800, 809, 811, 826, 832, 836, 840, 848, 852, 860, 861, 864, 866, 875, 878, 881, 882, 886, 889, 890, 894, 899, 900, 915, 919, 923, 929, 931, 933, 935, 943, 944, 948, 954, 956, 957, 958, 964, 969, 973, 983, 986
  taglia media per giocatore: 3.00 = battaglie 0.34 + missioni 1.48 + Tesoro 0.42 + monete 3.45 - missioni incomplete 2.71 + poker 0.02
  quota delle fonti positive: battaglie 5.9%, missioni 25.9%, Tesoro 7.4%, monete 60.5%, poker 0.3%
  missioni completate per partita: 1.48; vincitore: taglia media 7.46, ha preso il Tesoro nel 81.0% delle partite; parità al primo posto: 15
  poker: HighCard 99.0%, Pair 1.0%, TwoPair 0.1%

4 giocatori: 1000 partite, 981 finite con l'Isola Sacra, 19 interrotte da maxRounds, 0 errori
  round (partite finite): min 9, media 146.97, mediana 123, max 494
  interrotte da maxRounds, seed: 12, 52, 123, 155, 189, 201, 203, 318, 332, 369, 389, 408, 491, 540, 562, 706, 714, 760, 930
  taglia media per giocatore: 2.72 = battaglie 0.48 + missioni 1.04 + Tesoro 0.25 + monete 3.18 - missioni incomplete 2.25 + poker 0.03
  quota delle fonti positive: battaglie 9.7%, missioni 20.9%, Tesoro 4.9%, monete 63.9%, poker 0.6%
  missioni completate per partita: 2.07; vincitore: taglia media 12.84, ha preso il Tesoro nel 94.5% delle partite; parità al primo posto: 3
  poker: HighCard 98.2%, Pair 1.8%, ThreeOfAKind 0.0%

8 giocatori: 1000 partite, 1000 finite con l'Isola Sacra, 0 interrotte da maxRounds, 0 errori
  round (partite finite): min 10, media 91.70, mediana 76, max 444
  taglia media per giocatore: 2.06 = battaglie 0.72 + missioni 0.72 + Tesoro 0.13 + monete 2.24 - missioni incomplete 1.79 + poker 0.04
  quota delle fonti positive: battaglie 18.7%, missioni 18.7%, Tesoro 3.3%, monete 58.3%, poker 1.0%
  missioni completate per partita: 2.96; vincitore: taglia media 17.09, ha preso il Tesoro nel 99.6% delle partite; parità al primo posto: 0
  poker: HighCard 97.4%, Pair 2.6%, TwoPair 0.0%, ThreeOfAKind 0.0%, Straight 0.0%, FullHouse 0.0%

Errori: nessuno
```

**Rispetto alla 20×20** (ultimo gate, spec 0004 dopo R-020):

| Giocatori | Interrotte da `maxRounds` | Round min / mediana / max (finite) |
|---|---|---|
| 2 | 158 (prima 89) | 15 / 174 / 498 (prima 17 / 152 / 500) |
| 4 | 19 (prima 4) | 9 / 123 / 494 (prima 9 / 85 / 476) |
| 8 | 0 (prima 0) | 10 / 76 / 444 (prima 7 / 50 / 310) |

La mappa è più grande e i punti di partenza sono a 11 celle dall'Isola Sacra, con l'anello in Tempesta (livello 5) a sbarrare quasi tutte le rotte, quindi i bot casuali ci mettono di più. Le battaglie calano molto (a 2 giocatori 0,34 segnalini a testa invece di 0,76): le navi si incontrano meno. Per questo `Simulation60RoundsWithFullPhase2` ora richiede attacchi solo a 8 giocatori (a 4, su 30 partite da 60 round, le navi a tiro sono capitate 14 volte), e controlla anche che i bot mettano il segnalino. Sono statistiche di bot casuali, non di bilanciamento.

#### Domande

- **R-097 — Vedetta con più isole a tiro**: con Vedetta + Jolly (2 celle) possono esserci più isole raggiungibili; `02` non dice su quale va il segnalino. Ho scelto la più vicina e, a parità, quella con l'id più basso. In alternativa il giocatore potrebbe sceglierla (una decisione in più). `// TODO R-097` in `TurnFlow.PortIsland`.
- **Livello iniziale degli spicchi in due posti**: `05` §5 mette il livello iniziale nel layout (campo `zones`), la spec chiede anche `RulesConfig.ringInitialLevel`. Per non avere due verità, `Validate()` rifiuta un layout i cui spicchi non partono da `ringInitialLevel` (e le nuvole da 0): il valore che conta è quello della config, il layout deve coincidere. Effetto collaterale: cambiare `ringInitialLevel` in `RulesConfig.asset` rende non valido `MapLayout.asset` finché non lo si aggiorna. Si può togliere uno dei due?
- **R-097 — isole di più celle**: passare da una cella all'altra della stessa isola col proprio segnalino conta come arrivo. Nel layout v4 tutte le isole hanno una cella sola, quindi oggi non succede.

#### Note

- Le carte Meteo bersagliano una zona per id, ma la decisione resta un elenco di tutte le 20 zone (anche quelle dove la carta sarebbe sprecata): sprecarla è legale (R-088).
- "Adiacente a un'isola" per i punti di partenza in mare (`05` §5) comprende anche l'Isola Sacra.
- `ShipStrandedEvent` per l'isola del segnalino porta la cella dell'isola: Gamba di legno conta celle distinte di cornice e di isola (R-069, `03`).

### Claude Code — 2026-10-03 — commit 3600e40

Risposte alle due domande della voce precedente (patch `1f52a93`, "docs: patch Athena (R-097 Vedetta, livello anello)").

- **R-097 — Vedetta con più isole a portata**: ora sceglie il giocatore. Nuova decisione `LookoutIsland` (segreta, opzioni `IslandOption` con l'id dell'isola, in ordine crescente), chiesta subito dopo aver scelto il turno di porto con la Vedetta e solo se le isole a portata sono più di una; con una sola isola il motore la applica da solo. Il segnalino va sull'isola scelta. Tolti il `// TODO R-097` e la scelta "più vicina, poi id più basso". Sul layout v4 succede solo con Vedetta + Jolly (portata 2): le isole distano almeno 3 tra loro. Test: `WithOneIslandInReachTheLookoutDoesNotAsk_R058_R097`, `WithSeveralIslandsInReachThePlayerChooses_R058_R097` (morde: prendere sempre la prima isola lo fa diventare rosso), `ChoosingTheSeaTurnAsksNoIsland_R058`.
- **Livello iniziale degli spicchi**: tolto `RulesConfig.ringInitialLevel` e il confronto in `Validate()`. Il livello iniziale di ogni zona viene solo dal layout; `Validate()` controlla soltanto che non sia negativo (R-081), con lo stesso codice `ZoneInitialLevel`. `RulesConfigAsset` non ha un campo suo: serializza `RulesConfig` intera, quindi il campo sparisce con la classe (in `RulesConfig.asset`, se c'era, Unity lo ignora). `LayoutV4` porta il livello di ogni zona dalla tabella di `05` §3. Test: `TheInitialLevelComesOnlyFromTheLayout_05_3`.
- `dotnet test Tools/RulesHarness` verde, 398 test.

**Gate** (seed 1–1000 × 2/4/8, `maxRounds` 500, layout v4, 1 min 19 s): **nessuna eccezione, nessuna violazione di invarianti**. Rispetto al gate precedente cambia poco: la decisione nuova consuma scelte del bot e sposta qualche partita (interrotte 157 / 19 / 0 invece di 158 / 19 / 0).

```
Simulazione: seed 1-1000, maxRounds 500, bot casuale (seed del bot = seed della partita)

2 giocatori: 1000 partite, 843 finite con l'Isola Sacra, 157 interrotte da maxRounds, 0 errori
  round (partite finite): min 15, media 198.28, mediana 174, max 498
  interrotte da maxRounds, seed: 10, 11, 17, 24, 32, 56, 58, 79, 82, 83, 85, 91, 93, 96, 116, 117, 118, 120, 133, 151, 152, 161, 165, 166, 177, 197, 209, 210, 211, 215, 216, 224, 227, 228, 229, 230, 245, 248, 250, 251, 255, 279, 287, 291, 297, 303, 304, 328, 330, 339, 340, 341, 343, 344, 348, 349, 354, 356, 368, 374, 383, 399, 407, 414, 418, 425, 427, 433, 445, 454, 461, 468, 482, 491, 492, 501, 504, 510, 512, 516, 520, 532, 544, 546, 549, 568, 573, 576, 587, 589, 596, 626, 669, 671, 673, 680, 681, 694, 699, 700, 701, 709, 712, 713, 717, 728, 735, 736, 749, 750, 760, 766, 768, 769, 782, 800, 809, 811, 826, 832, 836, 840, 848, 852, 860, 861, 864, 866, 875, 878, 881, 882, 886, 889, 890, 894, 899, 900, 915, 919, 923, 929, 931, 933, 935, 943, 944, 948, 954, 956, 957, 958, 964, 969, 973, 983, 986
  taglia media per giocatore: 2.99 = battaglie 0.34 + missioni 1.47 + Tesoro 0.42 + monete 3.45 - missioni incomplete 2.70 + poker 0.02
  quota delle fonti positive: battaglie 5.9%, missioni 25.8%, Tesoro 7.4%, monete 60.5%, poker 0.3%
  missioni completate per partita: 1.47; vincitore: taglia media 7.44, ha preso il Tesoro nel 81.1% delle partite; parità al primo posto: 15
  poker: HighCard 99.0%, Pair 1.0%, TwoPair 0.1%

4 giocatori: 1000 partite, 981 finite con l'Isola Sacra, 19 interrotte da maxRounds, 0 errori
  round (partite finite): min 9, media 147.32, mediana 124, max 494
  interrotte da maxRounds, seed: 12, 52, 123, 155, 189, 201, 203, 318, 332, 369, 389, 408, 491, 540, 562, 706, 714, 760, 930
  taglia media per giocatore: 2.73 = battaglie 0.48 + missioni 1.04 + Tesoro 0.25 + monete 3.18 - missioni incomplete 2.24 + poker 0.03
  quota delle fonti positive: battaglie 9.6%, missioni 20.9%, Tesoro 4.9%, monete 64.0%, poker 0.6%
  missioni completate per partita: 2.07; vincitore: taglia media 12.85, ha preso il Tesoro nel 94.5% delle partite; parità al primo posto: 3
  poker: HighCard 98.2%, Pair 1.8%, ThreeOfAKind 0.0%

8 giocatori: 1000 partite, 1000 finite con l'Isola Sacra, 0 interrotte da maxRounds, 0 errori
  round (partite finite): min 10, media 91.34, mediana 74, max 444
  taglia media per giocatore: 2.05 = battaglie 0.72 + missioni 0.72 + Tesoro 0.13 + monete 2.23 - missioni incomplete 1.79 + poker 0.04
  quota delle fonti positive: battaglie 18.7%, missioni 18.8%, Tesoro 3.3%, monete 58.2%, poker 1.0%
  missioni completate per partita: 2.96; vincitore: taglia media 17.00, ha preso il Tesoro nel 99.6% delle partite; parità al primo posto: 0
  poker: HighCard 97.4%, Pair 2.6%, TwoPair 0.0%, ThreeOfAKind 0.0%, Straight 0.0%, FullHouse 0.0%

Errori: nessuno
```
