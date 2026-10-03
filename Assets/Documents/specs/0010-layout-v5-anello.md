# 0010 — Layout v5: anello a archi e nuvole riviste

Stato: pronta per Claude Code
Commit di riferimento: da compilare
Regole coinvolte: R-080 (testo); `05` §3–§6 (nuova versione, §A1)
Documenti: `tech/05_mappa.md`, `tech/02_regole.md`

Un file per task. Sopra `## Report` scrivono Athena (intestazione, Obiettivo, Parte A) e Claude Code (Parte B). Sotto `## Report` ogni attore aggiunge voci in coda.

## Obiettivo

Sostituire il layout v4 con il v5 approvato da Franci il 03/10/2026: l'anello centrale diventa un insieme di 8 archi regolari (4 varchi diagonali a spigolo e 4 canali dritti larghi 1 sugli assi), le nuvole passano da 12 a 8. Cambia solo la mappa: le regole del gioco non cambiano (salvo il testo di R-080 che nomina le nuvole). Prima i documenti (Parte A), poi il motore e l'asset (Parte B).

## Parte A — Patch di documentazione (testo esatto)

Regole di applicazione (`CLAUDE.md`, "Patch di documentazione da Athena"): applica parola per parola, senza riformulare. Se un file, una riga o una sottostringa non esiste o è diversa da come la descrivo, fermati e riportalo. Un commit a parte "docs: patch Athena (layout v5)", solo documenti. Mostra `git diff --stat`.

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

- **Nuvola**: forma curva e irregolare, spessore 2–3 celle, partenza a livello 0.
- **Spicchio**: arco dell'anello di tempesta che circonda l'Isola Sacra, partenza a **livello 5**, scritto nel layout (campo `livelloIniziale` di ogni zona).

Ogni zona ha un **livello** intero ≥ 0 (R-081): 0 Normale, 1 Mare Mosso, ≥ 2 Tempesta. Le celle di mare che non stanno in nessuna zona sono mare libero.

**Regole di costruzione** (`Validate()` le controlla tutte):

- ogni zona è fatta di celle di mare (non isole, non cornice) ed è connessa per lati;
- le nuvole (N1–N8) stanno a distanza Chebyshev ≥ 2 tra loro (almeno 1 cella libera), ≥ 3 da ogni spicchio e ≥ 2 da ogni punto di partenza; possono toccare un'isola;
- gli spicchi sono 8, in ordine R1…R8 per angolo attorno all'Isola Sacra. Due spicchi **consecutivi** sono collegati in uno di due modi: **varco diagonale** (si toccano per **uno spigolo solo** e per nessun lato; le altre due celle del blocco 2×2 sono di mare libero e permettono un solo passo diagonale tra loro) oppure **canale** (nessun contatto; tra i due c'è una fila dritta di celle di mare libero larga 1). Due spicchi non consecutivi stanno a distanza ≥ 2;
- uno spicchio sta a distanza ≥ 2 da ogni cella dell'Isola Sacra (1 cella libera) e ≥ 3 da ogni altra isola.

### Elenco delle zone

| Id | Tipo | Livello iniziale | Celle | Elenco |
|---|---|---|---|---|
| R1 | spicchio | 5 | 6 | Q13, Q14, Q15, R13, R14, R15 |
| R2 | spicchio | 5 | 6 | N16, N17, O16, O17, P16, P17 |
| R3 | spicchio | 5 | 6 | J16, J17, K16, K17, L16, L17 |
| R4 | spicchio | 5 | 6 | H13, H14, H15, I13, I14, I15 |
| R5 | spicchio | 5 | 6 | H10, H11, H9, I10, I11, I9 |
| R6 | spicchio | 5 | 6 | J7, J8, K7, K8, L7, L8 |
| R7 | spicchio | 5 | 6 | N7, N8, O7, O8, P7, P8 |
| R8 | spicchio | 5 | 6 | Q10, Q11, Q9, R10, R11, R9 |
| N1 | nuvola | 0 | 9 | U17, U18, V17, V18, W16, W17, W18, X15, X16 |
| N2 | nuvola | 0 | 9 | P23, Q22, Q23, R20, R21, R22, S20, S21, S22 |
| N3 | nuvola | 0 | 9 | G20, G21, G22, H20, H21, H22, I22, I23, J23 |
| N4 | nuvola | 0 | 9 | B15, B16, C16, C17, C18, D17, D18, E17, E18 |
| N5 | nuvola | 0 | 9 | B8, B9, C6, C7, C8, D6, D7, E6, E7 |
| N6 | nuvola | 0 | 9 | G2, G3, G4, H2, H3, H4, I1, I2, J1 |
| N7 | nuvola | 0 | 9 | P1, Q1, Q2, R2, R3, R4, S2, S3, S4 |
| N8 | nuvola | 0 | 9 | U6, U7, V6, V7, W6, W7, W8, X8, X9 |

