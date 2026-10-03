# 09 — Bot strategici

Questo file è la fonte di verità del comportamento dei bot. Le regole del gioco sono in `02_regole.md`; il contratto del motore in `04_motore.md`.

## 1. Principi

- Un bot è un giocatore: riceve una `PendingDecision` e il `PlayerView` del proprio posto e risponde con una delle opzioni legali. Non legge altro: né lo stato interno del motore né le carte nascoste degli altri (R-130). Quindi i risultati delle simulazioni valgono anche per un giocatore umano.
- **Versione 1**: regole semplici, nessuna probabilità, nessuna ricerca oltre il round in corso. Una versione 2 più intelligente si innesta sulla stessa struttura.
- **Casi limite e parità**: si sceglie a caso tra le opzioni equivalenti, con la casualità propria del bot (separata da quella della partita, seed derivato dal seed della partita e dal posto). Non serve essere perfettamente deterministici nelle scelte equivalenti.
- Una decisione non coperta da un profilo si risolve come `RandomBot`: opzione legale a caso.
- Un bot non deve mai dare una risposta non valida né bloccare la partita.
- I profili hanno un nome descrittivo (Rush, Cacciatore, …). Il colore di un bot in Unity viene dal suo posto (`PlayerPalette`), non dal profilo.

## 2. Struttura

- `IBot`: `DecisionAnswer Choose(PendingDecision decision, PlayerView view)`. `RandomBot` la implementa.
- `BotProfile`: dati immutabili: nome, obiettivo, regola di offerta, ordine di priorità delle carte crew sopra coperta, soglie.
- `StrategicBot : IBot`: applica gli strumenti comuni di §3 e le politiche del profilo di §4; per il resto ricade sul casuale.
- Gli strumenti comuni sono funzioni pure, testabili da sole.

## 3. Strumenti comuni

**Velocità stimata** (R-060–R-064): base 1; vento +1 se la rotta coincide con la direzione del vento e −1 se è opposta (non vale se si parte da porto o cornice); +1 per ogni Timoniere sopra coperta, anche quello duplicato dal Jolly (R-015); minimo 0.

**Scelta della rotta** (R-040, R-065–R-066): per ognuna delle 8 direzioni si simula il movimento di `velocità` celle: ci si ferma su isola, Isola Sacra o cornice. Alla cella d'arrivo si dà un costo = distanza residua verso la meta (cammino minimo che evita isole e cornice) + una penalità se è in una zona di livello ≥ 2 (+4) o di livello 1 (+1). Si sceglie il costo minimo, a caso tra i pari. La meta dipende dal profilo.

**Stima di punteggio** (R-143), con ciò che si vede:
- *proprio, se prendo il Tesoro ora* = segnalini + 1 + (monete + Tesoro) ÷ 10 (per difetto) − missioni in mano + punteggio poker della propria ciurma (tutta nota);
- *altrui, minimo noto* = segnalini + monete ÷ 10 (per difetto) − missioni in mano + punteggio poker delle sole carte sopra coperta visibili. Le carte sotto coperta degli altri non si vedono e si ignorano: è un limite inferiore, aggiungere carte nascoste può solo aumentare.

Dato che a parità vince chi ha raggiunto l'Isola Sacra (R-147), il confronto che interessa è "proprio < altrui" in senso stretto.

**Bersaglio**: la nave avversaria in mare entro la gittata (R-100, R-101); a parità la più vicina all'Isola Sacra; poi a caso.

**Scambio crew** (R-057): si fa uno swap solo se porta sopra coperta una carta di priorità più alta di quella che esce, secondo l'ordine del profilo; una carta sotto coperta non ancora nota al bot non si usa. Le carte crew ricevute (R-020) vanno in uno slot vuoto a scelta; se sono pieni si sostituisce la carta di priorità più bassa, oppure si scarta quella in arrivo se è la più bassa.

## 4. Profili (versione 1)

### Rosso — Rush

Obiettivo: arrivare all'Isola Sacra il prima possibile. Offre tutte le monete per giocare per primo.

