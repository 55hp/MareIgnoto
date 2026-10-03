# 04 — Motore a decisioni (assembly Rules)

Contratto pubblico del motore. I nomi indicati sono vincolanti perché li usano presentazione, tutorial e test; la struttura interna (come avanzano le fasi, come sono organizzati i risolutori degli effetti) la decide Claude Code.

## 1. Sessione

```csharp
public sealed class GameSession
{
    public static GameSession Start(GameSetup setup, RulesConfig config, MapLayout map, IRandomSource random);

    public IReadOnlyGameState State { get; }      // stato in sola lettura
    public PendingDecision Pending { get; }       // null solo a partita finita
    public bool IsOver { get; }
    public GameResult Result { get; }             // null finché IsOver è false

    // Valida la risposta contro Pending; se non valida lancia InvalidDecisionException e lo stato non cambia.
    // Se valida, avanza il gioco fino alla prossima decisione che richiede un giocatore e restituisce
    // gli eventi prodotti, in ordine.
    public IReadOnlyList<GameEvent> Submit(DecisionAnswer answer);
}
```

- `GameSetup`: numero di giocatori, nome e posto di ognuno, seed, e opzioni per il tutorial (ordine predefinito dei mazzi, stato iniziale di vento e zone).
- Anche `Start` produce eventi (setup, pescate): disponibili come `InitialEvents`.
- **Determinismo**: stesso setup + stesso seed + stessa sequenza di risposte ⇒ stessa partita, eventi identici. È un requisito testato.

## 2. Decisioni

`PendingDecision` dice **chi** deve decidere, **cosa**, con **quali opzioni legali**, e se la decisione è **segreta** (gli altri giocatori non devono vedere lo schermo: hot-seat).

Il motore chiede una decisione solo quando c'è una scelta reale. Se l'unica opzione legale è una, la applica da solo (esempio: perdere una crew avendone una sola), salvo le decisioni segrete di Fase 1, che vanno sempre confermate dal giocatore.

Tipi di decisione minimi (nomi indicativi, Claude Code può raggrupparli):

| Decisione | Proprietario | Segreta | Regole |
|---|---|---|---|
| Tenere missioni (pesca 3, tieni ≥1) | giocatore | sì | R-034, R-093 |
| Posizionare crew iniziali sotto coperta | giocatore | sì | R-032 |
| Offerta a Gartya | giocatore | sì | R-036 |
| Scegliere la rotta | giocatore | sì | R-040 |
| Scegliere carta Pirateria da perdere (meteo) | giocatore | sì | R-083 |
| Scegliere crew sotto coperta da perdere (Tempesta) | giocatore | sì | R-084 |
| Timoniere: scegliere rotazione / cella di riposizionamento | giocatore | no | R-086, R-073b |
| Abbordaggio: 1 crew o 2 Pirateria, e quali | giocatore | sì | R-070, R-071 |
| Cella dell'attraversamento | giocatore con valore più basso | no | R-067 |
| Sacrificare il Medico? | proprietario del Medico | sì | R-023 |
| Ricevere una crew: slot / sostituisci / scarta | ricevente | sì | R-020 |
| Turno in mare: pesca 2, oppure azione (swap, attacco, carta, fine turno) | giocatore di turno | sì | R-056 |
| Turno in porto: azione di porto (+ Vedetta: porto o mare) | giocatore di turno | sì | R-054, R-058 |
| Reclutamento: quali crew tenere | giocatore | sì | R-092 |
| Bersaglio e parametri di una carta (zona, avversario, slot, direzione) | chi gioca | no | §7.3 |
| Risposta in combattimento: Parlè!/Bordata!/arrendersi, paga 5 per Arrembaggio! | difensore o attaccante | sì | R-103–R-110 |
| Scelta della crew da rubare con Arrembaggio! | attaccante | no | R-110 |

Una risposta deve sempre poter essere verificata contro `Pending` (le opzioni legali sono enumerabili o hanno un validatore). Il bot casuale (`08`) sceglie tra le opzioni legali: se una decisione non permette di enumerare le opzioni, il bot non può giocarla. Quindi **ogni decisione deve esporre le sue opzioni legali**.

## 3. Eventi

`GameEvent` è un record immutabile di qualcosa che è successo, nell'ordine in cui è successo: mazzo mescolato, carta pescata, monete cambiate, rotte rivelate, meteo applicato, nave mossa di una cella, collisione, dado tirato, carta giocata, battaglia iniziata/risposta/vinta, missione completata, zona cambiata, vento cambiato, fine partita, punteggio finale.

- Ogni evento ha una **visibilità**: pubblico, oppure privato per un giocatore (esempio: la carta pescata è visibile solo a chi pesca; gli altri vedono "ha pescato 1 carta").
- La presentazione riproduce gli eventi in sequenza; il log di partita (`06`) li trasforma in testo.
- Il movimento produce un evento per passo, così la vista può animare le collisioni cella per cella.

## 4. Casualità

```csharp
public interface IRandomSource
{
    int RollD8();                 // 1..8
    int Range(int minInclusive, int maxExclusive);
    void Shuffle<T>(IList<T> list);
}
```

- Implementazione standard: generatore seedato scritto nel motore (non `System.Random` di sistema né `UnityEngine.Random`), così il risultato è identico in Unity e nell'harness.
- Ogni tiro di dado produce un evento con il risultato.
- Il tutorial usa una sorgente che restituisce tiri prestabiliti (e, quando la lista finisce, prosegue con il generatore seedato).

## 5. Stato

`IReadOnlyGameState` espone almeno: round, fase, ordine di turno corrente, giocatore attivo, vento, stato delle zone, Tesoro, per ogni giocatore (posizione nave, monete, segnalini, slot ciurma, numero carte in mano, missioni in mano e completate, rotta scelta se rivelata), dimensioni dei mazzi e degli scarti.

Le informazioni segrete (carte sotto coperta, mano, missioni) si leggono attraverso un metodo che riceve il punto di vista (`ViewFor(playerId)`), così la UI non mostra per errore informazioni di altri giocatori.

## 6. Invarianti (verificati dai test e dalla simulazione)

- Conservazione delle carte: per ogni mazzo, carte nel mazzo + scarti + mani + slot + missioni completate = totale iniziale.
- Monete mai negative; monete totali in gioco + Tesoro cambiano solo per effetti che creano o distruggono monete (Saccheggio, Mozzo, Pesca Fortunata!, Commercio creano; costi pagati vanno al Tesoro o spariscono come da regola).
- Mai due navi sulla stessa cella di mare a inizio Fase 2, salvo il caso R-071 / R-073a.
- La Fase 2 di ogni round dà un turno a ogni giocatore esattamente una volta.
- Il Nostromo, una volta sopra coperta, torna sotto solo per l'intervento del Medico (R-016, R-023): nessun altro effetto, né lo swap del proprietario, lo sposta sotto.
