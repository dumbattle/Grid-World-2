# Interaction

The player faces a tile and presses interact, and whatever is on that tile starts a cutscene. Interactions are positional: an entity's interaction follows it around, and a tile's stays on its tile. The design rules are in [architecture.md](../.claude/rules/architecture.md#triggers-props-and-interaction). Cutscenes themselves are in [cutscenes.md](cutscenes.md).

Code: `GridWorld/Interaction/`, `Functions/InteractionFunctions.cs`, `Functions/InteractAttemptFunctions.cs`, `Npcs/NpcInteractBehaviour.cs`, `Npcs/NpcInteractData.cs`.

## The press

- The root calls `input.PressInteract()` on a frame where `Player/Interact` was pressed (`WasPressedThisFrame`).
- The press stays buffered for `PlayerInput.ButtonBufferTicks` (6) ticks, or until a tick uses it. `PlayerInputSystem` ages it with `AgeButtonPresses()` at the end of each decision phase.
- While the player is locked by a cutscene, a buffered press is dropped, so a press made during a cutscene can't fire once it ends.

## Attempts

In the decision phase, `PlayerInputSystem` first clears last tick's `interactAttempts`. Then, if the player is idle and has a buffered press, it uses the press and records an `InteractAttempt`:

| Field | What it is |
|---|---|
| `entity` | The player |
| `tile` | The faced tile |
| `target` | The entity on that tile, or `EntityID.INVALID` |

The player requests no move on that tick, even with a direction held. A press made mid-step fires once the player is idle again, the tick after the step shows as finished, as long as that's within the buffer.

Permissions: WriteInteractAttempts records attempts, ClearInteractAttempts clears them, and ReadInteractAttempts reads them. Only `DecisionPhaseContext` has the first two, so the cutscene phase can only read. A game can read `interactAttempts` for its own mechanics.

## Interactions

`WorldInstance.interactions` is a `WorldInteractions`, which keeps both kinds:

- **Entity interactions** are the `entities` store, an `EntityComponent<ICutscene>`. They follow the entity as it walks. The store is in the components' cleanup registry, so a despawn removes it.
- **Tile interactions** are a `tile2interaction` map, one per tile. They cover signs, hidden items, and counters: a counter is a tile interaction in front of an NPC that doesn't move.

| Function | Permission | What it does |
|---|---|---|
| `TryAddTileInteractions(worldTiles, cutscene, out clash)` | SetupWorld | Adds the cutscene to every tile in the rectangle. If any tile already has one, it adds nothing and returns false with that tile in `clash`. A null cutscene or an empty rectangle throws |
| `TryAddEntityInteraction(id, cutscene)` | SpawnEntities | Returns false if the entity already has one. A null cutscene or a missing entity throws |
| `TryGetInteractionAt(tile, out cutscene)` | ReadInteractions | Checks the entity on the tile first, through occupancy, then the tile's own interaction. Only the first match is returned |

The adds return false instead of throwing, because the caller can't check first. Each caller decides what a clash means: both feature types turn it into a startup error naming the area. There are no remove functions yet.

## `InteractionSystem`

Runs in the cutscene phase with an `InteractionContext` (ReadInteractAttempts, ReadInteractions, StartCutscenes), before `CutsceneSystem`. That's the one ordered pair within a phase, so a press's cutscene starts on the tick after the attempt. For each attempt it calls `TryGetInteractionAt(attempt.tile)` and starts the cutscene it finds.

`CutsceneSystem` drops the cutscene with a log if one of its entities is locked, such as an NPC already in another cutscene. If the target is despawned between the attempt and the cutscene phase, occupancy no longer finds it, so nothing starts.

## Authoring

Both features hold their cutscene with `[SerializeReference, CutscenePicker] ICutscene`. The picker draws a dropdown on the field's first line, listing every `ICutscene` class that's serializable, non-abstract, not a `UnityEngine.Object`, and has a parameterless constructor. The chosen class's own fields show below it. The bake stores a copy of the cutscene (`CutsceneCopy.Of`, through `JsonUtility`), so the record doesn't share the instance with the scene.

- **NPC Interact add-on** (`NpcInteractBehaviour`): one per NPC. A missing cutscene is a bake error. At startup, `NpcInteractData` adds it with `TryAddEntityInteraction`, and a second interaction on the same NPC is a startup error.
- **Interact Tile** (`InteractTileBehaviour`): a world feature with an optional ID, a `size` in tiles from its own tile upward and rightward, and a cutscene. It draws as a magenta rectangle. A size below 1 or a missing cutscene is a bake error. At startup, `InteractTileFeatureType` checks the cutscene, the size, and the area bounds, then adds every tile at once. A clash is an error naming the area and the area-local tile.

A cutscene that needs a specific NPC holds its feature ID and finds it in `Build` with `TryGetEntity(featureId, out id)`, under ReadEntities. See [Feature IDs](npcs.md#feature-ids-at-runtime).

## Test content

In `TEST_a`, made by Create Test World:

- **`TEST_a_greeter`:** a villager three tiles above the player spawn. Its `TestGreetCutscene` holds its own ID: it locks the player, turns to face them, waits a third of a second, then spins once.
- **`TEST_a_dance_wall`:** an interact tile on the bottom row of the wall block, from (8,6) to (10,6). Its `TestDanceCutscene` spins the player twice. Face it from below and press interact.
