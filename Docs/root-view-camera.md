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

**Start:** loads the world with `PlayerSpawnFeatureType` registered, sets a `FollowCameraController` on the player, creates the view, camera view, and scene loader, runs area activation once, and ticks the view once so sprites exist on the first frame.

**Each frame:**

1. Store input on `world.input`.
2. For each whole tick from `TickClock`: run the phases, update active areas, and tick the view.
3. Update the scene loader.
4. Render sprites, then the camera, using the clock's `alpha`.

## `TickClock`

Turns frame time into ticks: 60 per second, at most 4 per frame. Time beyond 4 ticks is dropped, but the fraction of a tick is kept so `alpha` stays smooth. `alpha` runs from 0 to 1 between the last two ticks.

## Blending

`Step.BlendedPosition(alpha)` returns a position in tiles, between the last tick's state (at 0) and this tick's (at 1). It needs no stored copy of the last tick: a step starts at `elapsed` 1, so the previous tick is always `elapsed - 1`, even on a step's first tick. An idle step returns its tile.

`GetBlendedPosition(id, alpha)` exposes this through `RenderContext`. Sprites and the follow camera both use it, so the camera can't drift from the sprite.

## `WorldView`

A plain class that owns one `SpriteRenderer` per entity, under the root's transform.

- `Tick(ctx)`, once per tick: creates a sprite for any new entity, and sets each sprite from its facing.
- `Render(ctx, alpha)`, once per frame: positions each sprite at its blended position plus half a tile.
- `Dispose()`: destroys the sprites and placeholder textures.

Every entity gets a `PlaceholderSprites` sprite for now: an orange square spanning one tile, with a dark bar on the side it faces. Sprites draw at sorting order 10, above the area tilemaps. They're never removed, and they don't yet hide in inactive areas.

## Camera

`ICameraController` positions are in world units, where tile (x, y) spans x to x + 1.

- `GetTarget(ctx)`, once per tick: what active areas are measured from.
- `GetViewPosition(ctx, alpha)`, once per frame: the point the camera centres on.

`FollowCameraController(entity)` targets the centre of the entity's tile, and views its blended position.

`CameraView` sets the camera to orthographic, with a height of the view's tile height (11), and in `Render` moves it to the controller's view position, keeping its z. It does nothing if there's no controller.
