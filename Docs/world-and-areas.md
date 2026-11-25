# World state and areas

How world state is held, how systems reach it, how the world starts up, and how area scenes stream. The design rules are in [architecture.md](../.claude/rules/architecture.md#state-and-contexts).

Code: `GridWorld/WorldInstance.cs`, `Contexts/`, `Permissions/`, `Functions/`, `World/`, `Areas/`.

## `WorldInstance`

Owns all overworld state as public fields. Only the root holds it.

| Field | What it is | Written by |
|---|---|---|
| `entityIds`, `components` | Entity IDs and component stores (`tile`, `facing`, `definition`, `step`, `ai`) | Startup, spawn phase, movement |
| `spawnZones` | Every `SpawnZone`, in the order they were added | Startup |
| `viewTiles` | The view size in tiles, from the world settings (15×11 by default) | Startup |
| `random` | A `System.Random`, seeded at startup from the world settings | Startup, decision phase |
| `player` | The player's `EntityID` | Startup |
| `tiles`, `occupancy` | Tile data lookup and `tile2entity` | Startup, movement |
| `areas` | Each area's ID, world bounds, and active flag | Startup, area activation |
| `input` | Held directions and run, as Unity last reported them | Root, each frame |
| `cameraController` | The current `ICameraController` | Root |
| `playerControl` | The player's hold timer | Decision phase |
| `id2moveRequest` | Move requests, Decision → Movement | Decision phase |

## Contexts and permissions

A system gets a context from `world.GetContext<T>()`, which creates it once and caches it by type. A context has no state of its own: it's a list of `IPerm_*` markers.

| Context | Permissions | Used by |
|---|---|---|
| `WorldSetupContext` | SetupWorld, ReadEntities, ReadTiles | World startup and feature types |
| `DecisionPhaseContext` | ReadEntities, RequestMove, PlayerControl | `PlayerInputSystem` |
| `SpawnContext` | ReadTiles, ReadEntities, ReadAreas, ReadCamera, UseRandom, SpawnEntities, DespawnEntities | `SpawnSystem` and every `SpawnZone` |
| `AIContext` | ReadTiles, ReadEntities, ReadAreas, RequestMove, UseRandom | `AISystem` and every `EntityAI` |
| `MovementPhaseContext` | ReadTiles, ReadEntities, ResolveMovement | `MovementSystem` |
| `AreaActivationContext` | ReadAreas, UpdateAreas | `AreaActivationSystem` |
| `RenderContext` | ReadEntities, ReadCamera, ReadAreas | `WorldView`, `CameraView`, camera controllers, `AreaSceneLoader` |

Functions are extension methods in `Functions/` (and next to their system, such as `Areas/AreaFunctions.cs`), constrained on `IWorldFunctionContext` plus one marker:

```csharp
public static Vector2Int GetTile<T>(this T ctx, EntityID id)
            where T : IWorldFunctionContext, IPerm_ReadEntities {
    return ctx.data.components.tile.Get(id);
}
```

To add an operation, write a function like this. To give a system new access, add an `IPerm_*` marker (one file each in `Permissions/`) and list it on that system's context. Only functions touch `ctx.data`; systems never cast a context to `IWorldFunctionContext`.

## Spawning and despawning

Entities are created through `EntityLifecycle`, behind two sets of functions with the same names: `SpawnEntity` and `AddAI` under SetupWorld for startup, and under SpawnEntities for runtime. `AddAI` throws if the entity already has one.

`Despawn(id)`, under DespawnEntities, removes an entity completely:

- It releases the entity's tile. Mid-step, that's the target tile, since the old one was released when the step began.
- It removes the entity from every component store, and drops any pending move request.
- Its `EntityID` is never reused. `EntityExists(id)`, under ReadEntities, returns false from then on.
- It throws if the entity doesn't exist, or is the player.

Every store derives from `EntityComponentStore`, and the manager keeps a list of them for this. The framework's stores are added in its constructor. A game adds its own with `components.AddStore(store)` when it creates them, so despawning cleans them up too. Adding a store twice throws.

`EntityIDManager.count` is the number of IDs ever created, and `liveCount` the number that still exist.

## Startup

`WorldStartup.Load(ctx, worldData, catalogue)`, called by the root:

1. `WorldValidation.FindError` checks for missing areas, empty or duplicate IDs, and overlapping bounds. Any error throws.
2. Every area's tiles are added, with `AddArea(id, tiles)`, which also registers the area in `areas`.
3. Every feature record is passed to the `WorldFeatureType` registered for its data type. A null record (its class is missing) or an unregistered type throws, naming the area.

The root registers `PlayerSpawnFeatureType`, `NpcFeatureType`, and `SpawnZoneFeatureType<SpawnZoneData>` before loading. The player spawn throws on a second spawn. NPCs and spawn zones are described in [NPCs and AI](npcs.md).

## Area activation

`AreaActivationSystem` runs once per tick after the phases, and once at startup. It measures from the camera controller's `GetTarget`.

- The current area, the one containing the target's tile, is always active, along with every area touching it. Sharing a corner counts as touching (`AreaActivationSystem.Touches`).
- As a backup, an inactive area also activates when its bounds overlap the view plus the activate margin around the target.
- An active area deactivates only when it doesn't touch the current area and no longer overlaps the view plus the deactivate margin.
- If the target is outside every area, only the margins apply.

The view size and margins are on `WorldSettingsSO`: 15×11 tiles, 4 tiles to activate, and 8 to deactivate. The deactivate margin is never less than the activate margin. The test world's areas run C, A, B from west to east. Starting in A, all three are active. Walking into C deactivates B once it's beyond the margin, and walking back into A activates it again.

In a row of areas A, B, C, entering C deactivates A once it's beyond the margin, and going back into B loads it again. A is a whole area away, so this costs a load but nothing pops into view.

## Scene streaming

`AreaSceneLoader` runs once per frame, after the ticks, and only reads the active flags. Each area's scene is in one of four states: unloaded, loading, loaded, or unloading.

- An active, unloaded area starts an additive `LoadSceneAsync`. An inactive, loaded area starts an `UnloadSceneAsync`.
- A scene mid-load or mid-unload finishes first; the loader catches up on a later frame.
- A scene already open when Play starts counts as loaded, so opening area scenes in the Editor doesn't duplicate them.
- Each finished load and unload logs its time: `Grid World: loaded area 'TEST_b' in N ms.`
- A scene missing from the build's scene list logs an error.

The bake adds each area scene to the build's scene list, so this normally can't happen.
