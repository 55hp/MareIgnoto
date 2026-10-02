# 03 — Contenuti: carte, missioni, tabelle

Dati di contenuto del gioco. Gli **ID** (colonna `Id`) sono quelli da usare nel codice (enum o costanti). I **numeri** (copie, costi, ricompense) sono i valori di default di `RulesConfig` (`01_architettura.md` §4): vivono lì e solo lì, il codice non li ripete altrove. Gli **effetti** sono codice.

Riferimenti alle regole: `02_regole.md`.

---

## 1. Mazzo Crew — 54 carte

4 semi × 13 ranghi + 2 Jolly. Semi: Cuori, Quadri, Fiori, Picche (servono solo al poker, R-144).

| Rango | Id | Nome | Posizione effetto | Effetto | Regole |
|---|---|---|---|---|---|
| 1 | `Mozzo` | Mozzo | Sopra | +1 moneta all'inizio del proprio turno | R-052 |
| 2 | `Bucaniere` | Bucaniere | Sopra | +1 carta Pirateria giocabile nel turno | §7.3 |
| 3 | `Vedetta` | Vedetta | Sopra | "Terra!!!": attracca a un porto anche da 1 cella di distanza | R-058 |
| 4 | `Cuoco` | Cuoco di bordo | Sopra | −1 moneta su ogni azione di porto a pagamento; +1 risorsa alle carte pesca giocate nel turno | R-095, R-122 |
| 5 | `Medico` | Medico di bordo | **Sotto** | Sale sopra coperta al posto di una carta sopra coperta che si sta per perdere (questa scende sotto); la perdita è annullata | R-023 |
| 6 | `Navigatore` | Navigatore | Sopra | −1 intensità meteo percepita | R-085 |
| 7 | `Cannoniere` | Cannoniere | Sopra | Una Bordata! gratuita per combattimento | R-105 |
| 8 | `Timoniere` | Timoniere | Sopra | +1 velocità; sceglie l'esito invece di tirare il dado (rotazione meteo, riposizionamento); nell'Abbordaggio sceglie per ultimo | R-063, R-086, R-073b |
| 9 | `Nostromo` | Nostromo | Sopra | Vincendo una battaglia ruba 1 carta Pirateria al perdente; a fine partita raddoppia il punteggio poker; una volta sopra non torna sotto | R-109, R-146, R-016 |
| 10 | `Quartiermastro` | Quartiermastro | Sopra | Scartando una Bordata! scambia una propria carta sopra coperta con una di un avversario a tiro; quel turno niente battaglia | R-111 |
| J (11) | `Falconet` | Falconet | Sopra | +1 gittata; ogni scontro che inizia diventa Duello obbligatorio | R-100, R-106 |
| Q (12) | `Saker` | Saker | Sopra | +1 gittata; vittoria con Bordata! su difensore senza Parlè! raddoppia il guadagno | R-100, R-107 |
| K (13) | `Culverin` | Culverin | Sopra | +2 gittata; attacco da 2+ celle toglie Parlè! al difensore | R-100, R-108 |
| — | `Jolly` | Jolly | Sopra | Duplica l'effetto dell'altra carta sopra coperta; a fine partita vedi R-145 | R-015, R-145 |

## 2. Mazzo Pirateria — 110 carte

### 2.1 Battaglia ed economia — 80 carte

| Id | Nome | Copie | Quando | Effetto | Regole |
|---|---|---|---|---|---|
| `Bordata` | Bordata! | 20 | Solo in combattimento | Attacco base (come il Bang! di *Bang!*) | R-103 |
| `Parle` | Parlè! | 20 | Solo in combattimento | Difesa base (come il Mancato!) | R-103, R-110 |
| `PescaFortunata` | Pesca Fortunata! | 12 | Carta del turno | +2 monete subito | R-122 |
| `ReteAStrascico` | Rete a Strascico! | 8 | Carta del turno | Scartala e pesca 3 carte Pirateria | R-122 |
| `Arrembaggio` | Arrembaggio! | 8 | Apertura di un attacco | Ruba 1 carta crew sopra coperta; il difensore può pagare 5 monete o giocare Parlè! per evitarlo | R-110 |
| `UomoInMare` | Uomo in mare! | 8 | Carta del turno, a tiro | Il bersaglio scarta 1 carta crew a sua scelta | R-120 |
| `Spyglass` | Spyglass! | 4 | Carta del turno, a tiro | Scambia di posizione due carte crew della nave avversaria bersaglio (scelte da chi gioca la carta; le carte sotto coperta si scelgono alla cieca, per slot) | R-120, R-016 |

### 2.2 Meteo — 30 carte

Costo pagato al Tesoro (R-121). Bersaglio: qualsiasi zona, da qualsiasi posizione.

