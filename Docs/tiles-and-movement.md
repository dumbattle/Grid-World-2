# Tiles and movement

Tile data, occupancy, steps, and how move requests and player input are resolved each tick. The design rules are in [architecture.md](../.claude/rules/architecture.md#tile-data) and [Movement](../.claude/rules/architecture.md#movement).

Code: `GridWorld/Tiles/`, `Movement/`, `Input/`, `Phases/`.

## Tile data

Each tile is a `ushort`. `TileBits` names the framework's low 8 bits:

| Bit | Value | Meaning |
|---|---|---|
| `EnterUp` | 1 | A step moving up may enter |
| `EnterDown` | 2 | A step moving down may enter |
| `EnterLeft` | 4 | A step moving left may enter |
| `EnterRight` | 8 | A step moving right may enter |
| `EnterAll` | 15 | All four |

Bits 16 to 128 are reserved for the framework, and the high 8 bits belong to the game.

`WorldTiles.Get(tile)` checks tile changes first, then each `AreaTiles` in turn, and returns 0 (blocked) for a tile outside every area. An `AreaTiles` holds one area's tiles in world coordinates, indexed `(x - xMin) + (y - yMin) * width`.

`TileOccupancy` maps each tile to the one entity on it. `Claim` throws if another entity holds the tile, and `Release` only releases the tile's own occupant.

## Steps

`Step` is a readonly struct: `from`, `to`, `duration`, and `elapsed`.

| State | When |
|---|---|
| `Idle` | `duration` is 0 |
| `Moving` | `elapsed` < `duration` |
| `Finished` | `elapsed` ≥ `duration` |

The entity's tile is already `to` while a step runs. `BlendedPosition(alpha)` gives the view's position, described in [root, view, and camera](root-view-camera.md#blending).

## A tick

`PhaseExecutionManager.Tick(world)` runs `PlayerInputSystem` (decision), then `MovementSystem` (movement). There's no cutscene phase yet.

### Requests

A `MoveRequest` is a direction plus `turnOnly` and `run`. `RequestMove` writes one per entity into `id2moveRequest`, and the latest wins.

### `MovementSystem.Run`

1. Collect every moving step and every finished step, before anything changes.
2. Stage requests from entities that aren't moving, then sort them: the player first, then lowest y, then lowest x.
3. For each staged request: set the facing; stop if it's turn-only; stop if the target tile doesn't allow entry in that direction or holds an entity. Otherwise `BeginStep` moves the tile and occupancy and starts the step at `elapsed` 1, using `walkTicks` or `runTicks` from the entity's definition.
4. Clear the requests.
5. Advance the steps that were moving at the start of the tick.
6. End each step that was finished at the start of the tick, unless it started a new step this tick.

Steps 5 and 6 give a constant rate across consecutive steps: a 16-tick step finishes on one tick, and the next one starts at `elapsed` 1 on the following tick.

An entity blocked by one that moves later in the sorted pass simply retries next tick. Swaps can't happen, because each target is still occupied when it's checked.

## Player input

`PlayerInput` keeps the held directions in press order, so the most recently pressed one wins, plus `runHeld`. The root writes it each frame from the `Player/Move` and `Player/Sprint` actions: a stick counts as held past 0.5 on an axis.

`PlayerInputSystem` (with `PlayerControlState` for its hold timer):

| Situation | Result |
|---|---|
| Nothing held | Reset the hold timer |
| Idle, new direction, not facing it | Turn only, then walk once it's held for `TurnHoldTicks` (6) |
| Idle, new direction, already facing it | Walk at once |
| Moving | Remember the direction; no request |
| Finished | Request the next step in the held direction, with no delay |

`run` is read whenever a request is made, so speed only changes when a step starts.
