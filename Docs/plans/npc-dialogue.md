# Plan: NPC dialogue milestone

Status: proposed, not approved. Nothing is built yet. Once a slice is built and approved, its design moves into the system docs and comes out of this file.

The milestone has three slices, each playable on its own: cutscenes, then interaction, then dialogue. All code goes in `GridWorld` and test content in `Game`. No LPE changes are needed.

## Slice 1: Cutscenes

**Playable result:** a debug key in `TEST_Main` starts a test cutscene. The player walks 3 tiles, an NPC turns to face them, and then both walk at once. While it runs, the player can't move and the NPC's AI pauses. Both are released when it ends.

**Tick loop:** a Cutscene phase goes between Spawn and Decision.

**New state on `WorldInstance`:**

- a `scripted` marker store, which is the lock. It's added through `Register`, so `Despawn` clears it.
- the running cutscenes, each with its root sequence and the entities it locked
- a queue of cutscene start requests

**New types**, in `GridWorld/Cutscenes/`:

| Type | Role |
|---|---|
| `CutsceneContext` | Has `ReadEntities`, `ReadTiles`, `RequestMove`, plus new `RunCutscenes` (lock, unlock, start) and `Teleport` |
| `CutsceneSystem` | Starts queued cutscenes, dropping one with a log if any of its entities is already locked. Advances each running cutscene once per tick, and calls `OnComplete` and unlocks when it's done |
| `CutsceneBuilder` | A thin layer over LPE `SequenceQueue`: `Walk(id, dir, count)`, `Face(id, dir)`, `Teleport(id, tile)`, `Wait(ticks)`, `Do(action)`, `Parallel(b => ...)`, `Extend(fn)`, `Lock(id)`. Records the entities its steps touch |
| `CutsceneCatalogue` | Maps a string ID to a build function. The root registers entries before load. A duplicate ID is an error |
| Step sequences | `WalkStepsSequence`, `FaceSequence`, `TeleportSequence`, `ExtendSequence` |

**How the steps behave:**

- **Walk and Face submit normal move requests.** Face is a turn-only request, so movement, blocking, and animation all behave as usual.
- **Walk requests its next step on the `Finished` tick,** which keeps chained steps at a constant rate. It counts as done once its last step finishes, so the next cutscene step starts on that same tick.
- **A blocked walk times out** after 120 ticks. It then teleports to its destination, or the nearest free tile, and logs a warning.
- **Steps run with no gap.** Every entry is added with `playNextImmediate: true`, so instant steps run back to back and `Wait(n)` takes exactly n ticks.
- **Extend checks its locks.** An extender that adds a step for an entity that isn't locked is an error naming the cutscene.
- **Locked entities are left alone.** `PlayerInputSystem` and `AISystem` skip them, and the player's control state resets.
- **A despawned entity ends its step.** A step whose entity is despawned partway through ends at once and logs a warning.

**Tests** (EditMode, with `TestWorld`) cover:

- the lock, and dropping a cutscene when an entity is already locked
- exact tiles and timing for a walk, including chained steps at a constant rate
- the timeout teleport when a walk is blocked
- two entities walking in parallel
- Extend reading up-to-date state, and its error on an unlocked entity
- the AI and the player skipping locked entities
- the unlock at the end
- a duplicate catalogue ID
- a despawned entity

**Editor:** bind the debug key on `TestWorldRootBehaviour`, like the despawn key.

## Slice 2: Interaction (sketch)

- **Input:** the root reads `Player/Interact`, and presses are buffered for 6 ticks.
- **Interact attempts:** in the decision phase, an idle, unlocked player with a buffered press consumes it and writes an `interactAttempts` entry: the player, the faced tile, and any entity on it. No move is requested that tick.
- **`InteractionSystem`:** runs in the next tick's cutscene phase. It reads and clears the attempts, then starts the target entity's interact cutscene. If the NPC is locked, the cutscene is dropped.
- **Interact NPC add-on:** holds a cutscene ID. An ID that isn't in the catalogue is a startup error naming the area and the ID.
- **Test content:** an NPC whose cutscene faces the player and does a short walk.

## Slice 3: Dialogue (sketch)

- **`DialogueSO`:** a list of pages of text (*Create → Grid World → Dialogue*).
- **`PlayDialogue(dialogue)`:** a builder step. It posts a request to dialogue state on `WorldInstance`, then waits until the text box reports done. Requests from several cutscenes queue and show one at a time.
- **Talk NPC add-on:** gives the NPC a `dialogue` component. `InteractionSystem` builds the talk cutscene: the NPC faces the player, then the dialogue plays.
- **The text box:** a plain class the root drives outside the phases. It reads the request through a small UI context, shows the pages, and advances on `Player/Interact`.

## Open questions and recommendations

1. **UI framework** (slice 3): UI Toolkit. It's built in, its layout is plain UXML and USS text, and the Editor setup is one UIDocument plus a Panel Settings asset. The alternative is uGUI with TextMeshPro and a hand-built Canvas prefab.
2. **Facing after talking:** the NPC stays facing the player. Its AI turns it again later anyway.
3. **An NPC with both an Interact and a Talk add-on:** a startup error.
4. **Walk timeout:** 120 ticks, as a constant on the walk step.
5. **Create Test World:** each slice adds its own test content to it.

## Later, not in this milestone

- `Walk(id, tile)` with pathfinding
- camera steps and the camera lock
- triggers and props
- the `failedMoves` and `stepsCompleted` hooks
- a typewriter text effect
- speaker names and portraits

## Noted in LPE, not changed

`sequence-system.md` calls the queue factory `SequenceQueue.Get()`, but the method is `GetNew()`, in `ChainedSequence.cs`. Fixing it needs your confirmation.
