# NNNN — Titolo del task

Stato: bozza | in corso | pronta per Bezi | chiusa
Commit di riferimento: `abc1234`
Regole coinvolte: R-xxx, R-yyy
Documenti: `tech/0x_....md`

Un file per task. Sopra `## Report` scrivono Athena (intestazione, Obiettivo, Contratto, Verifica) e Claude Code (Lato codice, checklist Bezi, Per Franci). Sotto `## Report` ogni attore aggiunge voci in coda e non modifica quelle degli altri.

## Obiettivo

Cosa deve funzionare alla fine del task, in poche righe.

## Contratto

Nomi, percorsi, campi e comportamenti che altri attori useranno. Deciso prima di iniziare, così codice ed Editor procedono in parallelo.

## Lato codice (Claude Code)

- Script nuovi o modificati, con percorso.
- `[SerializeField]` esposti che aspettano un collegamento.

## Lato Editor (Bezi) — checklist

- [ ] Scena / prefab: oggetto → componente → campo → cosa assegnare.
- [ ] Asset da creare (menu `Create/...`) e dove salvarli.

## Per Franci — modifiche isolate

- Singole modifiche piccole che non giustificano un giro di Bezi.

## Verifica

- Comandi/test da lanciare (`dotnet test Tools/RulesHarness`, Test Runner).
- Cosa controllare in Play.

## Report

<!-- Voci in coda. Intestazioni:
### Claude Code — AAAA-MM-GG — commit abc1234
### Bezi — AAAA-MM-GG — commit abc1234
### Franci — AAAA-MM-GG
Testo semplice. Modifiche fatte a mano nell'Editor: [///MANUAL_CHANGES].
Domande sulle regole sotto "#### Domande". -->
