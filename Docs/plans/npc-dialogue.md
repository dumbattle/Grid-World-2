# Plan: NPC dialogue milestone

Status: slice 1 (cutscenes) is built and documented in [cutscenes.md](../cutscenes.md), including the move to `ICutscene` objects. Slice 2's design is agreed below, and an earlier draft built on string IDs is in `git stash`. Slice 3 is proposed, not approved. Once a slice is built and approved, its design moves into the system docs and comes out of this file.

The milestone has three slices, each playable on its own: cutscenes, then interaction, then dialogue. All code goes in `GridWorld` and test content in `Game`. No LPE changes are needed.

## Slice 2: Interaction (agreed, not built)

- **Input:** the root reads `Player/Interact` with `WasPressedThisFrame`, and presses are buffered for 6 ticks. A locked player's buffered press is dropped.
- **Interact attempts:** in the decision phase, an idle, unlocked player with a buffered press uses it and records an `interactAttempts` entry: the player, the faced tile, and any entity on it. No move is requested that tick, even with a direction held. The decision phase clears last tick's attempts, so the cutscene phase only reads them. Permissions: `IPerm_WriteInteractAttempts` and `IPerm_ReadInteractAttempts`.
- **Interactions are positional.** `TryGetInteractionAt(tile, out cutscene)`, under `IPerm_ReadInteractions`, checks the entity on the tile first, through occupancy, then the tile's own interaction. Only the first match fires.
  - Entity interactions are an `EntityComponent<ICutscene>`, so they follow the entity and a despawn clears them.
  - Tile interactions are a `tile2interaction` map, one per tile. They cover hidden items, signs, and counters: a counter is tile interactions in front of an NPC that doesn't move.
  - `IPerm_EditInteractions`: `AddTileInteraction`, `RemoveTileInteraction`, `AddEntityInteraction`, `RemoveEntityInteraction`. Adding a second one throws. The setup and cutscene contexts have it.
- **`InteractionSystem`:** runs in the cutscene phase before `CutsceneSystem`, the one ordered pair in a phase, so the cutscene starts on the tick after the press. For each attempt it calls `StartCutscene` with the interaction's `ICutscene`. A locked target drops the cutscene.
- **Authoring:** the NPC Interact add-on, and an `InteractTileBehaviour` world feature with a size, each hold a `[SerializeReference] ICutscene`, chosen with an Inspector type picker in GridWorld.Editor. An empty cutscene is a bake error.
- **Feature ID lookup:** `TryGetEntity(featureId)`, so a baked cutscene can find the NPCs it names.
- **Test content:** a greeter NPC whose cutscene faces the player and spins, and a tile interaction on the wall block in area A that makes the player dance.

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

## Noted in LPE, not changed

`sequence-system.md` calls the queue factory `SequenceQueue.Get()`, but the method is `GetNew()`, in `ChainedSequence.cs`. Fixing it needs your confirmation.
