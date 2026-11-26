# Plan: NPC dialogue milestone

Status: slice 1 (cutscenes) is built and documented in [cutscenes.md](../cutscenes.md). Slices 2 and 3 are proposed, not approved. Once a slice is built and approved, its design moves into the system docs and comes out of this file.

The milestone has three slices, each playable on its own: cutscenes, then interaction, then dialogue. All code goes in `GridWorld` and test content in `Game`. No LPE changes are needed.

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
4. **Create Test World:** each slice adds its own test content to it.

## Later, not in this milestone

- `Walk(id, tile)` with pathfinding
- a smaller step context for game-written steps, without `RunCutscenes`
- camera steps and the camera lock
- triggers and props
- the `failedMoves` and `stepsCompleted` hooks
- a typewriter text effect
- speaker names and portraits

## Noted in LPE, not changed

`sequence-system.md` calls the queue factory `SequenceQueue.Get()`, but the method is `GetNew()`, in `ChainedSequence.cs`. Fixing it needs your confirmation.
