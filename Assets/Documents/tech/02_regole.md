# 02 — Regole operative del gioco

Questo è il regolamento completo in forma implementabile. Deriva dal GDD su Notion ("Mareignoto - the main PROJECT", revisione chiusa il 02/10/2026), che gli agenti non leggono: **per gli agenti questo file è il GDD**.

Ogni regola ha un ID (`R-xxx`) da citare in codice, test e report. Le voci marcate **[DEFAULT]** non sono scritte nel GDD: sono una scelta di Athena per non bloccare il lavoro, elencata anche in `_PER_FRANCI/DOMANDE_APERTE.md`. Si implementano così finché Franci non decide diversamente; quando cambia, cambia prima questo file e poi il codice.

Se una regola qui è ambigua o manca un caso: **non inventare**. Implementa il resto, scrivi il caso nel report della spec sotto "Domande", e lascia un punto di estensione chiaro (un `// TODO R-xxx` con il riferimento).

---

## 1. Componenti e numeri

| ID | Regola |
|---|---|
| R-001 | Giocatori: da 2 a 8. |
| R-002 | Mappa: griglia quadrata 20×20. Prima/ultima riga e prima/ultima colonna sono **cornice** (terra ferma, punti di partenza). Area navigabile 18×18. Dettaglio celle e coordinate: `05_mappa.md`. |
| R-003 | Mazzo Crew: 54 carte = 13 ranghi (1–10, J, Q, K) × 4 semi + 2 Jolly. |
| R-004 | Mazzo Pirateria: 110 carte = 80 da battaglia/economia + 30 Meteo. Composizione: `03_contenuti.md`. |
| R-005 | Mazzo Corsaro: missioni, elenco in `03_contenuti.md`. **[DEFAULT]** 2 copie per missione (con 8 giocatori il setup pesca 24 carte). |
| R-006 | Moneta: unica risorsa economica, intero ≥ 0. |
| R-007 | Segnalini taglia: intero; il punteggio finale si chiama "taglia". |
| R-008 | Dado: d8 (risultati 1–8). Tutti i tiri passano dalla sorgente di casualità del motore (`04_motore.md`). |
| R-009 | Ogni mazzo ha la sua pila degli scarti. Quando un mazzo si esaurisce, la sua pila degli scarti viene rimescolata e diventa il nuovo mazzo. Se anche gli scarti sono vuoti, si pescano solo le carte disponibili. (Il GDD lo dice per il Crew; **[DEFAULT]** stessa regola per Pirateria e Corsaro.) |

## 2. La nave e la ciurma

| ID | Regola |
|---|---|
| R-010 | Ogni giocatore ha una nave su una cella della mappa e una **ciurma** di 5 slot: 2 **sopra coperta** (scoperte, visibili a tutti) e 3 **sotto coperta** (coperte, visibili solo al proprietario). Gli slot possono essere vuoti. |
| R-011 | Sopra coperta: la carta applica il suo effetto passivo. Sotto coperta: **nessun effetto**, unica eccezione il Medico di bordo (rango 5). |
| R-012 | Mano Pirateria: nessun limite di carte. Missioni Corsaro in mano: nessun limite. |
| R-013 | Tutti gli effetti delle carte crew sono cumulabili, anche tra due copie dello stesso rango. |
| R-014 | Valore di una carta crew per l'ordine turno e per Commercio: rango numerico 1–10; J, Q, K valgono rispettivamente 11, 12, 13 per l'ordine turno **[DEFAULT]** e 10 per Commercio (GDD). Jolly: 0 per l'ordine turno (GDD), 0 per Commercio **[DEFAULT]**. Slot vuoto: 0. |
| R-015 | **Jolly sopra coperta**: se l'altro slot sopra coperta contiene una carta crew, il Jolly ne duplica l'effetto (come se quella carta fosse presente due volte). Da solo, o accanto all'altro Jolly, non fa nulla. Effetti di fine partita: §9. |
| R-016 | **Nostromo** (rango 9): una volta messo sopra coperta non può più tornare sotto coperta per tutta la partita. Qualsiasi effetto che lo sposterebbe sotto (swap, Spyglass!, Quartiermastro) non può sceglierlo come carta da spostare sotto. |

