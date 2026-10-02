# 08 — Test e simulazione

## 1. Harness .NET (`Tools/RulesHarness/`)

- Progetto `MareIgnoto.RulesHarness.csproj` (target `net8.0` o quello disponibile sulla macchina di Franci, `LangVersion 9.0`, `Nullable disable`), NUnit 3 + `NUnit3TestAdapter` + `Microsoft.NET.Test.Sdk`.
- Include come file collegati: `../../Assets/Game/Rules/**/*.cs` e `../../Assets/Tests/EditMode/Rules/**/*.cs`.
- Comando: `dotnet test Tools/RulesHarness`. Claude Code lo esegue da sé prima di dichiarare finito un task sulle regole. Se `dotnet` non è installato, lo segnala nel report e chiede a Franci di installarlo (SDK .NET 8): non è un motivo per saltare i test.
- Gli stessi test girano anche nel Test Runner di Unity (EditMode), grazie all'asmdef `hp55games.MareIgnoto.Rules.Tests`. Fanno fede entrambi: un test che passa nell'harness e fallisce in Unity indica un problema di versione del linguaggio o di API.

## 2. Cosa testare

Per ogni regola implementata, almeno un test che citi l'ID nel nome o in un commento (`R-066`). In particolare i casi limite:

- movimento: collisione con nave ferma, attraversamento (R-067), arrivo su isola, su cornice, velocità 0, vento non applicato in partenza dal porto;
- Abbordaggio: due navi con tiri uguali (ripetizione), tre navi (R-071), destinazione isola/cornice/occupata;
- meteo: rotazione con 8 = nessun cambio, Navigatore che abbassa, Jolly + Navigatore, Vento in Poppa;
- combattimento: botta e risposta fino all'esaurimento, Cannoniere, Falconet, Culverin a distanza 2, Saker, Nostromo, Arrembaggio! pagato/parato/riuscito, Medico;
- ordine turno: round 1 per offerta, poi somma con slot vuoti e Jolly a 0, catena dei pareggi;
- missioni: ogni missione completata e una non completata; R-131 (eventi prima della pesca non contano);
- punteggio poker: ogni combinazione, scale con l'asso alto e basso, un Jolly (dimezzato), due Jolly (4), Nostromo che raddoppia;
- determinismo: due sessioni con stesso seed e stesse risposte producono gli stessi eventi.

## 3. Simulazione

`SimulationRunner` (nel progetto harness o come test `[Explicit]`): gioca N partite complete con bot casuali, per un insieme di numeri di giocatori (2, 4, 8) e seed diversi.

Ad ogni `Submit` verifica gli invarianti di `04_motore.md` §6. Alla fine riporta:
- partite completate / interrotte per `maxRounds` (con il seed di quelle interrotte);
- round medi, minimi e massimi per numero di giocatori;
- distribuzione delle fonti di taglia (battaglie, missioni, monete, poker, Tesoro).

Il gate di fine Fase 1 del piano è: 1000 partite (seed 1–1000, 2/4/8 giocatori) senza eccezioni né violazioni di invarianti. Le statistiche servono a Franci per il bilanciamento, non sono un criterio di accettazione.

## 4. Test in Unity

- EditMode: i test Rules (sopra) + test di conversione `RulesConfigAsset → RulesConfig` e `MapLayoutAsset → MapLayout` (incluso `Validate()` sull'asset reale del progetto).
- Prova manuale in Play: lista nella sezione "Verifica" di ogni spec.
