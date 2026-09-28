# Documentazione operativa — MareIgnoto

Questa cartella è la fonte operativa per lo sviluppo del prototipo digitale Unity di MareIgnoto (board game "Pirates of the 7 Orbs"). È il riferimento per Claude Code e Bezi.

Il GDD di design (visione, meccaniche, pilastri) vive su Notion — "Mareignoto - the main PROJECT" — e resta il documento di riferimento per le decisioni di design e come materiale potenzialmente da portfolio. Questa cartella non lo duplica: contiene solo le spec tecniche derivate, con dettaglio sufficiente a implementare senza ambiguità.

## Struttura

- `CLAUDE.md` (root del repo, non qui) — regole per Claude Code: convenzioni C#, architettura, cosa riusare/evitare. Non ancora scritto: in attesa delle decisioni di design core (combattimento, punteggio, eventi).
- `Bezi_Rules.md` (root del repo, non qui) — regole per Bezi: scene, prefab, Inspector, Addressables. Non ancora scritto, stesso motivo.
- `specs/NNNN-slug.md` — una spec per feature/task, generata in chat con Athena quando una decisione di design diventa pronta per l'implementazione. Numerazione progressiva a partire da 0001. Ogni spec riporta in coda i report di Bezi/Claude Code sotto un heading `## Report`.

## Regola di sincronia

Il design vive su Notion. Quando una decisione cambia lì, la spec corrispondente in `specs/` va aggiornata di conseguenza prima di essere ridata agli agenti — una spec deve sempre riflettere l'ultima decisione presa su Notion, mai una versione superata.

## Convenzioni repo

- Branch di lavoro: `develop`. Merge a `main` solo alle milestone (stesso schema di Blockout).
- Claude Code possiede il C#, non tocca scene/prefab/asset.
- Bezi possiede scene, prefab, riferimenti Inspector, Addressables.
- Franci decide, pusha, fa playtest.
- Niente workaround runtime per collegare oggetti autorabili (AddComponent/FindObjectOfType/transform.Find) — setup Editor mancante si segnala, non si compensa da codice.
- Si lavora senza lasciare modifiche locali non committate o non pushate a fine sessione.