### Ricevere e perdere carte crew

| ID | Regola |
|---|---|
| R-020 | **Ricevere** una carta crew (Reclutamento, Arrembaggio!, Quartiermastro, Spyglass! non rientra: è uno scambio): il ricevente la mette in uno slot vuoto a sua scelta. Se non ci sono slot vuoti, sceglie: sostituire una carta già presente (che va negli scarti Crew) oppure scartare direttamente la carta in arrivo. La regola sul Nostromo (R-016) resta valida. |
| R-021 | **Perdita di crew**: una carta crew lascia gli slot del giocatore per un effetto subito (meteo, Abbordaggio fortuito, Uomo in mare!, Arrembaggio!, sacrificio del Medico, sostituzione o scarto in R-020). Non è una perdita la vendita con Commercio né uno scambio (swap, Spyglass!, Quartiermastro). Le perdite servono alle missioni (§8). |
| R-022 | Perdita "in mare": perdita avvenuta mentre la nave è su una cella di mare (non isola, non cornice). **[DEFAULT]** comprende anche l'Abbordaggio fortuito. Serve alla missione "Parlare con i pesci". |
| R-023 | **Medico di bordo** (rango 5, sotto coperta): quando il giocatore sta per perdere una carta crew **sopra coperta** (R-021), può sacrificare il Medico al posto di quella carta: il Medico va negli scarti, la carta minacciata resta dov'è, l'effetto che causava la perdita è bloccato. **[DEFAULT]** sull'interpretazione: il GDD dice "si sacrifica (si sposta sopra coperta) al posto di qualsiasi carta crew scoperta"; si è letto "si sacrifica" come "viene scartato". Il sacrificio è una scelta del giocatore, non automatica. Il sacrificio del Medico conta come perdita (R-021). |

## 3. Preparazione della partita (solo prima del primo round)

| ID | Regola |
|---|---|
| R-030 | Si mescolano i mazzi Crew, Pirateria e Corsaro. |
| R-031 | Ogni nave parte su un punto di partenza della cornice (`05_mappa.md`). Il punto viene assegnato in ordine di posto **[DEFAULT]**. |
| R-032 | Ogni giocatore pesca 2 carte crew e le mette sotto coperta, negli slot sotto coperta che preferisce. |
| R-033 | Ogni giocatore pesca 3 carte Pirateria. |
| R-034 | Ogni giocatore pesca 3 carte Corsaro, ne tiene almeno 1 e scarta le altre che non vuole. |
| R-035 | Ogni giocatore riceve 10 monete. |
| R-036 | **Offerta a Gartya**: ogni giocatore sceglie in segreto quante monete offrire (da 0 a tutte). Le offerte vengono rivelate insieme e le monete vanno nel **Tesoro** dell'Isola Sacra. |
| R-037 | L'ordine di turno del **primo round** è per offerta decrescente (chi ha offerto di più gioca per primo). Pareggi: tiro di dado, il risultato più alto precede; se il tiro pareggia si ritira tra i pari. |
| R-038 | Direzione iniziale del vento dominante: **[DEFAULT]** casuale tra le 8. Stato iniziale di tutte le zone meteo: **[DEFAULT]** Normale. |

## 4. Struttura del round

Il round ha due fasi. La partita è una sequenza di round finché non scatta la fine (§9).

### Fase 1 — Preparazione (simultanea, segreta)

| ID | Regola |
|---|---|
| R-040 | Ogni giocatore sceglie in segreto una **rotta**: una delle 8 direzioni (N, NE, E, SE, S, SO, O, NO). La scelta è obbligatoria. |
| R-041 | Le rotte vengono rivelate tutte insieme. |
| R-042 | Si applicano gli effetti del **meteo** (§6) a ogni nave che si trova in una zona meteo, nell'ordine di turno del round precedente (per il round 1: ordine dell'offerta) **[DEFAULT]** per l'ordine delle scelte. |
| R-043 | Si applica il **movimento** simultaneo, cella per cella (§5), poi si risolvono tutti gli Abbordaggi fortuiti (§5.3). |
| R-044 | In Fase 1 non si gioca nessuna carta. |
| R-045 | Eccezione **Svago** (R-091): la nave che ha scelto Svago nel round precedente non sceglie una rotta, non subisce il meteo e non si muove. |

