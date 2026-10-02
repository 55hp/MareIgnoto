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