### Varchi e canali dell'anello

| Tra | Tipo | Celle | Celle libere (solo varchi) |
|---|---|---|---|
| R1–R2 | varco diagonale | Q15, P16 | Q16, P15 |
| R2–R3 | canale dritto | M16, M17 | — |
| R3–R4 | varco diagonale | J16, I15 | J15, I16 |
| R4–R5 | canale dritto | H12, I12 | — |
| R5–R6 | varco diagonale | I9, J8 | I8, J9 |
| R6–R7 | canale dritto | M7, M8 | — |
| R7–R8 | varco diagonale | P8, Q9 | P9, Q8 |
| R8–R1 | canale dritto | Q12, R12 | — |

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

Proprietà verificate sul layout v5 (valgono per ogni N da 2 a 8): ogni punto di partenza dista **11** celle dall'Isola Sacra e ha le due isole più vicine a **4 e 4** celle. Gli angoli hanno 3 rotte che portano in mare (N, NE, E per B1); i lati ne hanno 3.

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

## 6. Layout approvato (v5, 25×25)

Approvato da Franci il 03/10/2026; sostituisce la v4, il cui anello a lobi risultava poco leggibile. Simmetria: isole, nuvole, anello, Isola Sacra e punti di partenza sono invarianti per rotazione di 90° e per specchio.

`#` cornice, `.` mare libero, `1`–`8` spicchi R1–R8, `a`–`h` nuvole N1–N8, `I` isola, `S` Isola Sacra, `o` punto di partenza.

```
   ABCDEFGHIJKLMNOPQRSTUVWXY
24 ############o############
23 #o......cc.....bb......o#
22 #....Iccc.......bbbI....#
21 #.....ccI.......Ibb.....#
20 #.....cc.........bb.....#
19 #.I...................I.#
18 #.ddd...............aaa.#
17 #.ddd....333.222....aaa.#
16 #ddI.....333.222.....Iaa#
15 #d.....44.......11.....a#
14 #......44.......11......#
13 #......44...S...11......#
12 o..........SSS..........o
11 #......55...S...88......#
10 #......55.......88......#
 9 #e.....55.......88.....h#
 8 #eeI.....666.777.....Ihh#
 7 #.eee....666.777....hhh.#
 6 #.eee...............hhh.#
 5 #.I...................I.#
 4 #.....ff.........gg.....#
 3 #.....ffI.......Igg.....#
 2 #....Ifff.......gggI....#
 1 #o......ff.....gg......o#
 0 ############o############
```

- **Isola Sacra** (croce, 5 celle): M12, L12, N12, M11, M13.
- **Isole** (16, `islandId` = numero): 1=C5, 2=C19, 3=D8, 4=D16, 5=F2, 6=F22, 7=I3, 8=I21, 9=Q3, 10=Q21, 11=T2, 12=T22, 13=V8, 14=V16, 15=W5, 16=W19.
- **Distanze**: dall'Isola Sacra 11 per tutti i punti di partenza; due isole più vicine a 4 e 4.
- **Rotte sicure** (senza celle di tempesta, 11 passi): dagli angoli 7 rotte minime su 23; dai lati 2.751 su 25.653. Verso le due isole più vicine: angoli 12 su 12, lati 3 su 3.
- **Distanze minime**: anello–Isola Sacra 3, anello–isole 4, anello–nuvole 3, nuvole tra loro 2, nuvole–punti di partenza 3, spicchi non consecutivi 5.
- **Copertura**: 120 celle di zona (48 di anello, 72 di nuvole) su 508 celle di mare, il 24%.
- **Scostamenti noti**: la copertura è sotto il 30% indicativo, per non restringere le rotte dei lati; le nuvole possono toccare le isole; l'anello ha spessore 2–3.
<<<FINE 05_mappa.md>>>

### A2. `tech/02_regole.md`

Sostituisci la riga di tabella la cui prima cella è R-080 con esattamente questa riga:

| R-080 | Il meteo è una proprietà delle **zone**: gruppi fissi di celle di mare definiti nel layout (`05_mappa.md` §3), cioè le nuvole (N1–N8) e gli spicchi dell'anello centrale (R1–R8). Le celle di mare che non appartengono a nessuna zona sono mare libero: non hanno meteo e non cambiano mai. Le celle isola non appartengono a nessuna zona. |