### Fase 2 — Attiva (a turno)

| ID | Regola |
|---|---|
| R-050 | Ordine di turno dal round 2 in poi: somma crescente dei valori (R-014) delle 2 carte sopra coperta di ciascun giocatore. Il totale più basso gioca per primo. Si calcola all'inizio della Fase 2. |
| R-051 | Pareggi nell'ordine: chi ha meno monete, poi chi ha meno carte Pirateria in mano, poi tiro di dado (come R-037). |
| R-052 | All'inizio del proprio turno il giocatore riceve 1 moneta per ogni Mozzo sopra coperta (R-013, R-015). **[DEFAULT]**: non vale se il turno salta per la cornice (R-053). |
| R-053 | Nave sulla **cornice**: il turno del giocatore termina subito. |
| R-054 | Nave su un'**isola** (porto): il giocatore sceglie **una** azione di porto (§7) e il turno termina. **[DEFAULT]**: in porto non si fanno swap, attacchi né carte Pirateria. |
| R-055 | Nave sull'**Isola Sacra**: vedi §9; nessuna azione di porto. |
| R-056 | Nave in **mare**: il giocatore sceglie una delle due opzioni: (A) pesca 2 carte Pirateria e il turno termina; (B) esegue, nell'ordine che vuole, nessuna, alcune o tutte queste azioni, ciascuna al massimo una volta: 1 swap crew (R-057), 1 attacco (§7.2), 1 carta Pirateria giocata (§7.3). Poi il turno termina. |
| R-057 | **Swap crew**: sposta una carta da uno slot a un altro qualsiasi dei propri 5 slot (anche verso uno slot vuoto, anche tra due slot dello stesso tipo). Se lo slot di arrivo è occupato, le due carte si scambiano. Vincolo Nostromo (R-016). |
| R-058 | **Vedetta** (rango 3): se la nave termina il movimento su una cella di mare adiacente (distanza 1, `05_mappa.md`) a una cella isola, il giocatore può scegliere di trattare il turno come se fosse in quel porto (R-054). |

## 5. Movimento

### 5.1 Velocità

| ID | Regola |
|---|---|
| R-060 | Velocità base: 1 cella. |
| R-061 | Vento dominante: una direzione tra le 8, condivisa da tutta la mappa. Rotta **uguale** alla direzione del vento: +1. Rotta **opposta**: −1. Qualsiasi altra rotta (comprese le diagonali a 45° e 135°): nessun effetto. **[DEFAULT]** di convenzione: la lancetta indica la direzione **verso cui** soffia il vento. |
| R-062 | Il vento non si applica se la nave parte da un porto (isola) o dalla cornice. |
| R-063 | Timoniere (rango 8) sopra coperta: +1 velocità per copia (R-013, R-015). |
| R-064 | La velocità finale non scende sotto 0. Velocità 0 = la nave non si muove. |

### 5.2 Risoluzione cella per cella

| ID | Regola |
|---|---|
| R-065 | Il movimento è simultaneo a passi discreti. A ogni passo ogni nave con passi residui avanza di una cella nella sua rotta. |
| R-066 | Dopo ogni passo si controlla la cella appena raggiunta. Se è **terra** (isola, Isola Sacra o cornice) la nave si ferma lì e perde il movimento residuo. Se è occupata da **un'altra nave** (ferma o appena arrivata) la nave si ferma lì e perde il movimento residuo: è una **collisione**. |
| R-067 | **Attraversamento**: se due navi si scambiano di posizione nello stesso passo senza mai condividere una cella, la collisione avviene comunque. Le due navi si fermano sulla stessa cella, scelta dal giocatore con il valore più basso secondo l'ordine turno (R-050/R-051) tra le due celle coinvolte. |
| R-068 | Le celle di terra possono ospitare più navi senza collisione: le isole sono zona franca, la cornice è terra ferma. **[DEFAULT]** per le isole. |
| R-069 | Una nave che finisce sulla cornice si è **arenata** (serve alla missione "Gamba di legno"). |

