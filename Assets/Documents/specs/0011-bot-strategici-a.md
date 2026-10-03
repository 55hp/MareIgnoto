# 0011 — Bot strategici, parte A: infrastruttura, Rush e Cacciatore

Stato: pronta per Claude Code
Commit di riferimento: da compilare
Regole coinvolte: nessuna regola del gioco cambia
Documenti: `tech/09_bot.md` (nuovo, §A1), `tech/04_motore.md`, `tech/08_test-e-simulazione.md`

Un file per task. Sopra `## Report` scrivono Athena (intestazione, Obiettivo, Parte A) e Claude Code (Parte B). Sotto `## Report` ogni attore aggiunge voci in coda.

## Obiettivo

Avere bot che giocano con una strategia, usando solo ciò che un giocatore vede (`PlayerView`), definiti da dati (`BotProfile`): il **Rush** (rosso) e il **Cacciatore** (giallo). Servono a misurare la durata di una partita in cui qualcuno punta dritto all'Isola Sacra, a bilanciare, e come avversari nel primo playtest. Verde e blu seguono in una spec successiva.

## Parte A — Patch di documentazione (testo esatto)

Regole di applicazione (`CLAUDE.md`, "Patch di documentazione da Athena"): applica parola per parola, senza riformulare. Se un file, una riga o una sottostringa non esiste o è diversa da come la descrivo, fermati e riportalo. Un commit a parte "docs: patch Athena (bot strategici)", solo documenti. Mostra `git diff --stat`.

### A1. Nuovo file `Assets/Documents/tech/09_bot.md`

Crea il file con esattamente il testo tra i marcatori (marcatori esclusi). Se esiste già, fermati e riportalo.

<<<INIZIO 09_bot.md>>>
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
<<<FINE 09_bot.md>>>

### A2. `tech/00_INDICE.md`

Inserisci come riga NUOVA nella tabella dei file, subito dopo la riga la cui prima cella è `08_test-e-simulazione.md`:

| `09_bot.md` | Bot strategici: principi, strumenti comuni, profili (rosso, giallo; verde e blu da specificare), misure | Solo Athena/Franci |

Aggiungi in fondo alla tabella "Registro modifiche":

| 03/10/2026 (5) | **`09`** nuovo: bot strategici basati su `PlayerView`, profili Rush e Cacciatore, scenari di misura | `09` |

## Parte B — Lato codice (Claude Code)

Leggi prima `tech/09_bot.md` e `tech/04_motore.md`. Il motore e le regole non cambiano.

1. **Struttura** (`Assets/Game/Rules/Bots/`): `IBot` (`Choose(PendingDecision, PlayerView)`), con `RandomBot` che la implementa senza cambiare comportamento; `BotProfile` (dati immutabili); `StrategicBot : IBot`; i profili `Rush` e `Cacciatore` come dati. Gli strumenti comuni di `09` §3 sono funzioni pure (velocità stimata, scelta della rotta, stima di punteggio, bersaglio, scambio crew). Il bot non deve poter leggere altro che `PlayerView` e la decisione.
2. **Politiche**: implementa le tabelle di `09` §4 per ogni `DecisionKind`; per le altre ricadi sul casuale. Risposte sempre legali. Casualità propria del bot: sorgente separata da quella della partita, seed derivato da seed della partita e posto. A parità si sceglie a caso.
3. **Test** (Rules): ogni strumento comune da solo (velocità con vento e Timoniere, scelta della rotta su mappa libera e con Tempesta sulla via, stima di punteggio e confronto stretto/pari); Rush: offre tutte le monete, non entra nell'Isola Sacra quando "proprio" è strettamente minore di "altrui" e ci entra a parità; Cacciatore: offre 0, attacca quando c'è un bersaglio entro la gittata; determinismo (stesso seed, stessa partita); partite complete con posti misti (profili e casuali) senza eccezioni né violazioni di invarianti.
4. **Simulazione**: `SimulationRunner` accetta una configurazione per posto (bot casuale o profilo) e, per gli scenari, fa ruotare con il seed il posto dei profili. Aggiungi il riepilogo di `09` §5. Un test `[Explicit]` esegue gli scenari A, B, C e D (4 giocatori, 500 seed ciascuno) e stampa il riepilogo. Incolla il riepilogo completo nel report.
5. **Unity** (`hp55games.MareIgnoto.Unity`): in `GameBootstrap` una lista per posto (tipo di bot: Casuale, Rush, Cacciatore) con valore di partenza [Rush, Casuale, Cacciatore, Casuale] per 4 giocatori, come nuovo campo con inizializzatore (nessun collegamento di scena da fare); `GameFlowController` usa un bot per posto; i nomi dei bot vengono da `UiText` (per esempio "Bot Rush", "Bot Cacciatore", "Bot casuale 2"). Non toccare scene, prefab o asset.
6. **Gate**: rilancia il gate da 1000 seed (2/4/8 giocatori, bot casuali) e una simulazione da 1000 seed con posti misti: puliti.

Se una regola non è chiara, non inventare: lascia un TODO e scrivi la domanda sotto "#### Domande".

## Lato Editor (Bezi) — checklist

Nessuna in questo task.

## Per Franci — modifiche isolate

- Dopo il task: aprire l'Editor per i `.meta` dei file nuovi, controllare la Console (codice non compilabile fuori da Unity) e lanciare il Test Runner EditMode. Poi committare i `.meta`.

## Verifica

- Parte A: `git diff --stat` mostra solo documenti.
- Parte B: `dotnet test Tools/RulesHarness` verde; gate e simulazione mista puliti; riepilogo degli scenari A–D nel report.

## Report

<!-- Voci in coda. Intestazione: ### Claude Code — AAAA-MM-GG — commit abc1234 -->
