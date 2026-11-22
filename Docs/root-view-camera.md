# Root, view, and camera

The game loop, how sprites are drawn from state, and how the camera follows. The design rules are in [architecture.md](../.claude/rules/architecture.md#tick-loop), [Rendering](../.claude/rules/architecture.md#rendering), and [Camera](../.claude/rules/architecture.md#camera).

Code: `GridWorld/Root/`, `Rendering/`, `Camera/`.

## `WorldRootBehaviour`

The framework's default root, and the only holder of `WorldInstance`. A game that needs more writes its own.

| Inspector field | Purpose |
|---|---|
| `worldData` | The `WorldDataSO` to load |
| `viewCamera` | The Unity camera to move |
| `moveAction` | `Player/Move` from `InputSystem_Actions` |
| `runAction` | `Player/Sprint` |

**Start:** logs an error and stops if the world data or its settings are missing. Otherwise it seeds the world's random generator and sets the view size from the settings, loads the world with `PlayerSpawnFeatureType`, `NpcFeatureType`, and `SpawnZoneFeatureType<SpawnZoneData>` registered, sets a `FollowCameraController` on the player, creates the view, camera view, and scene loader, runs area activation once, and ticks the view once so sprites exist on the first frame.

**Each frame:**

1. Store input on `world.input`, in `StoreInput()`.
2. For each whole tick from `TickClock`: run the phases in `TickWorld()`, update active areas, and tick the view.
3. Update the scene loader.
4. Render sprites, then the camera, using the clock's `alpha`.

A game can subclass the root instead of writing its own. `world` is protected, and `OnEnable`, `StoreInput`, and `TickWorld` are virtual. `Game/TestWorldRootBehaviour` is the example: it adds a debug key that despawns the entity the player faces.

## `TickClock`

Turns frame time into ticks: 60 per second, at most 4 per frame. Time beyond 4 ticks is dropped, but the fraction of a tick is kept so `alpha` stays smooth. `alpha` runs from 0 to 1 between the last two ticks.

## Blending

`Step.BlendedPosition(alpha)` returns a position in tiles, between the last tick's state (at 0) and this tick's (at 1). It needs no stored copy of the last tick: a step starts at `elapsed` 1, so the previous tick is always `elapsed - 1`, even on a step's first tick. An idle step returns its tile.

`GetBlendedPosition(id, alpha)` exposes this through `RenderContext`. Sprites and the follow camera both use it, so the camera can't drift from the sprite.

## `WorldView`

A plain class that gives each visible entity a `SpriteRenderer` from a pool, under the root's transform.

An entity is visible while `IsEntityInActiveArea(id)` is true: either end of its current step is in an active area, which for an idle entity is its own tile. Checking both ends keeps a sprite on screen for the whole of a step across the edge of an active area, in either direction.

- `Tick(ctx)`, once per tick: gives each visible entity without a sprite one from the pool, with its definition's tint, and sets each sprite from its facing. Then it returns the sprites of entities that were despawned or are no longer visible. A returned sprite's object is turned off and stays under the root.
- `Render(ctx, alpha)`, once per frame: positions each shown sprite at its blended position plus half a tile.
- `Dispose()`: destroys every sprite, shown or pooled, and the placeholder textures.

The pool is an `LPE.ObjectPool<SpriteRenderer>`, with no prewarming and no cap. `visibleCount` and `pooledCount` report how many sprites are in use and waiting. In the Editor, a sprite's object is renamed `Entity <id>` each time it's reused.

Every entity gets a `PlaceholderSprites` sprite for now: a white square spanning one tile, with a grey outline and a bar on the side it faces, tinted by the **Placeholder Color** on its definition (orange by default). Sprites draw on the Entity On Ground sorting layer, above the ground and doodad tilemaps.
## Camera

`ICameraController` positions are in world units, where tile (x, y) spans x to x + 1.

- `GetTarget(ctx)`, once per tick: what active areas are measured from.
- `GetViewPosition(ctx, alpha)`, once per frame: the point the camera centres on.

`FollowCameraController(entity)` targets the centre of the entity's tile, and views its blended position.

`IsTileInView(tile, marginTiles)`, under ReadCamera, says whether any part of a tile is inside the view (`WorldInstance.viewTiles`, centred on the controller's target) plus an optional margin. It returns false with no controller. Spawn zones use it to keep new mobs off screen.

`CameraView` sets the camera to orthographic, with a height of the view's tile height (11), and in `Render` moves it to the controller's view position, keeping its z. It does nothing if there's no controller.