### A3. `specs/0005-scena-tabellone.md`, sostituzioni esatte di sottostringa (ognuna deve combaciare una sola volta)

- "nell'Editor eseguire **MareIgnoto > Create MapLayout v4**. Il comando crea" → "nell'Editor eseguire **MareIgnoto > Create MapLayout v5**. Il comando crea"
- "dal layout v4 (`LayoutV4`, `05` §3–§6); se il file esiste" → "dal layout v5 (`LayoutV5`, `05` §3–§6); se il file esiste"

### A4. `tech/00_INDICE.md`

Aggiungi in fondo alla tabella "Registro modifiche":

| 03/10/2026 (4) | **`05`**: layout v5. L'anello diventa 8 archi regolari: 4 varchi diagonali a spigolo e 4 canali dritti larghi 1 sugli assi (prima: lobi irregolari con varchi diagonali). Nuvole da 12 a 8. Copertura 24%. **R-080**: nuvole N1–N8 | `05`, `02` |

## Parte B — Lato codice (Claude Code)

Leggi prima `tech/05_mappa.md` (nuova versione). Le regole del gioco non cambiano: cambiano i dati della mappa e le regole di costruzione delle zone.

1. **Dati**: sostituisci `LayoutV4` con `LayoutV5` (stesso posto in Rules, UNA sola copia): isole, Isola Sacra, preset di partenza (invariati), 8 spicchi e 8 nuvole costruiti dagli elenchi di `05` §3 e §6 (senza duplicati). Tolti gli ex dati della v4 e ogni riferimento a "20 zone" o a nuvole N9–N12.
2. **`Validate()`**: aggiorna le regole di costruzione delle zone come in `05` §3: nuvole a distanza ≥ 2 tra loro, ≥ 3 da ogni spicchio, ≥ 2 dai punti di partenza, possono toccare un'isola; 8 spicchi in ordine di angolo; tra due consecutivi esattamente uno di due casi (varco diagonale a un solo spigolo con le altre due celle del 2×2 di mare libero, oppure canale: nessun contatto e una fila dritta di mare libero larga 1); non consecutivi a distanza ≥ 2; spicchio a distanza ≥ 2 dall'Isola Sacra e ≥ 3 dalle altre isole. Test per ogni regola con un layout non valido.
3. **Test sul layout**: verifica che `LayoutV5` superi `Validate()` e che valgano le proprietà di `05` §6: ogni punto di partenza a distanza 11 dall'Isola Sacra e con le due isole più vicine a 4 e 4; simmetria di rotazione e di specchio di isole, nuvole, anello e punti di partenza; conteggi (16 isole, 8+8 zone, 120 celle di zona). Aggiorna `ZoneGeometryTests` (ora 16 zone) e ogni test che nomina zone o conteggi della v4.
4. **Comando Editor e test Unity**: rinomina il comando in `MareIgnoto/Create MapLayout v5` (costruisce l'asset da `LayoutV5`, sovrascrive `MapLayout.asset` in place dopo conferma, poi `Validate()`); rinomina `MapLayoutV4AssetTests` in `MapLayoutV5AssetTests`, che confronta l'asset con `LayoutV5` cella per cella.
5. **Gate**: rilancia il gate da 1000 seed (2/4/8 giocatori) sul layout v5 e incolla il riepilogo: partite fermate da `maxRounds` con i seed, round minimo/mediano/massimo e cosa è cambiato rispetto alla v4.
6. Aggiorna la sezione "Lato codice" e la checklist di `specs/0005-scena-tabellone.md` dove nomina il layout v4 o 20 zone, e lo stato in `README.md`.

Se una regola non è chiara, non inventare: lascia un TODO con l'id della regola e scrivi la domanda sotto "#### Domande".

## Lato Editor (Bezi) — checklist

Nessuna in questo task. Le viste e i prefab non cambiano.

## Per Franci — modifiche isolate

- Dopo il task: aprire l'Editor, controllare la Console, eseguire **MareIgnoto > Create MapLayout v5** (conferma la sovrascrittura), controllare che la Console dica `Validate OK`, lanciare il Test Runner EditMode e committare `MapLayout.asset`.

## Verifica

- Parte A: `git diff --stat` mostra solo documenti.
- Parte B: `dotnet test Tools/RulesHarness` verde; gate da 1000 seed senza eccezioni né violazioni di invarianti; `LayoutV5` supera `Validate()`.

## Report

<!-- Voci in coda. Intestazione: ### Claude Code — AAAA-MM-GG — commit abc1234 -->
