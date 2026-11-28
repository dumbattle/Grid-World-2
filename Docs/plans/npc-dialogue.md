# Plan: NPC dialogue milestone

Status: slices 1 and 2 are built and documented, in [cutscenes.md](../cutscenes.md) and [interaction.md](../interaction.md). Slice 3 is proposed, not approved. Once a slice is built and approved, its design moves into the system docs and comes out of this file.

The milestone has three slices, each playable on its own: cutscenes, then interaction, then dialogue. All code goes in `GridWorld` and test content in `Game`. No LPE changes are needed.

## Slice 3: Dialogue (sketch)

- **`DialogueSO`:** a list of pages of text (*Create → Grid World → Dialogue*).
- **`PlayDialogue(dialogue)`:** a builder step. It posts a request to dialogue state on `WorldInstance`, then waits until the text box reports done. Requests from several cutscenes queue and show one at a time.
- **Talk NPC add-on:** gives the NPC a `dialogue` component. Its interaction is a framework talk `ICutscene` holding the NPC's ID and the dialogue: the NPC faces the player, then the dialogue plays.
- **A closing press:** the Interact press that closes the text box must be used up, or it stays buffered and starts the talk again.
- **The text box:** a plain class the root drives outside the phases. It reads the request through a small UI context, shows the pages, and advances on `Player/Interact`.

## Open questions and recommendations

1. **UI framework** (slice 3): UI Toolkit. It's built in, its layout is plain UXML and USS text, and the Editor setup is one UIDocument plus a Panel Settings asset. The alternative is uGUI with TextMeshPro and a hand-built Canvas prefab.
2. **Facing after talking:** the NPC stays facing the player. Its AI turns it again later anyway.
3. **An NPC with both an Interact and a Talk add-on:** a startup error.
4. **Create Test World:** each slice adds its own test content to it.

## Later, not in this milestone

- `Walk(id, tile)` with pathfinding
- camera steps and the camera lock
- triggers and props
- the `failedMoves` and `stepsCompleted` hooks
- a typewriter text effect
- speaker names and portraits
- removing interactions, once something needs it, such as a picked-up hidden item
- `AddAI` and `SetFeatureId` still throw on a duplicate. Consider try-adds, as the interaction adds now do

## Noted in LPE, not changed

`sequence-system.md` calls the queue factory `SequenceQueue.Get()`, but the method is `GetNew()`, in `ChainedSequence.cs`. Fixing it needs your confirmation.
