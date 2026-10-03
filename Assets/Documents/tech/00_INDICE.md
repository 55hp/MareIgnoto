# Documentazione tecnica — indice

| File | Contenuto | Chi lo modifica |
|---|---|---|
| `01_architettura.md` | Assembly, cartelle, config, flusso runtime, scope | Athena/Franci; Claude Code solo per allinearlo al codice reale |
| `02_regole.md` | Regolamento completo con ID `R-xxx` — per gli agenti è il GDD | Solo Athena/Franci |
| `03_contenuti.md` | Carte, missioni, tabella poker, valori di default | Solo Athena/Franci (i numeri si bilanciano poi in `RulesConfig.asset`) |
| `04_motore.md` | Contratto pubblico del motore (sessione, decisioni, eventi, casualità) | Athena/Franci; Claude Code può estenderlo se serve, segnalandolo nel report |
| `05_mappa.md` | Coordinate, tipi di cella, zone, `MapLayoutAsset` | Athena/Franci |
| `06_presentazione.md` | Scene, contratto dei nomi della gerarchia, hot-seat, testi | Athena/Franci; Claude Code e Bezi propongono modifiche nel report |
| `07_tutorial.md` | Obiettivi, scenario, passi del tutorial | Athena/Franci (testi rifinibili da Inspector) |
| `08_test-e-simulazione.md` | Harness .NET, casi di test, simulazione | Athena/Franci; Claude Code aggiorna la parte operativa |

## Regola di sincronia

Il design nasce su Notion ("Mareignoto - the main PROJECT"), ma gli agenti non lo leggono: per loro questi file sono il riferimento. Quando una decisione cambia su Notion, Athena aggiorna prima questi file, poi si danno istruzioni agli agenti. Un **[DEFAULT]** in questi file è una scelta provvisoria in attesa di Franci: quando Franci decide, si aggiorna il file e si toglie il marcatore.

Divergenze:
- tra `02`/`03` e il codice: **vale il documento** (è il contratto di design). Il codice va corretto, oppure la divergenza va segnalata come domanda nel report. Mai "correggere" la regola nel documento per farla combaciare col codice.
- tra il `README.md` (stato del progetto) e il codice: **vale il codice**, il README va corretto nello stesso commit.

## Registro modifiche

| Data | Cosa è cambiato | Dove |
|---|---|---|
| 02/10/2026 | Risposte di Franci alle domande aperte recepite. **R-009**: il mazzo Corsaro non si rimescola (Crew e Pirateria sì). **R-021/R-023**: il Medico sale sopra coperta al posto della carta minacciata (non viene scartato; non è una perdita). **R-031 / `05` §4–5**: punti di partenza = preset per numero di giocatori (non più 8 punti fissi). **R-038**: vento iniziale col d8. **R-042/R-050**: ordine delle scelte di Fase 1 = ordine calcolato all'inizio del round. **R-056/R-112**: aprire un attacco con una carta della mano consuma la carta Pirateria del turno. **R-073a**: riposizionamento su nave non coinvolta (2 navi) → nuovo Abbordaggio a catena, con limite tecnico. **R-141**: parità sull'Isola Sacra → Tesoro diviso. **`05` §6**: la mappa 20×20 non esiste, layout da definire. **`03`**: Medico, Corsaro 34 carte | `02`, `03`, `05` |
| 02/10/2026 (2) | **R-032**: la disposizione delle 2 crew iniziali è libera, la posizione conta solo per Spyglass!. **R-034/R-130**: le missioni scartate restano coperte e fuori dal gioco | `02` |
| 02/10/2026 (3) | **R-016/R-023**: il Medico può riportare sotto coperta il Nostromo (unica eccezione al blocco). Errore mio nella versione precedente, che escludeva il caso | `02`, `03`, `04` §6 |
| 02/10/2026 (4) | **R-131**: le missioni di stato contano quando la condizione *diventa* vera dopo la pesca. **R-140/R-073a/R-141**: una nave riposizionata da un Abbordaggio sull'Isola Sacra prende il Tesoro e chiude la partita (arriva dopo i movimenti). **R-142**: monete delle carte Meteo del round di cortesia escono dal gioco. **R-146**: Jolly accanto al Nostromo = secondo Nostromo; con il Jolly nella combinazione resta il dimezzamento | `02` |
| 02/10/2026 (5) | **R-145**: con un solo Jolly vale la lettura più favorevole tra Jolly nella combinazione (dimezzato) e Jolly fuori (senza dimezzamento); a parità, fuori | `02` |
| 02/10/2026 (6) | **R-020/R-016**: un Nostromo ricevuto (Arrembaggio!, Reclutamento, Quartiermastro) può essere messo sotto coperta; il blocco vale solo da quando è sopra coperta. Errore mio nella specifica precedente | `02`, `04` |
| 03/10/2026 | **Mappa 25×25** con layout v4 approvato (`05` §6). **Zone** = nuvole e spicchi dell'anello centrale, con livelli (R-080, R-081, R-085, R-088); l'anello parte a livello 5. **R-002/R-031/R-038**: nuova mappa, spawn (angoli in mare, lati su cornice), livelli iniziali. **R-097**: segnalino isola; **R-053/R-069** aggiornate. **`03`**: Invocazione, Ira, Favore, Gamba di legno | `02`, `03`, `05` |
| 03/10/2026 (2) | **R-097**: con la Vedetta e più isole a portata sceglie il giocatore. **`05` §3**: il livello iniziale degli spicchi sta solo nel layout (tolto `ringInitialLevel` da `RulesConfig`) | `02`, `05`, `01` |
| 03/10/2026 (3) | **`06`**: BoardView mostra i segnalini isola; ZoneOverlayView mostra il livello delle zone. **Spec 0005**: la pipeline URP va creata, non assegnata | `06` |
| 03/10/2026 (4) | **`05`**: layout v5. L'anello diventa 8 archi regolari: 4 varchi diagonali a spigolo e 4 canali dritti larghi 1 sugli assi (prima: lobi irregolari con varchi diagonali). Nuvole da 12 a 8. Copertura 24%. **R-080**: nuvole N1–N8 | `05`, `02` |
