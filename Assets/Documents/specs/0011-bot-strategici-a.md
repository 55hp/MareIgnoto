# 0011 — Bot strategici, parte A: infrastruttura, Rush e Cacciatore

Stato: pronta per Claude Code
Commit di riferimento: `3c014b2`
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

### Claude Code — 2026-10-03 — commit 3c014b2

Parte A applicata parola per parola in un commit a parte (`d34fd6d`, "docs: patch Athena (bot strategici)"): `tech/09_bot.md` creato (non esisteva), riga della tabella dopo `08_test-e-simulazione.md` e riga del registro aggiunte.

**Parte B**
1. **Struttura** (`Assets/Game/Rules/Bots/`): `IBot.Choose(PendingDecision, PlayerView)`; `RandomBot` la implementa con lo stesso comportamento (il gate dei soli casuali dà esattamente i numeri della spec 0010). `BotProfile`: dati immutabili (proprietà a sola lettura), con `BotProfile.Rush` ("Rush") e `BotProfile.Hunter` ("Cacciatore"); i numeri del profilo (8 monete per il riscatto, 4 per reclutare, penalità di rotta 4 e 1) stanno lì. `StrategicBot : IBot` applica le politiche; `BotTools` contiene gli strumenti comuni di `09` §3 come funzioni pure. Il bot riceve solo la decisione e il proprio `PlayerView` (stato pubblico più mano, missioni e sotto coperta propri). Velocità e gittata usano la stessa formula del motore: `MovementFlow.Speed` e `CombatFlow.Range` hanno un sovraccarico con soli dati pubblici, usato sia dal motore sia dai bot.
2. **Politiche**: le tabelle di `09` §4 per offerta, missioni, rotta (con la modalità cauta del Rush), perdite per meteo e per Uomo in mare!, turno in mare, porto, Vedetta, botta e risposta, difesa da Arrembaggio!, crew ricevute, bersagli delle carte. Le altre decisioni sono a caso. Casualità propria del bot: `SeededRandom(StrategicBot.SeedFor(seed, posto))`.
3. **Test** (`BotTests`, 14 nuovi): velocità stimata (vento, Timoniere col Jolly, partenza da porto), movimento simulato, rotta su mare libero e con Tempesta o Mare Mosso sulla cella d'arrivo, stima di punteggio (solo ciò che si vede), Rush che entra a parità ed evita l'Isola Sacra se strettamente dietro, offerte di Rush e Cacciatore, Rush che non attacca né gioca carte, Cacciatore che attacca il bersaglio a tiro e senza bersaglio pesca, swap utile, determinismo, partite intere con posti misti. Verificato che mordono: senza modalità cauta, con confronto non stretto, con il Cacciatore che non attacca, senza penalità meteo, con le offerte invertite (1 rosso ciascuno). `dotnet test Tools/RulesHarness`: 427 verdi.
4. **Simulazione**: `SimulationOptions.SeatProfiles` (un profilo per posto, null = casuale; con N giocatori si usano i primi N, ruotati col seed) e `Name`; il report aggiunge battaglie e Abbordaggi per partita, posti, vittorie (pari merito divisi) e fonti di punteggio per profilo, ingressi del Rush nell'Isola Sacra e vittorie dopo esserci entrato. Test `[Explicit]`: `BotScenarios` (A–D, 4 giocatori, 500 seed) e `MixedSeats1000Seeds`.
5. **Unity**: `GameBootstrap` ha `seats` (lista di `SeatBot`: Random, Rush, Hunter) con valore di partenza [Rush, Random, Hunter, Random]; i posti oltre la lista giocano col casuale. `GameFlowController` riceve un bot per posto e passa a ciascuno il suo `PlayerView`. Nomi da `UiText.BotNames`: "Bot Rush", "Bot casuale 2", "Bot Cacciatore", "Bot casuale 4" (con il numero del posto anche sui profili ripetuti). Nessuna scena, prefab o asset toccati.
6. **Gate**: seed 1–1000 × 2/4/8 con soli bot casuali: pulito, identico alla spec 0010 (interrotte 188 / 32 / 2). Posti misti, seed 1–1000 × 2/4/8: pulito.