**Modalità cauta**: se "proprio, se prendo il Tesoro ora" è **strettamente minore** di "altrui, minimo noto" per almeno un avversario, non entra nell'Isola Sacra: la meta diventa il porto più vicino senza il proprio segnalino, e agli attracchi fa Saccheggio. Ad ogni round rivaluta e, se la condizione cade, riprende il rush.

| Decisione | Politica |
|---|---|
| Offerta a Gartya | Tutte le monete |
| Tenere missioni | Ne tiene una sola, a caso |
| Rotta | Meta: Isola Sacra (o porto in modalità cauta) |
| Perdite per meteo | Scarta la carta di priorità più bassa (Pirateria: la meno utile in difesa) |
| Turno in mare | Se c'è uno swap utile (priorità crew sotto) lo fa, poi pesca 2. Non gioca carte Pirateria né attacca |
| Turno in porto | Saccheggio |
| Vedetta | Sceglie il mare; con più isole a portata, a caso |
| Combattimento, difesa da Arrembaggio! | Difende con Parlè! se ce l'ha; altrimenti cede (nel duello) o, per l'Arrembaggio!, paga 5 monete se ne ha almeno 8, altrimenti subisce |
| Altre decisioni | A caso |

Priorità crew sopra coperta: Timoniere, Navigatore, Vedetta, Mozzo, Jolly.

### Giallo — Cacciatore

Obiettivo: attaccare le navi nemiche. Offre 0 monete.

| Decisione | Politica |
|---|---|
| Offerta a Gartya | 0 |
| Tenere missioni | Ne tiene una sola, a caso |
| Rotta | Meta: il bersaglio (§3); se non c'è nessuna nave in mare, il porto più vicino |
| Perdite per meteo | Scarta la carta di priorità più bassa |
| Turno in mare | Se c'è un bersaglio entro la gittata: attacca (apre con Bordata!, altrimenti con Arrembaggio!). Altrimenti swap utile, poi pesca 2 |
| Carte Pirateria | Gioca Uomo in mare! e Spyglass! sul bersaglio; non gioca carte Meteo |
| Turno in porto | Reclutamento se ha almeno 4 monete e non ha carte da combattimento sopra coperta; altrimenti Saccheggio |
| Vedetta | Sceglie il porto solo se non c'è un bersaglio entro la gittata |
| Combattimento | Continua il botta e risposta finché ha carte da giocare |
| Difesa da Arrembaggio! | Parlè! se ce l'ha, poi paga 5 se ne ha almeno 8, altrimenti subisce |
| Altre decisioni | A caso |

Priorità crew sopra coperta: Culverin (K), Falconet (J), Cannoniere, Saker (Q), Nostromo, Bucaniere.

### Previsti, da specificare nella spec successiva

- **Verde**: accumula carte Pirateria e le usa per ostacolare la nave più vicina all'Isola Sacra.
- **Blu**: punta prima a 5 carte crew e poi va dritto all'Isola Sacra; fa uno scambio crew solo se pesca il Nostromo e deve metterlo sotto coperta.
- Possibili aggiunte: un bot che punta sull'economia (monete, Commercio) e uno sulle missioni Corsaro, così ogni via di punteggio ha chi la sfrutta.

## 5. Misure

La simulazione (`08_test-e-simulazione.md` §3) accetta una configurazione per posto: bot casuale o profilo. Scenari con 4 giocatori (tutti agli angoli, equivalenti per simmetria); il posto dei profili ruota con il seed:

| Scenario | Posti |
|---|---|
| A | Rush + 3 casuali |
| B | Cacciatore + 3 casuali |
| C | Rush + Cacciatore + 2 casuali |
| D | 2 Rush + 2 casuali |

Per ogni scenario si riportano: partite finite con l'Isola Sacra e fermate da `maxRounds`; round di fine partita (minimo, mediana, massimo); quante volte vince ogni profilo (i pari merito si dividono); quante volte il Rush entra nell'Isola Sacra e quante volte vince dopo averlo fatto; numero medio di battaglie e di Abbordaggi per partita; fonti di punteggio per profilo.
