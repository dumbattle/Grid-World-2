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

A game can subclass the root instead of writing its own. `world` is protected, and `OnEnable`, `StoreInput`, and `TickWorld` are virtual. `Game/TestWorldRootBehaviour` is the example: it adds debug keys that despawn the entity the player faces (`despawnAction`) or play the test cutscene with it (`cutsceneAction`).

## `TickClock`

Turns frame time into ticks: 60 per second, at most 4 per frame. Time beyond 4 ticks is dropped, but the fraction of a tick is kept so `alpha` stays smooth. `alpha` runs from 0 to 1 between the last two ticks.

## Blending

`Step.BlendedPosition(alpha)` returns a position in tiles, between the last tick's state (at 0) and this tick's (at 1). It needs no stored copy of the last tick: a step starts at `elapsed` 1, so the previous tick is always `elapsed - 1`, even on a step's first tick. An idle step returns its tile.

`GetBlendedPosition(id, alpha)` exposes this through `RenderContext`. Sprites and the follow camera both use it, so the camera can't drift from the sprite.

## `WorldView`

A plain class that gives each visible entity a `SpriteRenderer` from a pool, under the root's transform.

An entity is visible while `IsEntityInActiveArea(id)` is true: either end of its current step is in an active area, which for an idle entity is its own tile. Checking both ends keeps a sprite on screen for the whole of a step across the edge of an active area, in either direction.

- `Tick(ctx)`, once per tick: gives each visible entity without a sprite one from the pool, then advances its animation (below). Then it returns the sprites of entities that were despawned or are no longer visible. A returned sprite's object is turned off and stays under the root.
- `Render(ctx, alpha)`, once per frame: positions each shown sprite at its blended position plus half a tile across, so it stands on the bottom centre of its tile.
- `Dispose()`: destroys every sprite, shown or pooled, and the placeholder textures.

The pool holds a `SpriteRenderer` and an LPE `AnimationPlayer` per sprite, in an `LPE.ObjectPool`, with no prewarming and no cap. `visibleCount` and `pooledCount` report how many sprites are in use and waiting. In the Editor, a sprite's object is renamed `Entity <id>` each time it's reused.

Sprites draw on the Entity On Ground sorting layer, above the ground and doodad tilemaps. Entity art uses bottom pivots, and the sprite's sort point is its pivot, so entities sort by their feet.

### Animation

An entity's definition has an optional **Animation Set**, an `EntityAnimationSO` (*Create → Grid World → Entity Animation*). It holds idle and walk `DirectionalClips`: one `SpriteClip` per facing direction, each a list of LPE `AnimationFrameDefinition`s (sprite, ticks, tags). A frame of 0 ticks counts as 1.

Each tick, `EntityAnimationRules` picks what to play:

| Step | State | Speed |
|---|---|---|
| None, or idle | Idle | 1 |
| Moving or finished, at walk duration or slower | Walk | 1 |
| Moving or finished, faster than the definition's walk | Walk | 2 animation ticks per world tick |

A finished step counts as walking, so a chained step carries on the same cycle. The player keeps playing while the clip is the same, and restarts when the state or facing changes, including a turn mid-chain, because LPE's `AnimationPlayer.Set` always starts at frame 0. An empty walk clip plays the idle clip for that direction. With no animation set, or an empty idle clip, the entity draws its placeholder.

Real art draws untinted. The placeholder, from `PlaceholderSprites`, is a white square spanning one tile with a bottom pivot, a grey outline, and a bar on the side it faces, tinted by the **Placeholder Color** on its definition (orange by default).

### Making an animation from sprite sheets

The `EntityAnimationSO` Inspector warns about empty clips and has a **Fill From Sheet** section. A sheet has one row per direction and one column per frame.

1. Import each sheet with Sprite Mode Multiple, the game's Pixels Per Unit (one tile is one unit), and its filter mode. Slice it in the Sprite Editor with **Grid By Cell Size** and **Pivot Bottom Center**.
2. Set **Idle Sheet** and **Walk Sheet**, their ticks per frame (12 and 4 by default), and the row order from the top (Down, Up, Left, Right by default).
3. Click **Fill**. It replaces the clips for each sheet that's set, and can be undone.

`EntityAnimationSheet` does the work: it groups sprites into rows by their rects, top row first and left to right, and rejects a sheet whose row count doesn't match the row order, or a row order that repeats a direction.
## Camera

`ICameraController` positions are in world units, where tile (x, y) spans x to x + 1.

- `GetTarget(ctx)`, once per tick: what active areas are measured from.
- `GetViewPosition(ctx, alpha)`, once per frame: the point the camera centres on.

`FollowCameraController(entity)` targets the centre of the entity's tile, and views its blended position.

`IsTileInView(tile, marginTiles)`, under ReadCamera, says whether any part of a tile is inside the view (`WorldInstance.viewTiles`, centred on the controller's target) plus an optional margin. It returns false with no controller. Spawn zones use it to keep new mobs off screen.

`CameraView` sets the camera to orthographic, with a height of the view's tile height (11), and in `Render` moves it to the controller's view position, keeping its z. It does nothing if there's no controller.