**Scenari di `09` §5** (4 giocatori, seed 1–500, `maxRounds` 500, layout v5):

```
Scenario A — Rush + 3 casuali
Simulazione: seed 1-500, maxRounds 500, posti: Rush, casuale, casuale, casuale (con N giocatori i primi N, ruotati col seed)

4 giocatori: 500 partite, 497 finite con l'Isola Sacra, 3 interrotte da maxRounds, 0 errori
  round (partite finite): min 4, media 14.65, mediana 13, max 96
  interrotte da maxRounds, seed: 46, 134, 152
  taglia media per giocatore: 0.11 = battaglie 0.01 + missioni 0.37 + Tesoro 0.25 + monete 0.82 - missioni incomplete 1.40 + poker 0.07
  quota delle fonti positive: battaglie 0.9%, missioni 24.2%, Tesoro 16.4%, monete 54.0%, poker 4.6%
  missioni completate per partita: 0.72; vincitore: taglia media 3.30, ha preso il Tesoro nel 95.6% delle partite; parità al primo posto: 0
  poker: HighCard 94.0%, Pair 6.0%
  battaglie per partita: 0.05; Abbordaggi per partita: 0.09
  casuale: 1500 posti, vittorie 22.00 (4.4% delle partite); taglia media -0.92 = battaglie 0.02 + missioni 0.40 + Tesoro 0.00 + monete 0.18 - incomplete 1.59 + poker 0.07
  Rush: 500 posti, vittorie 478.00 (95.6% delle partite); taglia media 3.21 = battaglie 0.01 + missioni 0.26 + Tesoro 0.98 + monete 2.72 - incomplete 0.82 + poker 0.06
  Rush nell'Isola Sacra: in 492 partite (98.4%); vittorie dopo esserci entrato: 475.00 (96.5%)

Errori: nessuno

Scenario B — Cacciatore + 3 casuali
Simulazione: seed 1-500, maxRounds 500, posti: Cacciatore, casuale, casuale, casuale (con N giocatori i primi N, ruotati col seed)

4 giocatori: 500 partite, 449 finite con l'Isola Sacra, 51 interrotte da maxRounds, 0 errori
  round (partite finite): min 6, media 157.45, mediana 111, max 494
  interrotte da maxRounds, seed: 9, 10, 23, 39, 45, 49, 51, 97, 107, 108, 117, 126, 132, 137, 168, 178, 180, 184, 190, 202, 208, 213, 221, 227, 257, 269, 279, 283, 285, 288, 293, 325, 326, 346, 349, 360, 368, 373, 382, 386, 389, 392, 395, 428, 429, 446, 458, 474, 482, 485, 494
  taglia media per giocatore: 15.37 = battaglie 14.11 + missioni 1.37 + Tesoro 0.23 + monete 1.62 - missioni incomplete 1.99 + poker 0.04
  quota delle fonti positive: battaglie 81.3%, missioni 7.9%, Tesoro 1.3%, monete 9.3%, poker 0.2%
  missioni completate per partita: 2.75; vincitore: taglia media 52.55, ha preso il Tesoro nel 24.0% delle partite; parità al primo posto: 0
  poker: HighCard 97.5%, Pair 2.5%
  battaglie per partita: 52.92; Abbordaggi per partita: 25.04
  Cacciatore: 500 posti, vittorie 484.00 (96.8% delle partite); taglia media 52.48 = battaglie 49.43 + missioni 0.69 + Tesoro 0.22 + monete 2.74 - incomplete 0.68 + poker 0.09
  casuale: 1500 posti, vittorie 16.00 (3.2% delle partite); taglia media 3.00 = battaglie 2.33 + missioni 1.60 + Tesoro 0.23 + monete 1.24 - incomplete 2.43 + poker 0.02

Errori: nessuno

Scenario C — Rush + Cacciatore + 2 casuali
Simulazione: seed 1-500, maxRounds 500, posti: Rush, Cacciatore, casuale, casuale (con N giocatori i primi N, ruotati col seed)

4 giocatori: 500 partite, 493 finite con l'Isola Sacra, 7 interrotte da maxRounds, 0 errori
  round (partite finite): min 4, media 21.34, mediana 13, max 489
  interrotte da maxRounds, seed: 100, 102, 264, 304, 344, 408, 482
  taglia media per giocatore: 1.76 = battaglie 1.01 + missioni 0.35 + Tesoro 0.25 + monete 1.35 - missioni incomplete 1.26 + poker 0.06
  quota delle fonti positive: battaglie 33.5%, missioni 11.5%, Tesoro 8.2%, monete 44.7%, poker 2.1%
  missioni completate per partita: 0.69; vincitore: taglia media 6.83, ha preso il Tesoro nel 89.0% delle partite; parità al primo posto: 0
  poker: HighCard 94.6%, Pair 5.4%, ThreeOfAKind 0.1%
  battaglie per partita: 4.02; Abbordaggi per partita: 1.91
  Cacciatore: 500 posti, vittorie 95.00 (19.0% delle partite); taglia media 4.58 = battaglie 3.39 + missioni 0.20 + Tesoro 0.13 + monete 1.69 - incomplete 0.90 + poker 0.07
  casuale: 1000 posti, vittorie 21.00 (4.2% delle partite); taglia media -0.80 = battaglie 0.09 + missioni 0.47 + Tesoro 0.01 + monete 0.21 - incomplete 1.65 + poker 0.07
  Rush: 500 posti, vittorie 384.00 (76.8% delle partite); taglia media 4.06 = battaglie 0.48 + missioni 0.26 + Tesoro 0.83 + monete 3.28 - incomplete 0.84 + poker 0.05
  Rush nell'Isola Sacra: in 417 partite (83.4%); vittorie dopo esserci entrato: 383.00 (91.8%)

Errori: nessuno

Scenario D — 2 Rush + 2 casuali
Simulazione: seed 1-500, maxRounds 500, posti: Rush, Rush, casuale, casuale (con N giocatori i primi N, ruotati col seed)

4 giocatori: 500 partite, 500 finite con l'Isola Sacra, 0 interrotte da maxRounds, 0 errori
  round (partite finite): min 4, media 10.52, mediana 12, max 35
  taglia media per giocatore: 0.26 = battaglie 0.00 + missioni 0.31 + Tesoro 0.27 + monete 0.81 - missioni incomplete 1.20 + poker 0.06
  quota delle fonti positive: battaglie 0.0%, missioni 21.1%, Tesoro 18.5%, monete 55.9%, poker 4.4%
  missioni completate per partita: 0.65; vincitore: taglia media 3.32, ha preso il Tesoro nel 98.0% delle partite; parità al primo posto: 19
  poker: HighCard 94.1%, Pair 6.0%
  battaglie per partita: 0.00; Abbordaggi per partita: 0.00
  casuale: 1000 posti, vittorie 10.00 (2.0% delle partite); taglia media -0.93 = battaglie 0.00 + missioni 0.39 + Tesoro 0.00 + monete 0.15 - incomplete 1.54 + poker 0.08
  Rush: 1000 posti, vittorie 490.00 (98.0% delle partite); taglia media 1.44 = battaglie 0.00 + missioni 0.23 + Tesoro 0.54 + monete 1.47 - incomplete 0.85 + poker 0.05
  Rush nell'Isola Sacra: in 500 partite (100.0%); vittorie dopo esserci entrato: 490.00 (98.0%)

Errori: nessuno
```

