# World state and areas

How world state is held, how systems reach it, how the world starts up, and how area scenes stream. The design rules are in [architecture.md](../.claude/rules/architecture.md#state-and-contexts).

Code: `GridWorld/WorldInstance.cs`, `Contexts/`, `Permissions/`, `Functions/`, `World/`, `Areas/`.

## `WorldInstance`

Owns all overworld state as public fields. Only the root holds it.

| Field | What it is | Written by |
|---|---|---|
| `entityIds`, `components` | Entity IDs and component stores (`tile`, `facing`, `definition`, `step`) | Startup, movement |
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
| `WorldSetupContext` | SetupWorld, ReadEntities | World startup and feature types |
| `DecisionPhaseContext` | ReadEntities, RequestMove, PlayerControl | `PlayerInputSystem` |
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

## Startup

`WorldStartup.Load(ctx, worldData, catalogue)`, called by the root:

1. `WorldValidation.FindError` checks for missing areas, empty or duplicate IDs, and overlapping bounds. Any error throws.
2. Every area's tiles are added, with `AddArea(id, tiles)`, which also registers the area in `areas`.
3. Every feature record is passed to the `WorldFeatureType` registered for its data type. A null record (its class is missing) or an unregistered type throws, naming the area.

The root registers `PlayerSpawnFeatureType` before loading. It spawns the player and throws on a second spawn.

## Area activation

`AreaActivationSystem` runs once per tick after the phases, and once at startup. It measures from the camera controller's `GetTarget`.

- The current area, the one containing the target's tile, is always active, along with every area touching it. Sharing a corner counts as touching (`AreaActivationSystem.Touches`).
- As a backup, an inactive area also activates when its bounds overlap the view plus the activate margin around the target.
- An active area deactivates only when it doesn't touch the current area and no longer overlaps the view plus the deactivate margin.
- If the target is outside every area, only the margins apply.

The view size and margins are on `WorldSettingsSO`: 15×11 tiles, 4 tiles to activate, and 8 to deactivate. The deactivate margin is never less than the activate margin. In the test world, A and B touch, so both are active from the start.

In a row of areas A, B, C, entering C deactivates A once it's beyond the margin, and going back into B loads it again. A is a whole area away, so this costs a load but nothing pops into view.

## Scene streaming

`AreaSceneLoader` runs once per frame, after the ticks, and only reads the active flags. Each area's scene is in one of four states: unloaded, loading, loaded, or unloading.

- An active, unloaded area starts an additive `LoadSceneAsync`. An inactive, loaded area starts an `UnloadSceneAsync`.
- A scene mid-load or mid-unload finishes first; the loader catches up on a later frame.
- A scene already open when Play starts counts as loaded, so opening area scenes in the Editor doesn't duplicate them.
- Each finished load and unload logs its time: `Grid World: loaded area 'test_b' in N ms.`
- A scene missing from the build's scene list logs an error.

The bake adds each area scene to the build's scene list, so this normally can't happen.