### 5.3 Abbordaggio fortuito

Si risolve dopo che tutte le navi hanno finito di muoversi, per ogni cella di mare che contiene 2 o più navi.

| ID | Regola |
|---|---|
| R-070 | **Due navi**: ciascun giocatore sceglie autonomamente se perdere 1 carta crew (a scelta, qualsiasi slot) oppure 2 carte Pirateria (a scelta). Poi entrambi tirano 1d8 per il riposizionamento. Se i due risultati coincidono si ripete il ciclo (nuova perdita e nuovo tiro) finché non divergono. Ogni nave si sposta nella cella adiacente corrispondente al proprio risultato. |
| R-071 | **Tre o più navi**: ogni giocatore perde 1 carta crew e 1 carta Pirateria (le sceglie lui, ma non può scegliere tra le due opzioni). Ognuno tira 1d8 una sola volta, senza ripetizioni. Chi finisce sulla stessa cella di un'altra nave resta lì, senza un nuovo Abbordaggio fortuito in quel round. |
| R-072 | Mappatura del d8 sulle 8 celle adiacenti: 1=N, 2=NE, 3=E, 4=SE, 5=S, 6=SO, 7=O, 8=NO. |
| R-073a | Se la cella di destinazione è un'isola, la nave è in porto e in Fase 2 gioca il turno di porto (R-054). Se è la cornice, la nave è arenata (R-053, R-069). Se è occupata da una nave non coinvolta, la nave resta lì senza nuovo Abbordaggio **[DEFAULT]**. |
| R-073b | Timoniere sopra coperta: invece di tirare, il giocatore sceglie il risultato, e sceglie per ultimo (dopo aver visto i tiri degli altri coinvolti). |
| R-073c | Se un giocatore non ha carte da perdere del tipo scelto, perde quelle che ha. Se non ne ha di nessun tipo, non perde nulla. |
| R-073d | Il Medico (R-023) può bloccare la perdita di una carta sopra coperta. |

## 6. Meteo

| ID | Regola |
|---|---|
| R-080 | Il meteo è una proprietà delle **zone**: blocchi fissi di 3×3 celle di mare (`05_mappa.md`). Le celle isola non appartengono a nessuna zona. |
| R-081 | Stati: **Normale**, **Mare Mosso**, **Tempesta**. Nessun decadimento automatico: una zona cambia stato solo per una carta Meteo. |
| R-082 | Il meteo si applica nella Fase 1 (R-042), alla zona in cui la nave si trova prima di muoversi. Navi su isole o cornice non subiscono il meteo. |
| R-083 | **Mare Mosso**: si tira 1d8 e si ruota la rotta della nave di quel numero di scatti in senso orario (8 = nessun cambio). Poi il giocatore perde 1 carta Pirateria dalla mano (**[DEFAULT]** a sua scelta). |
| R-084 | **Tempesta**: come Mare Mosso per la rotazione; inoltre il giocatore perde 1 carta crew **sotto coperta**, se ne ha (a sua scelta). |
| R-085 | **Navigatore** (rango 6) sopra coperta: −1 intensità percepita per copia (Tempesta→Mare Mosso, Mare Mosso→Normale). |
| R-086 | **Timoniere** sopra coperta: sceglie la rotazione invece di tirare il d8. |
| R-087 | **Vento in Poppa** (carta): immunità al meteo nella Fase 1 del round successivo a quello in cui è giocata. |

## 7. Azioni della Fase 2

### 7.1 Porti

Stesso set di azioni su ogni isola. Zona franca: nessun combattimento.