**Posti misti** (gate, seed 1–1000 × 2/4/8):

```
Scenario posti misti
Simulazione: seed 1-1000, maxRounds 500, posti: Rush, Cacciatore, casuale, casuale, Rush, Cacciatore, casuale, casuale (con N giocatori i primi N, ruotati col seed)

2 giocatori: 1000 partite, 991 finite con l'Isola Sacra, 9 interrotte da maxRounds, 0 errori
  round (partite finite): min 4, media 12.19, mediana 12, max 288
  interrotte da maxRounds, seed: 58, 285, 450, 506, 698, 767, 824, 882, 981
  taglia media per giocatore: 2.31 = battaglie 0.84 + missioni 0.16 + Tesoro 0.51 + monete 1.66 - missioni incomplete 0.90 + poker 0.05
  quota delle fonti positive: battaglie 26.1%, missioni 5.1%, Tesoro 15.8%, monete 51.5%, poker 1.5%
  missioni completate per partita: 0.20; vincitore: taglia media 4.41, ha preso il Tesoro nel 95.6% delle partite; parità al primo posto: 2
  poker: HighCard 95.5%, Pair 4.5%
  battaglie per partita: 1.49; Abbordaggi per partita: 0.28
  Cacciatore: 1000 posti, vittorie 296.00 (29.6% delle partite); taglia media 3.32 = battaglie 1.52 + missioni 0.13 + Tesoro 0.26 + monete 2.29 - incomplete 0.93 + poker 0.05
  Rush: 1000 posti, vittorie 704.00 (70.4% delle partite); taglia media 1.30 = battaglie 0.16 + missioni 0.20 + Tesoro 0.75 + monete 1.03 - incomplete 0.87 + poker 0.05
  Rush nell'Isola Sacra: in 753 partite (75.3%); vittorie dopo esserci entrato: 701.00 (93.1%)

4 giocatori: 1000 partite, 987 finite con l'Isola Sacra, 13 interrotte da maxRounds, 0 errori
  round (partite finite): min 4, media 19.76, mediana 13, max 489
  interrotte da maxRounds, seed: 100, 102, 264, 304, 344, 408, 482, 674, 696, 706, 767, 798, 974
  taglia media per giocatore: 1.60 = battaglie 0.94 + missioni 0.33 + Tesoro 0.25 + monete 1.29 - missioni incomplete 1.26 + poker 0.06
  quota delle fonti positive: battaglie 32.7%, missioni 11.4%, Tesoro 8.7%, monete 45.2%, poker 2.0%
  missioni completate per partita: 0.66; vincitore: taglia media 6.62, ha preso il Tesoro nel 89.1% delle partite; parità al primo posto: 1
  poker: HighCard 94.9%, Pair 5.0%, ThreeOfAKind 0.1%
  battaglie per partita: 3.62; Abbordaggi per partita: 1.56
  Cacciatore: 1000 posti, vittorie 196.50 (19.7% delle partite); taglia media 4.46 = battaglie 3.21 + missioni 0.20 + Tesoro 0.14 + monete 1.75 - incomplete 0.90 + poker 0.06
  casuale: 2000 posti, vittorie 37.00 (3.7% delle partite); taglia media -0.83 = battaglie 0.06 + missioni 0.44 + Tesoro 0.01 + monete 0.23 - incomplete 1.64 + poker 0.06
  Rush: 1000 posti, vittorie 766.50 (76.7% delle partite); taglia media 3.62 = battaglie 0.41 + missioni 0.23 + Tesoro 0.83 + monete 2.97 - incomplete 0.85 + poker 0.04
  Rush nell'Isola Sacra: in 835 partite (83.5%); vittorie dopo esserci entrato: 764.50 (91.6%)

8 giocatori: 1000 partite, 1000 finite con l'Isola Sacra, 0 interrotte da maxRounds, 0 errori
  round (partite finite): min 4, media 16.46, mediana 10, max 276
  taglia media per giocatore: 0.88 = battaglie 0.64 + missioni 0.29 + Tesoro 0.13 + monete 1.00 - missioni incomplete 1.24 + poker 0.06
  quota delle fonti positive: battaglie 30.4%, missioni 13.5%, Tesoro 6.1%, monete 47.1%, poker 2.9%
  missioni completate per partita: 1.21; vincitore: taglia media 6.74, ha preso il Tesoro nel 90.1% delle partite; parità al primo posto: 11
  poker: HighCard 94.8%, Pair 5.2%, ThreeOfAKind 0.0%
  battaglie per partita: 4.65; Abbordaggi per partita: 4.61
  Cacciatore: 2000 posti, vittorie 101.00 (10.1% delle partite); taglia media 2.49 = battaglie 2.25 + missioni 0.18 + Tesoro 0.01 + monete 0.89 - incomplete 0.90 + poker 0.06
  casuale: 4000 posti, vittorie 11.00 (1.1% delle partite); taglia media -0.91 = battaglie 0.07 + missioni 0.36 + Tesoro 0.01 + monete 0.20 - incomplete 1.61 + poker 0.06
  Rush: 2000 posti, vittorie 888.00 (88.8% delle partite); taglia media 2.82 = battaglie 0.17 + missioni 0.25 + Tesoro 0.49 + monete 2.70 - incomplete 0.84 + poker 0.06
  Rush nell'Isola Sacra: in 945 partite (94.5%); vittorie dopo esserci entrato: 887.00 (93.9%)

Errori: nessuno
```

