# Grid World 2: Docs Index

Orientation map for the project. Keep it short and current; add one line per system as it's built and approved.

## Status

The first slice is done: a player walks and runs across two touching areas, with baked tile data, walls, and area scenes streaming in and out. **Grid World → Test → Create Test World** builds the test world; open `TestMain` and press Play. There's no active plan. Remaining design questions and unbuilt work are in [open decisions](open-decisions.md).

## Systems

Design rules are in [architecture.md](../.claude/rules/architecture.md). The docs below describe the code as it is.

| System | Doc | Key types |
|---|---|---|
| World state and contexts | [world-and-areas.md](world-and-areas.md#worldinstance) | `WorldInstance`, `WorldFunctionContext`, the `IPerm_*` markers |
| Entities | [world-and-areas.md](world-and-areas.md#worldinstance) | `EntityIDManager`, `EntityComponent<T>`, `EntityComponentManager`, `EntityDefinitionSO` |
| Startup | [world-and-areas.md](world-and-areas.md#startup) | `WorldStartup`, `WorldValidation` |
| Areas and streaming | [world-and-areas.md](world-and-areas.md#area-activation) | `WorldAreas`, `AreaActivationSystem`, `AreaSceneLoader` |
| Tile data | [tiles-and-movement.md](tiles-and-movement.md#tile-data) | `TileBits`, `AreaTiles`, `WorldTiles`, `TileOccupancy` |
| Movement and phases | [tiles-and-movement.md](tiles-and-movement.md#a-tick) | `PhaseExecutionManager`, `MovementSystem`, `Step`, `MoveRequest` |
| Player input | [tiles-and-movement.md](tiles-and-movement.md#player-input) | `PlayerInputSystem`, `PlayerInput`, `PlayerControlState` |
| Root and tick loop | [root-view-camera.md](root-view-camera.md#worldrootbehaviour) | `WorldRootBehaviour`, `TickClock` |
| Rendering | [root-view-camera.md](root-view-camera.md#worldview) | `WorldView`, `RenderContext`, `PlaceholderSprites` |
| Camera | [root-view-camera.md](root-view-camera.md#camera) | `ICameraController`, `FollowCameraController`, `CameraView` |
| Authoring and bake | [authoring-areas.md](authoring-areas.md) | `AreaBehaviour`, `LogicLayerBehaviour`, `TilePropertyTableSO`, `AreaDataSO`, `WorldDataSO`, `AreaBaker` |
| World features | [authoring-areas.md](authoring-areas.md#adding-a-world-feature-type) | `WorldFeatureBehaviour`, `WorldFeatureType<TData>`, `WorldFeatureCatalogue`, `PlayerSpawnBehaviour` |
| Test world | [authoring-areas.md](authoring-areas.md#the-test-world) | `CreateTestWorld` |

## Related

- [LPE library docs](../Unity%20Project/Assets/2.%20Scripts/LPE/Docs/README.md)
- The previous project, `../../Grid World` (Unity 2020.3), is a reference for ideas only. Don't port from it unless asked.
