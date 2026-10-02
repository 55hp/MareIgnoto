# 0008 — Tutorial

Stato: bozza
Commit di riferimento: da compilare
Documenti: `tech/07_tutorial.md`, `tech/04_motore.md` §1, §4

## Obiettivo

Tutorial giocabile dal menu, come descritto in `07`: scenario a 2 con mazzi e dadi prestabiliti, avversario a risposte predefinite, `TutorialDirector` con fumetto, evidenziazione e filtro delle opzioni, 17 passi.

## Contratto

- `TutorialScenarioAsset` (`Create/MareIgnoto/Tutorial Scenario`) con setup dello scenario, risposte dell'avversario e lista dei passi (`07` §2). Asset: `Assets/Game/Content/Config/Tutorial.asset`.
- `/UI_Canvas/UI_Tutorial` (fumetto + evidenziatore).
- Un test nell'harness gioca l'intero scenario con le risposte guidate e verifica che ogni passo scatti nell'ordine (`07` §4). La costruzione dei mazzi/dadi dello scenario si fa iterando su quel test.

## Verifica

- Test dello scenario verde.
- In Play: tutorial completato da Franci senza blocchi; ogni passo evidenzia l'elemento giusto; il tutorial si abbandona in qualsiasi momento.

## Report