| ID | Azione | Effetto |
|---|---|---|
| R-090 | Saccheggio | Tira 1d8 e ottieni quel numero di monete. |
| R-091 | Svago | Paga 3 monete. La nave resta in porto per il round successivo (R-045) e in quel round, in Fase 2, gioca di nuovo un turno di porto. |
| R-092 | Reclutamento | Paga 4 monete: pesca 4 crew, tienine 1. Oppure paga 8: pesca 4, tienine 2. Le carte tenute si ricevono secondo R-020; le altre vanno negli scarti Crew. |
| R-093 | Missione | Pesca 3 missioni Corsaro, tienine almeno 1, scarta le altre. |
| R-094 | Commercio | Vendi una carta crew (da qualsiasi slot) per il suo valore di Commercio (R-014) in monete. |
| R-095 | Cuoco di bordo (rango 4) sopra coperta: −1 moneta sul costo di ogni azione di porto a pagamento, per copia (costo minimo 0). |
| R-096 | Un'azione a pagamento si può scegliere solo se si hanno le monete per il costo effettivo. |

### 7.2 Attacco navale

| ID | Regola |
|---|---|
| R-100 | Gittata: distanza di Chebyshev (`05_mappa.md`), nessun blocco da ostacoli. Base 1. Sopra coperta: Falconet (J) +1, Saker (Q) +1, Culverin (K) +2, per copia (R-013, R-015). |
| R-101 | Si può attaccare una nave avversaria in **mare** entro la gittata. Non si possono attaccare né si può attaccare da isole o cornice. |
| R-102 | L'attacco si apre con **Bordata!** oppure con **Arrembaggio!**. |
| R-103 | **Duello di Bordate**: l'attaccante gioca Bordata!. Il difensore risponde con Parlè! oppure perde. Se risponde, l'attaccante deve giocare un'altra Bordata! oppure perde; e così via, botta e risposta. Chi non risponde (o non può) perde la battaglia. |
| R-104 | **Vincitore**: +1 segnalino taglia e 1d8 monete prese dal perdente (al massimo le monete che il perdente possiede). |
| R-105 | **Cannoniere** (rango 7) sopra coperta: in ogni combattimento il proprietario ha una Bordata! gratuita in più (per copia), che non consuma carte. **[DEFAULT]** sull'interpretazione di "gratuito e riutilizzabile ogni combattimento": una Bordata gratuita per combattimento, che si rinnova al combattimento successivo. |
| R-106 | **Falconet** (J) sopra coperta dell'attaccante: ogni scontro che inizia diventa **Duello obbligatorio**: il primo colpo dell'attaccante è gratuito, poi i due si scambiano Bordata! dalla mano (il difensore risponde con Bordata!, non con Parlè!). Chi non risponde perde. |
| R-107 | **Saker** (Q) sopra coperta del vincitore: se vince con Bordata! contro un difensore che non ha giocato Parlè! in quel combattimento, il guadagno raddoppia (2 segnalini e 2×1d8 monete, sempre limitate alle monete del perdente). |
| R-108 | **Culverin** (K) sopra coperta dell'attaccante: un attacco portato da 2 o più celle di distanza toglie al difensore la possibilità di giocare Parlè!. |
| R-109 | **Nostromo** (9) sopra coperta del vincitore: ruba 1 carta Pirateria a caso dalla mano del perdente. |
| R-110 | **Arrembaggio!** come apertura: il difensore può annullare l'effetto giocando Parlè! oppure pagando 5 monete. Altrimenti l'attaccante prende 1 carta crew sopra coperta del difensore (a scelta dell'attaccante) e la riceve secondo R-020. **[DEFAULT]**: Arrembaggio! non è una "battaglia vinta" (nessun segnalino, nessuna moneta) e non apre un botta e risposta. Il Medico del difensore può bloccarlo (R-023). |
| R-111 | **Quartiermastro** (10) sopra coperta: in alternativa a un attacco (usa l'azione di attacco del turno), il giocatore scarta una Bordata! dalla mano e scambia una propria carta sopra coperta con una carta sopra coperta di una nave avversaria entro la gittata. In quel turno non può iniziare una battaglia. |
| R-112 | **[DEFAULT]**: le carte giocate durante un attacco (Bordata!, Parlè!, Arrembaggio!) non contano come "la carta Pirateria del turno" (R-056). |

### 7.3 Carte Pirateria giocate nel turno

Una carta per turno (R-056); il **Bucaniere** (rango 2) sopra coperta permette 1 carta in più per copia. Effetti in `03_contenuti.md`.

| ID | Regola |
|---|---|
| R-120 | Uomo in mare! e Spyglass! rispettano la gittata della propria nave (R-100) e si giocano solo su una nave avversaria in mare. Nessuna difesa. |
| R-121 | Le carte Meteo si giocano da qualsiasi posizione e possono bersagliare qualsiasi zona. Il costo in monete va nel Tesoro. |
| R-122 | **Cuoco di bordo**: +1 alla quantità ottenuta da ogni "carta pesca" giocata nel turno, per copia (Pesca Fortunata!: +1 moneta; Rete a Strascico!: +1 carta). |
| R-123 | Bordata! e Parlè! non si giocano fuori da un combattimento. |

## 8. Missioni Corsaro

| ID | Regola |
|---|---|
| R-130 | Le missioni in mano sono segrete. |
| R-131 | **[DEFAULT]** Una missione conta solo eventi successivi al momento in cui il giocatore l'ha pescata (unica eccezione: Cacciatore di Taglie, che conta tutta la partita). |
| R-132 | Le missioni con condizione "raggiungi/fai X" si completano nel momento in cui la condizione è vera: la carta viene rivelata e il giocatore riceve subito la ricompensa in segnalini. Le missioni con condizione "mai" (Olandese Volante) si valutano solo a fine partita. Condizioni precise: `03_contenuti.md`. |
| R-133 | A fine partita ogni missione in mano non completata vale −1 segnalino. |

## 9. Fine partita e punteggio

| ID | Regola |
|---|---|
| R-140 | La partita finisce quando una nave entra in una cella dell'Isola Sacra durante il movimento. Quella nave prende il **Tesoro**: 1 segnalino taglia più tutte le monete del Tesoro (offerte e costi delle carte Meteo). |
| R-141 | Se più navi entrano nell'Isola Sacra nello stesso round, prende il Tesoro chi ci entra nel passo di movimento più basso; a parità, chi viene prima nell'ordine turno del round **[DEFAULT]**. |
| R-142 | Si completa il round in corso (round di cortesia: la Fase 2 di chi non ha ancora giocato). Poi il conteggio. |
| R-143 | Taglia finale = segnalini (battaglie, missioni, Tesoro) + 1 per ogni 10 monete possedute (per difetto) − 1 per ogni missione non completata + punteggio poker della ciurma. |
| R-144 | **Punteggio poker**: miglior combinazione formata dalle carte crew nei 5 slot (sopra e sotto coperta). Tabella in `03_contenuti.md`. Con meno di 5 carte si valutano le combinazioni possibili (scala e colore richiedono 5 carte). |
| R-145 | **Jolly a fine partita**: entrambi i Jolly nella ciurma → punteggio poker fisso 4, si ignora la tabella. Un solo Jolly → fa da carta jolly per la miglior combinazione, e il punteggio risultante si dimezza per difetto. |
| R-146 | **Nostromo** sopra coperta a fine partita: raddoppia il punteggio poker (dopo l'eventuale dimezzamento del Jolly), per copia. |
| R-147 | Vince la taglia più alta. In caso di parità tra un giocatore e chi ha raggiunto l'Isola Sacra, vince chi l'ha raggiunta. Negli altri casi la parità resta. |

## 10. Limiti di sicurezza del prototipo

| ID | Regola |
|---|---|
| R-150 | Il motore accetta un numero massimo di round configurabile (`maxRounds`, 0 = nessun limite). Serve solo alle simulazioni automatiche per rilevare partite che non finiscono; in partita normale è 0. |