Lettura:
- **Durata con un Rush**: la partita dura molto meno, mediana 13 round (12 con due Rush), contro 123 con soli casuali. Il Rush entra nell'Isola Sacra nel 98% delle partite contro i casuali e vince il 96% delle volte che entra.
- **Il Cacciatore contro i casuali** fa in media 52 battaglie a partita e prende 49 segnalini di battaglia: i casuali non si difendono e le partite restano lunghe (mediana 111 round). È un dato da tenere d'occhio per il bilanciamento: le vittorie in battaglia non hanno un tetto.
- **Rush + Cacciatore**: il Rush vince il 77%, il Cacciatore il 19%.

**Non compilato in Unity.** Il codice Unity è compilato solo contro stub delle API (0 errori, 0 avvisi). Nella working copy sono comparsi i `.meta` dei file nuovi, quindi l'Editor ha già importato, ma non posso vedere la Console. Da controllare (Franci):
- nessun errore di compilazione in `hp55games.MareIgnoto.Rules`, `hp55games.MareIgnoto.Unity`, `hp55games.MareIgnoto.Rules.Tests`;
- su `/GAME_BOOTSTRAP` compare la lista `Seats` con Rush, Random, Hunter, Random (campo nuovo con inizializzatore; se la scena era già salvata, Unity tiene il valore di default, nessun collegamento da fare);
- Test Runner → EditMode: `BotTests` verdi;
- in Play: nel log i nomi "Bot Rush", "Bot casuale 2", "Bot Cacciatore", "Bot casuale 4"; il Rush va verso l'Isola Sacra e la partita finisce in pochi round;
- committare i `.meta` nuovi (`IBot`, `BotProfile`, `BotTools`, `StrategicBot`, `SeatBot`, `BotTests`, `tech/09_bot.md`).

