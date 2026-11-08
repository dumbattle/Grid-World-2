# First slice plan

**Status:** in progress. Phases 1 to 3 are done and approved. Each later phase starts only when approved.

**Goal:** press Play and walk or run a placeholder character across two touching areas, with walls blocking movement and the second area's scene loading before the player reaches it. This tests the riskiest parts of the design: the bake, `[SerializeReference]` records, additive streaming, the tick loop, and how movement feels.

Design rules are in [architecture.md](../.claude/rules/architecture.md).

**LPE changes:** none. The slice uses `EntityID` and `Direction4` as they are.

## Decisions to confirm

1. **Build scene list:** approved and done. The bake and test setup add scenes to `ProjectSettings/EditorBuildSettings.asset`, because `LoadSceneAsync` needs them there.
2. **Tile properties:** done, and in the authoring rules.
3. **Test setup:** done, as the `Game.Editor` assembly. Placeholder tiles are 16 px and the view is 15×11 tiles.
4. **Multiple direction keys:** the most recently pressed direction wins. Done, and in the movement rules.

## Phases

Each phase ends with a compile check, EditMode test results, and a review before the next one starts. The Unity Editor may be open, so verification follows the `unity-verify` workflow.

### Phase 1: foundation (done)

Plain C# state with no scenes. Only the `tile`, `facing`, and `definition` components exist so far, and the rest arrive with the phases that use them. The player's ID on `WorldInstance` comes in phase 2.

- `GridWorld` and `GridWorld.Tests` assemblies.
- `WorldInstance`, `IWorldFunctionContext`, `WorldFunctionContext`, and context caching.
- `EntityComponent<T>`, `EntityComponentManager`, `EntityIDManager`, and `EntityDefinitionSO`.

**Tests:** component stores, the `Register` path, and ID allocation.

### Phase 2: tiles and movement logic (done)

The core rules, fully testable without Unity scenes. `WorldSetupContext` adds areas, spawns entities, and sets the player, ready for world startup in phase 3. The `failedMoves` and `stepsCompleted` hooks aren't written yet.

- `WorldTiles` lookup.
- `IPerm_*` markers, function extensions, and the decision and movement contexts.
- `PhaseExecutionManager` with Decision and Movement phases. The cutscene phase comes with cutscenes.
- `MovementSystem` and `PlayerInputSystem`, reading stored input state from `WorldInstance`.

**Tests:**
- Tile lookup rules.
- Movement: step duration, constant rate across steps, tile claim and release, entry bits, entity blocking, tie-break order, follower retry, blocked swaps, and idle clearing.
- Player input: tap to turn, the 6-tick hold, walking while already facing that way, continuous walking, and reading run at step start.

### Phase 3: bake and world features (done)

Authored scenes become data. Every area bakes with a world offset of 0 until doors give interiors their offsets. The bake on save ran during the batch test world build; Bake All and the pre-build bake haven't been run yet.

- `AreaDataSO`, `WorldDataSO`, `WorldSettingsSO`, `AreaBehaviour`, `LogicLayerBehaviour`, and `TilePropertyTableSO`.
- `WorldFeatureBehaviour`, `WorldFeatureType<TData>`, the catalogue, and `PlayerSpawnBehaviour` with its type.
- `GridWorld.Editor` assembly: area bake on scene save, a "Bake All" menu command, pre-build bake, and overlap errors.
- `Game.Editor` assembly: **Grid World → Test → Create Test World**, which creates placeholder tiles, the main scene, two area scenes, and the data assets, then bakes them.

**Tests:** catalogue errors and overlap errors.

**Check:** run the menu command and inspect the baked assets.

### Phase 4: playable in one area

The first time it can be played and judged.

- `WorldRootBehaviour`: input capture, the tick accumulator, and world startup.
- `WorldView` with `Tick()` and `Render(alpha)`, using a generated placeholder that shows facing.
- `CameraView`, `ICameraController`, and the follow controller.

**Play test:** walking and running feel smooth with no stutter at tile boundaries, tap to turn feels right, and walls block movement.

### Phase 5: streaming across two areas

Completes the slice.

- Area activation in the tick, with separate load and unload distances.
- `AreaSceneLoader`, loading additive scenes asynchronously.
- Console logging for each area load and unload, with its time.

**Tests:** active area distances, including no reload when stepping across a border.

**Play test:** the second area appears before the player reaches it, the player walks across the border with no visible seam, and the first area unloads once far enough away. Check the logged load times for hitches.

## After the slice

Update `Docs/INDEX.md`, add system docs under `Docs/`, update the rules above, and remove this plan.

## Left for later

NPCs and AI, cutscenes and the cutscene phase, hooks, props, ledges, doors and interiors, pathfinding, saving, dialogue, the interact button, four-direction animation, Y-sorting, pixel snapping, the override tilemap, game extension, and the stress test for a large area scene. Test area sizes are constants, so that stress test is easy to try.