| Id | Nome | Costo | Copie | Effetto |
|---|---|---|---|---|
| `SupplicaGartya` | Supplica a Gartya | 2 | 6 | Sposta di 1 scatto la lancetta del vento dominante, in senso orario o antiorario a scelta |
| `InvocazioneGartya` | Invocazione di Gartya | 5 | 6 | La zona scelta diventa Mare Mosso |
| `IraGartya` | Ira di Gartya | 10 | 3 | La zona scelta diventa Tempesta |
| `FavoreGartya` | Favore di Gartya | 2 | 5 | La zona scelta scende di un livello (Tempesta→Mare Mosso, Mare Mosso→Normale) |
| `VentoInPoppa` | Vento in Poppa | 0 | 5 | Immunità al meteo nella Fase 1 del round successivo (R-087) |
| `RafficaCanaglia` | Raffica canaglia | 0 | 5 | Inverte la lancetta del vento dominante (180°) |

Controllo: 6+6+3+5+5+5 = 30; 20+20+12+8+8+8+4 = 80; totale 110.

## 3. Mazzo Corsaro — 17 missioni, 2 copie ciascuna (34 carte), non si rimescola (R-009)

Ricompense in segnalini taglia. "Contatore" indica cosa il motore deve tracciare per giocatore. Tutte rispettano R-131 (contano gli eventi dopo la pesca) salvo dove indicato.

| Id | Nome | Condizione esatta | Ricompensa | Valutazione | Contatore |
|---|---|---|---|---|---|
| `Barbanera` | Barbanera! | Ha vinto almeno una battaglia contro **ogni** altro giocatore | 2 × (numero di avversari) | Immediata | insieme degli avversari battuti |
| `Barbarossa` | Barbarossa! | Ha vinto 5 battaglie | 1 | Immediata | battaglie vinte |
| `OlandeseVolante` | Olandese Volante | Non ha perso nessuna battaglia (vale anche con zero battaglie) | 2 | Fine partita | battaglie perse |
| `MaledizionePirata` | Maledizione pirata | Ha perso 2 carte crew nello stesso round (R-021) | 2 | Immediata | perdite crew nel round corrente |
| `NaveCorsara` | Nave Corsara | Ha giocato 5 Bordata! nello stesso round, in qualsiasi modo (contano anche le Bordate gratuite del Cannoniere e del Falconet) | 2 | Immediata | Bordate nel round corrente |
| `Avido` | Avido | Ha avuto 30 o più monete | 1 | Immediata | monete correnti |
| `Avidissimo` | Avidissimo | Ha avuto 50 o più monete | 3 | Immediata | monete correnti |
| `SpugnaDiMare` | Spugna di mare! | Ha fatto 3 azioni Svago | 1 | Immediata | azioni Svago |
| `CacciatoreDiTaglie` | Cacciatore di Taglie | Ha completato 3 missioni nella partita (**conta tutta la partita**, eccezione a R-131) | 2 | Immediata | missioni completate |
| `Attaccabrighe` | Attaccabrighe | Ha combattuto (come attaccante o difensore, solo battaglie R-103/R-106) in 3 round consecutivi | 3 | Immediata | ultimo round con battaglia e serie corrente |
| `Bancarotta` | Bancarotta | È arrivato a 0 monete | 1 | Immediata | monete correnti |
| `DispersiInMare` | Dispersi in mare | È arrivato a 0 carte Pirateria in mano | 1 | Immediata | carte in mano |
| `LupoDiMare` | Lupo di mare | Ha vinto una battaglia con 0 carte crew sopra coperta | 1 | Immediata | — |
| `Gemelli` | Gemelli | Ha due carte dello stesso rango sopra coperta (i due Jolly valgono 4) | 2 (4 con i due Jolly) | Immediata | stato ciurma |
| `NaveDAssalto` | Nave d'assalto | Ha sopra coperta due carte di rango J, Q o K (qualsiasi combinazione) | 1 | Immediata | stato ciurma |
| `ParlareConIPesci` | Parlare con i pesci | Ha perso 3 carte crew "in mare" (R-022) | 3 | Immediata | perdite in mare |
| `GambaDiLegno` | Gamba di legno | Si è arenato 3 volte su 3 celle di cornice diverse (R-069) | 3 | Immediata | insieme delle celle di arenamento |

Esclusa dal mazzo: **Magellano x3** (bozza nel GDD, "Visita due isole specifiche", +2). Non va implementata finché Franci non la chiude.

## 4. Tabella punteggio poker (R-144)

| Combinazione | Segnalini |
|---|---|
| Carta Alta | 0 |
| Coppia | 1 |
| Doppia Coppia | 2 |
| Tris | 2 |
| Scala | 4 |
| Colore | 4 |
| Full | 6 |
| Poker | 8 |
| Scala Reale | 10 |
| Scala Reale Massima | 12 |

Dettagli confermati da Franci (non scritti nel GDD):
- Il Mozzo (rango 1) è l'asso: vale sia come 1 (scala 1-2-3-4-5) sia come carta più alta dopo il K (scala 10-J-Q-K-1). Niente scale "che girano" (Q-K-1-2-3).
- **Scala Reale** = scala dello stesso seme. **Scala Reale Massima** = 10-J-Q-K-1 dello stesso seme.
- Tris e Doppia Coppia valgono uguale (2), come da GDD.
