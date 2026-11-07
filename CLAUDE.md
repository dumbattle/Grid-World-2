# CLAUDE.md

Grid World 2 is a 2D, top-down, tile-based overworld in the style of older handheld monster-collecting RPGs: walking on a grid, talking to NPCs, moving between maps. **No battle system.** It is built to be the **reusable base for other games**, so the framework must stay separate from anything game-specific.

Inspired by the genre only: all names, characters, art, music and maps are original. Don't copy from or recreate any existing game.

## Working agreement

Follow these closely. When in doubt, ask.

### 1. Never change anything without explicit permission

Do not create, edit, delete, or move files (code, assets, config) until I explicitly approve.

- **Counts as permission:** "Implement", "Proceed", "Go ahead", "Do it", "Yes".
- **Does NOT count:** "I want this", "Let's add X", "It would be nice if…", or any statement of intent. Those are discussion: plan around them, don't act on them.

### 2. Propose before acting

Propose a plan before any non-trivial change. For a simple change, show the proposed diff or snippet instead.

- **Exception:** small, clearly scoped instructions ("Rename X to Y", "Reformat X") can just be done.
- Trivial doc edits (typos, formatting) don't need a plan.

### 3. Ask for review after changes

After a non-trivial change, ask me to review it.

- If I **approve**, update the relevant docs.
- If I **move on to another topic**, assume it's approved.
- If I **ask for more changes** to the same task, it isn't approved yet.
- If you **can't tell** which of these it is, ask.

### 4. Never commit or push without explicit permission

This covers `git commit`, `git push`, and any other history-changing git command.

### 5. Evolving these docs

Suggest updates to this file, the rules, or the workflows as we learn things, but get my approval before writing them.

### 6. Confirm every LPE change or addition

LPE is shared with my other projects. Confirm with me before changing or adding anything in it, however small. The small-task exception in rule 2 doesn't apply here.

## Quick reference

| Situation | Action |
|---|---|
| I say "I want X" / "Let's add X" | Discuss or plan only |
| I say "Implement" / "Proceed" | Go ahead |
| Non-trivial change, approved | Plan → do it → verify → ask for review |
| Small scoped task ("Rename X to Y") | Just do it |
| After a change, I start a new topic | Assume approved, update docs |
| After a change, I ask for more edits | Same task, not approved yet |
| You find a bug in passing | Report it; don't fix it without my OK |
| Commit / push | Only with explicit permission |
| Any change or addition to LPE | Confirm with me first, every time |
| Unsure about anything | Ask |

## Tech

- **Unity 6000.4.9f1**, URP 2D renderer. The Unity project is in `Unity Project/`, one level below this file.
- **Input:** Input System package only (`activeInputHandler: 1`). Actions are in `Assets/InputSystem_Actions.inputactions`.
- **Tilemaps:** `com.unity.2d.tilemap` and `tilemap.extras`, plus the LPE tile tools.
- **Sprites:** Aseprite importer is installed (`com.unity.2d.aseprite`).
- **Tests:** Unity Test Framework (NUnit).

## Layout

```
CLAUDE.md
Docs/                       Game docs. Start at INDEX.md
.claude/rules/              Standing rules (below)
.claude/skills/             Workflows (below)
Unity Project/Assets/
  2. Scripts/
    LPE/                    Library shared across my projects: confirm before changing
    GridWorld/              Reusable framework, with EditMode tests in Tests/
    Game/                   Optional, for testing the framework only (not created yet)
```

## Rules

| File | Covers | Loads |
|---|---|---|
| [architecture.md](.claude/rules/architecture.md) | Framework/game split, grid-as-truth, world areas and streaming, tile data, entities, world features, movement and hooks, triggers and props, NPC AI and pathfinding, tick timing, contexts and phases, game extension, rendering, camera, cutscenes, input, scope | Always |
| [csharp-style.md](.claude/rules/csharp-style.md) | C# formatting and naming | Working on `.cs` files |
| [unity-assets.md](.claude/rules/unity-assets.md) | `.meta` files, scenes and prefabs, art, folders | Working in `Unity Project/` |
| [lpe.md](.claude/rules/lpe.md) | Which LPE modules to use, and known gaps | Working in `2. Scripts/` |

## Workflows

Triggered by any reasonable phrasing, or directly as a slash command.

| Skill | Use when |
|---|---|
| [`/feature`](.claude/skills/feature/SKILL.md) | Building or changing a system |
| [`/bugfix`](.claude/skills/bugfix/SKILL.md) | Something is broken |
| [`/review-document`](.claude/skills/review-document/SKILL.md) | "Review these", "look at X", "document X" |
| [`/unity-verify`](.claude/skills/unity-verify/SKILL.md) | Checking that code compiles and tests pass |

## Documentation

- **Start every new session at [`Docs/INDEX.md`](Docs/INDEX.md)** so you don't have to rescan the project.
- **LPE** has its own docs at [`Assets/2. Scripts/LPE/Docs/README.md`](Unity%20Project/Assets/2.%20Scripts/LPE/Docs/README.md). Link to them; don't re-document LPE.
- Docs describe the *current* state. Git holds the history.