#### Domande

- **Turno in mare, "swap utile, poi pesca 2"** (Rush e Cacciatore): R-056 non lo permette, perché pescare è l'opzione (A) del turno, solo come prima azione, e chiude il turno. Il bot fa lo swap utile e chiude, altrimenti pesca. In alternativa: pescare sempre e fare lo swap solo quando non si pesca (con la pesca lo swap non si fa mai), oppure lo swap solo se porta sopra una carta della priorità più alta. `// TODO 09 §4` in `StrategicBot.SeaAction`.
- **Carte Pirateria da perdere**: `09` dice "priorità più bassa" e, per il Rush, "la meno utile in difesa", ma non dà una lista. Ho messo nel profilo un ordine di carte da tenere. Rush: Parlè!, Vento in Poppa, Favore, Pesca Fortunata!, Rete a Strascico! (le altre si perdono prima). Cacciatore: Bordata!, Arrembaggio!, Uomo in mare!, Spyglass!, Parlè!, Pesca Fortunata!, Rete a Strascico!. Va bene?
- **Crew ricevuta in uno slot vuoto**: va sopra coperta se è nella priorità del profilo, altrimenti sotto.
- **Rotta del Cacciatore**: la meta è la nave avversaria in mare più vicina, a qualsiasi distanza (a parità quella più vicina all'Isola Sacra, poi a caso). La regola del bersaglio di `09` §3, "entro la gittata", la uso per attaccare. Senza navi in mare va al porto più vicino senza il proprio segnalino, come il Rush in modalità cauta, per non arrivare sull'isola del segnalino e saltare il turno (R-097).
- **Rotta**: la stima non tiene conto delle collisioni con le altre navi (non si sa dove andranno), né delle rotazioni del meteo.

#### Note

- Le statistiche sono di bot contro bot casuali: dicono come si comportano i profili, non quanto è bilanciato il gioco tra giocatori veri.
