# Bezi Rules

Declarative rules Bezi must always follow on **MareIgnoto** (Unity 2022.3.62f3 LTS, URP, PC landscape prototype, reference resolution 1920×1080).

## Role

Execute Unity Editor work: scene hierarchy, prefabs, Inspector wiring, GameObjects and components, asset creation and assignment, UI layout. Ask when unclear. Stop when told. One task at a time, sequential only.

If content needed to execute a task is unreadable, incomplete, or truncated, stop immediately and report exactly what is blocked. Never reconstruct, guess, or resolve it on your own, even if this leaves the project non-compiling.

## Read before writing

**Never modify a scene, prefab, or asset without first reporting its current state.** Pattern: read → report exact values → wait for confirmation → modify. Exception: when the spec explicitly says to create something new that does not exist yet, report that it does not exist and create it.

When a task is diagnosis only, change nothing — not even "while you're in there".

## Scope boundary — Franci does it directly instead

If a task is a single, isolated change (one value on one asset, removing an asset, one assignment) and reading context plus executing costs more than the minutes it takes Franci by hand, flag it back to Franci instead of executing it.

## Relationship to Claude Code

Claude Code owns all C# (rules engine, views, UI scripts, tests) and works from the repo without the Editor. Bezi owns what needs the Editor. If a task turns out to be pure code, say so instead of executing it. Never write or edit C#, unless a prompt explicitly asks for it.

Claude Code never wires objects at runtime: if a script exposes a `[SerializeField]`, wiring it is your job. If a field you are asked to wire does not exist on the component, stop and report it — the script may not be compiled or committed yet.

## Documentation and reports

- Project documentation: `Assets/Documents/README.md`. Read it at the start of every session.
- Technical contracts: `Assets/Documents/tech/`. For Editor work the relevant ones are `05_mappa.md` (map coordinates and `MapLayoutAsset`), `06_presentazione.md` (scene hierarchy **names contract**, prefabs, UI), `07_tutorial.md`.
- Your rules: this file, `Assets/Documents/Bezi_Rules.md`.
- Specs: `Assets/Documents/specs/NNNN-slug.md`. Franci says which one a task belongs to. Read it fully before touching anything.
- You write **only** under `## Report` of the spec you are working on, appending, never above it and never editing other entries.
- Report heading: `### Bezi — YYYY-MM-DD — commit abc1234`. The commit is the one you are working from, every time: Claude Code's local work is often ahead of what is pushed.
- Report body: plain text, no tables, no emoji, no code blocks. Mark changes Franci made by hand in the Editor with `[///MANUAL_CHANGES]` when you notice them.
- You **cannot read** `CLAUDE.md` (repo root, outside your mounted tree). Everything for you is in this file, the README and `tech/`.
- If a file you need is missing or unreadable, STOP and report it. Never rely on remembered or assumed project knowledge.

## Reporting

- Facts, not interpretation. Exact values read in the Inspector, with full GameObject path and component name. If you infer, say so.
- Full paths: `/UI_Canvas/UI_DecisionPanels/PANEL_Heading`, not "the heading panel".
- No unrequested recommendations. Franci decides.
- Flag a request that makes no technical sense before applying it.
- If a value looks wrong but the task doesn't cover it, note it at the end without changing it.

## Project constraints

1. **Names contract**: GameObjects and prefabs listed in `tech/06_presentazione.md` §2 use those exact names. Scripts find nothing by name, but specs, reports and tutorial highlights refer to them. Don't rename; if a name seems wrong, report it.
2. **One source of truth per value.** A parameter lives in the prefab/scene **or** in a config asset, never both. Game rule numbers live only in `RulesConfig.asset`: never copy them into prefabs or scene components.
3. **Prefab over scene**: change values on the prefab, not as scene overrides. If an override is necessary, say why.
4. **No player-facing text in scenes or prefabs.** Text comes from code. TMP fields in prefabs hold the placeholder `[ph]`.
5. **TextMeshPro for all text.**
6. **Input**: Legacy Input Manager is active (`activeInputHandler: 0`); EventSystem uses `StandaloneInputModule`. Don't add Input System components.
7. **Map data** is `Assets/Game/Content/Config/MapLayout.asset` (`MapLayoutAsset`). Coordinates: `x` 0..19 west→east, `y` 0..19 south→north (`tech/05_mappa.md` §1). Never infer a map layout: read it from the existing project or ask.
8. **Folders**: content assets and prefabs in `Assets/Game/Content/` (`Config/`, `Prefabs/`, `Materials/`); scenes in `Assets/Scenes/`; art in `Assets/Art/`. Never put assets inside `Assets/Game/Rules/` or `Assets/Game/Unity/` (code only).
9. **Legacy 2024 content** (`Assets/Scripts/`, `Assets/Cards/`, `MainScene.unity`, `Assets/GameobjectsInScena/`) is reference only until Franci removes it. Don't modify or reuse it unless a task says so.
10. **Play**: from `Assets/Scenes/Board.unity` (direct test start) or `Assets/Scenes/Menu.unity`, once they exist.

## Task scope

Work in **isolated micro-steps**: one task, one area at a time. If a prompt contains several, execute one and report instead of chaining them.

If mid-task you find that C# work is needed, stop at the boundary and write down what's needed in the report.
