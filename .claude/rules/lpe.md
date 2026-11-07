---
paths:
  - "Unity Project/Assets/2. Scripts/**"
---

# Using LPE

LPE is my library at `Assets/2. Scripts/LPE/`, and my other projects use it too. Docs: `LPE/Docs/README.md`. Read the relevant doc before using a module, and check whether LPE already does something before writing it.

- **Confirm with me before any change or addition to LPE,** however small, including new files, bug fixes, and doc edits. Explain what you want to change and why, then wait for my answer. Otherwise, build on it from `GridWorld`.
- **Don't re-document LPE** in the game docs. Link to it.
- **LPE doesn't have to follow the game's style rules.** Leave its formatting alone.

## Modules likely to matter for the overworld

| Need | LPE module | Doc |
|---|---|---|
| Facing and movement directions | `Direction4` | `utilities.md` |
| Cutscenes, dialogue flow, transitions | `Sequence/` | `sequence-system.md` |
| Sprite animation | `Animation/` | `animation.md` |
| Autotiling and tile metadata | `Unity/` (`SiblingRuleTile`, `FamilyTransitionRuleTile`, `TileIndexBehaviour`, `TileRegionBehaviour`) | Source only |
| NPC pathfinding queue | `PriorityQueue` | `data-structures.md` |
| Entity handles and storage | `EntityID`, `FreeList` | `data-structures.md` |
| Pooling | `ObjectPool` | `object-pooling.md` |
| Loop guards, profiling, random | `LoopSafety`, `ProfileHelper`, `RandomUtility` | `utilities.md` |

Hex, Steering, Shapes, Triangulation, and ThalamicAI aren't aimed at a square-grid overworld. Don't reach for them without a specific reason.

## Known gaps

- **No four-direction animation asset.** The only `DirectionalAnimationSO` subclass is `DirectionalAnimationSO_2Dir`. Grid characters need up, down, left, and right, so a four-direction subclass belongs in `GridWorld`.
- **`AnimationPlayer.Update()` advances one tick per call.** Call it from the game's tick loop, so animation speed doesn't depend on frame rate.
- **`Direction4` is a class and its lookups can return `null`.** For example, `FromVector2Int` returns `null` for anything that isn't a unit direction. Handle that at the call site.
- **`Docs/README.md` links to `behavior-trees.md`, but that file isn't there.**
