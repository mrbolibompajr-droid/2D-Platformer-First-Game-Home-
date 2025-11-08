# Changelog

All notable changes to this project will be documented in this file.

## [1.3.1] — 2025-09-16
### Added
- **Action Nodes & Handlers:** New `ActionNode` type with `actionId` + `payloadJson`. Coroutine-based **`IActionHandler`** lets actions block dialog until completion.
- **Demo Handlers:** `DemoHandler_Countdown` (blocking countdown) and `DemoUnityEventActions` (UnityEvents) included.
- **Runtime UI bridge:** `DialogUIController` with explicit APIs to show/hide panel, set text/speaker/portrait, bind click, toggle autoplay/skip UI.
- **Helper utilities:** `PayloadHelper` (typed parsing, color/vector parsing, token interpolation), `TextResources` (paths), small audio samples for the demo.
- **Editor views per node:** `StartNodeView`, `DialogNodeView`, `ChoiceNodeView`, `ActionNodeView`, `EndNodeView`, plus `DialogEdge`.
- **Scenes:** Added **ActionDialogDemo** showcasing action chains and wait-for-completion flows.
- **Build hygiene:** `Assets.csc.rsp` to generate XML docs and suppress CS1591.

### Changed
- **Data model:** Split node classes (`Start/Dialog/Choice/Action/End`) inheriting from `BaseNode` with explicit `NodeKind`. Cleaner branching and rendering.
- **Editor structure:** All GraphView code is under `Scripts/Editor/...`; runtime assembly is leaner.
- **Samples:** Example conversations now ship as **ScriptableObject graphs** under `Resources/Conversation/` (JSON I/O still available via editor).
- **Runtime UI:** Renamed panel script to **`DialogUIController`**; clarified method names and responsibilities.

### Fixed
- More stable link handling between typed node views and edges.
- Consistent autoplay icon initialization and panel click listener binding.
- Minor polish to history item presentation (lines vs choices).

## [1.3.0] - 2025-09-06
### Added
- **Characters Sidebar**: Rescan speakers, set portrait per speaker, one‑click Apply to nodes.
- **JSON Import/Export**: Round‑trip JSON via DialogJsonIOWindow (backup, version control, AI workflows).
- **Runtime UI Panel (UGUI)** with typing effect, choices, skip line/skip all, autoplay.
- **DialogManager** API: play by graph or ID; hooks for line start/complete; inspector options.
- **Sample Scene**: Three demo conversations and portraits to showcase flow.
- **Minimap** in editor; improved toolbar (Add Node, Save, Clear, Hide/Show Characters).
- **Asmdefs & Namespaces**: `DialogSystem.Runtime`, `DialogSystem.Editor`.

### Changed
- Refined node layout and port styling; more readable dark theme.
- Safer defaults: no debug logs in release; editor code isolated in `Editor/` folders.

### Fixed
- Edge/link instability on fast undo/redo.
- Minor GUID/entry‑node validation issues when duplicating nodes.

## [1.2] — 2025-08-27
### Added
- **Dialogue History**: data model, `DialogueHistoryView`, and **DialogueHistoryPanel** prefab; manager pauses autoplay while open.
- **JSON Import/Export window** with Export & Import tabs, JSON preview, drag‑and‑drop, recent files, and **backup/undo** safety.
- **GUID policies** on import: Preserve / Regenerate on conflict / Regenerate all.
- **Graph editor** features: minimap, duplicate selection, safe delete (cleans links), undo/redo awareness.
- **Node fields**: display time (seconds), audio clip, portrait preview, entry badge, **Auto Next** port.
- **UI prefabs**: DialogUI_Panel, Choice_Btn, DialogueHistoryPanel.
- **Runtime events** (e.g., line shown, choice picked, conversation reset) and **Play by Type** mapping.

### Changed
- Reorganized folders into `_Scripts/Runtime` and `_Scripts/Editor`, plus `_Scenes`, `Prefabs`, `Resources`.
- DialogManager refactored into a **MonoSingleton** with clearer UI references and options (typing speed, skip, auto‑advance).

### Fixed
- Safer asset deletion from graph editor (prevents dangling references).
- Multiple UX polish items across graph toolbar and node inspectors.

## [1.0]
### Initial
- Basic dialog graph asset with entry node and choices
- Early graph editor (add/remove nodes, connect ports)
- Simple runtime playback (speaker, text, choices)
- Basic JSON export utility
- Minimal demo UI
