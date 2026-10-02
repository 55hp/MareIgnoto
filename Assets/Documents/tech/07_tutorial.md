# 07 — Tutorial

## 1. Obiettivo

Un giocatore che non conosce il gioco, in circa 10 minuti, deve aver fatto almeno una volta: scelta missioni, offerta a Gartya, scelta rotta con il vento, subire il meteo, azione di porto, swap della ciurma, carta Meteo, battaglia con botta e risposta, Abbordaggio fortuito, completamento di una missione, arrivo all'Isola Sacra e lettura del punteggio.

## 2. Struttura

- Partita reale del motore con `GameSetup` speciale (`04_motore.md` §1): 2 giocatori, seed fisso, **ordine dei mazzi prestabilito**, tiri di dado prestabiliti, posizioni iniziali, vento e zone impostati dallo scenario.
- Avversario: "Capitan Barbagrigia", che risponde con una lista di risposte predefinite. Se la lista finisce, risponde come il bot casuale.
- `TutorialDirector` (Unity) segue una lista di **passi**. Ogni passo ha:
  - `trigger`: la decisione in attesa o l'evento che lo attiva;
  - `text`: testo del fumetto (italiano);
  - `highlight`: cosa evidenziare (nome di un oggetto UI del contratto `06` §2, oppure celle della mappa);
  - `allowedAnswers`: filtro sulle opzioni legali (vuoto = tutte), per guidare il giocatore;
  - `advance`: quando il passo si chiude (risposta data, evento visto, pulsante "Avanti").
- Lo scenario e i passi stanno in `TutorialScenarioAsset` (ScriptableObject). Il testo lo può rifinire Franci dall'Inspector senza toccare il codice.
- Il motore **non sa** di essere in tutorial: il filtro agisce solo sulla UI (le opzioni non permesse sono disattivate con il motivo nel tooltip).
- Il tutorial si può abbandonare in qualsiasi momento (ritorno al menu).

## 3. Scenario proposto [DEFAULT, da rifinire con Franci]

- Mappa standard. Posizioni iniziali sovrascritte dallo scenario: le due navi partono a 3–4 celle dall'Isola Sacra, così la partita dura pochi round.
- Vento iniziale verso l'Isola Sacra rispetto al giocatore (rotta "a favore" disponibile al primo round).
- Una zona davanti al giocatore in Mare Mosso.
- Mano iniziale del giocatore: Bordata!, Parlè!, Invocazione di Gartya. Crew iniziali: Timoniere e Mozzo. Missioni pescate: Avido, Barbarossa!, Lupo di mare (il giocatore ne tiene almeno 1; il passo consiglia Avido).
- Avversario: crew e mano scelte per perdere il primo duello (ha 1 Parlè! contro 2 Bordata! del giocatore).

## 4. Passi (bozza dei testi)

| # | Trigger | Testo | Highlight | Filtro |
|---|---|---|---|---|
| 1 | Inizio | "Benvenuto, capitano. Vince chi a fine partita ha la **taglia** più alta. Ti mostro come si guadagna." | — | — |
| 2 | Decisione: tieni missioni | "Queste sono missioni segrete del Corsaro. Tienine almeno una: completarle dà taglia, lasciarle incomplete la toglie." | PANEL_ChooseCards | — |
| 3 | Decisione: crew iniziali | "La tua ciurma ha 2 posti sopra coperta (effetti attivi, visibili a tutti) e 3 sotto (nascosti, senza effetto). Ora vanno sotto." | UI_Crew | — |
| 4 | Decisione: offerta | "Offri monete a Gartya, la balena divinità. Chi offre di più gioca per primo nel primo round. Le offerte finiscono nel tesoro dell'Isola Sacra." | PANEL_Offer | — |
| 5 | Decisione: rotta (round 1) | "Scegli la rotta in segreto. Il **vento** dominante ti dà +1 se lo segui, −1 se lo contrasti." | PANEL_Heading, WindRoseView | solo la rotta a favore |
| 6 | Evento: meteo applicato | "Eri in una zona di **Mare Mosso**: la rotta gira di quanto dice il dado e perdi una carta. Le zone non si calmano da sole." | BOARD_Zones | — |
| 7 | Evento: movimento | "Le navi si muovono insieme, una cella alla volta." | BOARD_Ships | — |
| 8 | Decisione: turno in mare | "Nel tuo turno puoi pescare 2 carte e basta, oppure fare uno swap, un attacco e giocare una carta." | PANEL_TurnActions | — |
| 9 | Decisione: swap | "Porta il **Timoniere** sopra coperta: +1 velocità e scegli tu i dadi del meteo." | UI_Crew | solo lo swap del Timoniere |
| 10 | Decisione: carta del turno | "Le carte di **Gartya** cambiano il meteo, ovunque sulla mappa. Metti Mare Mosso sulla zona del tuo avversario." | UI_Hand, BOARD_Zones | solo Invocazione sulla zona indicata |
| 11 | Decisione: attacco (round successivo) | "Il tuo avversario è a tiro. Apri con **Bordata!**: lui deve rispondere con Parlè!, poi tocca a te, finché qualcuno non può più rispondere." | PANEL_Combat | solo Bordata! |
| 12 | Evento: battaglia vinta | "Vittoria: +1 segnalino taglia e monete prese a lui." | UI_PlayerStrip | — |
| 13 | Evento: collisione | "Due navi sulla stessa cella: **Abbordaggio fortuito**. Ognuno perde qualcosa e viene respinto." | BOARD_Ships | — |
| 14 | Decisione: turno in porto | "Sei in porto: qui si fa una sola azione. Prova il **Saccheggio**." | PANEL_TurnActions | solo Saccheggio |
| 15 | Evento: missione completata | "Missione completata: la ricompensa arriva subito." | UI_Missions | — |
| 16 | Evento: Isola Sacra raggiunta | "Chi raggiunge l'Isola Sacra prende il tesoro e chiude la partita, ma vince comunque la taglia più alta." | BOARD_Tiles | — |
| 17 | Evento: punteggio | "La taglia: segnalini, 1 ogni 10 monete, missioni incomplete in meno, e il poker della tua ciurma." | UI_EndScreen | — |

La sequenza esatta di round, dadi e mazzi che fa accadere questi passi in quest'ordine si costruisce nella spec 0008, provando lo scenario nell'harness (un test fa giocare l'intero tutorial con le risposte "corrette" e verifica che ogni passo scatti).
