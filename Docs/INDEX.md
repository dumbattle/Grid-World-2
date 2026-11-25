# Grid World 2: Docs Index

Orientation map for the project. Keep it short and current; add one line per system as it's built and approved.

## Status

The first slice is done: a player walks and runs across two touching areas, with baked tile data, walls, and area scenes streaming in and out. Placed NPCs spin, wander, or patrol, and spawn zones keep mobs alive while their area is active. **Grid World → Test → Create Test World** builds the test world; open `TEST_Main` and press Play. Entities have pooled sprites only while they're in an active area, and play idle and walk animations in four directions when their definition has an animation set. The NPC milestone's three slices are done: placed NPCs with AI, spawn zones with despawning, and sprites streaming with their areas. NPCs, spawn zones, and the player spawn have world-unique IDs, checked in the Inspector, the Scene view, the bake, and at startup, and fixed in **Grid World → Fix Feature IDs**. Remaining design questions and unbuilt work are in [open decisions](open-decisions.md).

## Systems

Design rules are in [architecture.md](../.claude/rules/architecture.md). The docs below describe the code as it is.

| System | Doc | Key types |
|---|---|---|
| World state and contexts | [world-and-areas.md](world-and-areas.md#worldinstance) | `WorldInstance`, `WorldFunctionContext`, the `IPerm_*` markers |
| Entities | [world-and-areas.md](world-and-areas.md#worldinstance) | `EntityIDManager`, `EntityComponent<T>`, `EntityComponentManager`, `EntityDefinitionSO` |
| Spawning and despawning | [world-and-areas.md](world-and-areas.md#spawning-and-despawning) | `EntityLifecycle`, `EntityComponentStore`, `SpawnFunctions` |
| Startup | [world-and-areas.md](world-and-areas.md#startup) | `WorldStartup`, `WorldValidation` |
| Areas and streaming | [world-and-areas.md](world-and-areas.md#area-activation) | `WorldAreas`, `AreaActivationSystem`, `AreaSceneLoader` |
| Tile data | [tiles-and-movement.md](tiles-and-movement.md#tile-data) | `TileBits`, `AreaTiles`, `WorldTiles`, `TileOccupancy` |
| Movement and phases | [tiles-and-movement.md](tiles-and-movement.md#a-tick) | `PhaseExecutionManager`, `MovementSystem`, `Step`, `MoveRequest` |
| NPCs and AI | [npcs.md](npcs.md) | `NpcBehaviour`, `NpcAddonBehaviour`, `NpcFeatureType`, `EntityAI`, `AISystem`, `SpinAI`, `WanderAI`, `PatrolAI`, `TileSpace`, `TileSpaces`, `RegionSpaceInspector` |
| Spawn zones | [npcs.md](npcs.md#spawn-zones) | `SpawnZoneBehaviour`, `SpawnAIBehaviour`, `SpawnZoneData`, `SpawnZoneFeatureType<TData>`, `SpawnZone`, `SpawnSystem`, `SpawnContext` |
| Player input | [tiles-and-movement.md](tiles-and-movement.md#player-input) | `PlayerInputSystem`, `PlayerInput`, `PlayerControlState` |
| Root and tick loop | [root-view-camera.md](root-view-camera.md#worldrootbehaviour) | `WorldRootBehaviour`, `TickClock`, `TestWorldRootBehaviour` (Game) |
| Rendering | [root-view-camera.md](root-view-camera.md#worldview) | `WorldView`, `RenderContext`, `RenderFunctions`, `PlaceholderSprites` |
| Entity animation | [root-view-camera.md](root-view-camera.md#animation) | `EntityAnimationSO`, `DirectionalClips`, `SpriteClip`, `EntityAnimationRules`, `EntityAnimationSheet` |
| Camera | [root-view-camera.md](root-view-camera.md#camera) | `ICameraController`, `FollowCameraController`, `CameraView` |
| Authoring and bake | [authoring-areas.md](authoring-areas.md) | `AreaBehaviour`, `LogicLayerBehaviour`, `TilePropertyTableSO`, `AreaDataSO`, `WorldDataSO`, `AreaBaker` |
| New Area window | [authoring-areas.md](authoring-areas.md#making-an-area-scene) | `NewAreaWindow`, `NewAreaCreator`, `NewAreaRules` |
| Delete Area | [authoring-areas.md](authoring-areas.md#deleting-an-area) | `AreaDeleter` |
| Area specs and previews | [authoring-areas.md](authoring-areas.md#drafting-areas-from-specs) | `MapSpecParser`, `MapSpecChecks`, `MapSpecBuilder`, `MapSpecDiff`, `MapSpecApplier`, `AreaPreviewExporter`, `Tools/mapread.py` |
| World features | [authoring-areas.md](authoring-areas.md#adding-a-world-feature-type) | `WorldFeatureBehaviour`, `WorldFeatureType<TData>`, `WorldFeatureCatalogue`, `PlayerSpawnBehaviour` |
| Feature IDs | [authoring-areas.md](authoring-areas.md#feature-ids) | `FeatureIds`, `IdentifiedFeatureBehaviour`, `IdentifiedFeatureData`, `FeatureIdChecks`, `FeatureIdIndex`, `FeatureIdWindow`, `mapread.py features` |
| Feature edits | [authoring-areas.md](authoring-areas.md#editing-features-from-text) | `FeatureEditParser`, `FeatureEditValues`, `FeatureEditApplier`, `FeatureEditMenu` |
| Test world | [authoring-areas.md](authoring-areas.md#the-test-world) | `CreateTestWorld` |

## Plans

- [NPC dialogue milestone](plans/npc-dialogue.md): cutscenes, interaction, then dialogue. Proposed, not approved.

## Related

- [LPE library docs](../Unity%20Project/Assets/2.%20Scripts/LPE/Docs/README.md)
- The previous project, `../../Grid World` (Unity 2020.3), is a reference for ideas only. Don't port from it unless asked.
