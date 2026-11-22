# NPCs and AI

How NPCs are placed, baked, and moved by their AI, and how spawn zones keep mobs alive. The design rules are in [architecture.md](../.claude/rules/architecture.md#npc-ai).

Code: `GridWorld/NPCs/` (authoring and baked records), `GridWorld/AI/` (runtime AI), and `GridWorld/Spawning/` (spawn zones).

## Placing an NPC

1. Add an object to an area scene, on the tile where the NPC starts, and give it an `NpcBehaviour` with an entity definition and a facing.
2. Optionally, add one AI profile to the same object: `SpinAIBehaviour`, `WanderAIBehaviour`, or `PatrolAIBehaviour`. An NPC with none stands still.
3. Save the scene to bake it.

Every profile has **Pause When Area Inactive**, on by default. Waits are in ticks, at 60 a second.

| Profile | Settings | Behaviour |
|---|---|---|
| Spin | Min and max wait | Stays on its tile, and turns to a random new direction after each wait |
| Wander | Region Space, region, and min and max wait | After each wait, steps once onto a random free tile it can enter inside the region. With none free, it waits again |
| Patrol | Points (tile offset, facing, wait), and Loop or Ping Pong | Walks to each point in turn. On arrival it turns to the point's facing, if set, then waits. Loop goes from the last point back to the first; Ping Pong walks the route in reverse |

### Relative and area tiles

Moving an NPC should usually move its route or region with it, so AI tiles can be offsets from the NPC's tile:

- **Patrol points are always offsets.** (0,0) is the NPC's own tile, and (0,3) is 3 tiles above it. Moving the NPC moves the whole route.
- **A wander region has a Region Space dropdown.**
  - **Relative to NPC** stores the region as an offset, so it moves with the NPC. New components start this way, with a 5×5 region around the NPC.
  - **Area tiles** stores area-local tiles, so the region stays put and you can move the NPC's start tile inside it. Components saved before the setting existed load this way.
  - Changing the dropdown converts the numbers so the region stays where it is in the scene. Undo works, and so does changing several objects at once.

The bake converts everything to area-local tiles, so the baked data and runtime don't depend on the space. The Inspector and gizmos take the NPC's tile from its position rounded down, which matches the bake for an area grid at the origin with one-unit cells.

Patrol details:

- Consecutive points must share a row or column, including last to first for Loop. Two points on one tile turn the NPC in place.
- The NPC starts heading for point 0, so it must share a row or column with it: one of point 0's offset values must be 0.
- A blocked step retries every tick until the way is clear.
- An NPC out of line with its next point, which can only happen at runtime, stands still and logs one warning. There's no `returnToRoute` yet, because it needs pathfinding.

The NPC and its profile draw gizmos: a cyan square with a facing line, the wander region, and the patrol route when selected.

## Bake

`NpcBehaviour.Bake` writes one `NpcData` record: definition, local tile, facing, and a `[SerializeReference]` list of add-on records. Each `NpcAddonBehaviour` on the object adds its own record through `Bake(baker, localTile)`, which returns null after logging an error. Wander and patrol convert their tiles to area-local tiles from `localTile` before checking them. Errors show the area-local values, plus the relative values you set where that helps.

| Problem | Result |
|---|---|
| No definition or no facing | Error, no record |
| More than one AI profile | Error, no record |
| Wander region empty, or not containing the NPC | Error, no record |
| Wander or spin max wait below min | Error, no record |
| Patrol with fewer than two points, or points not in line | Error, no record |
| NPC not in line with patrol point 0 | Error, no record |

## Startup

The root registers `NpcFeatureType`. For each record it spawns the entity at the world tile, then calls `Register(ctx, area, id)` on each add-on, in order. A null add-on, whose class went missing after the bake, throws naming the area.

AI add-ons derive from `NpcAIData`. Its `Register` calls `CreateAI(area)` for a fresh `EntityAI`, sets `pausesWhenInactive`, and adds it with `AddAI`. Building a new AI per NPC keeps runtime state out of baked data. An entity can have only one AI, and a second `AddAI` throws.

## Runtime

`AISystem` runs in the decision phase, after `PlayerInputSystem`, with an `AIContext` (read tiles, entities, and areas; request moves; use random). For each entity with an `ai` component, it calls `Decide(ctx, self)` unless:

- the entity is partway through a step, or
- its area is inactive and its AI pauses when inactive. An entity outside every area counts as inactive.

An entity that has just finished a step still decides, so a patrol walks consecutive steps without a gap. AI only submits move requests; movement resolves them like the player's, so an NPC can't walk into the player or another NPC.

Randomness comes from `WorldInstance.random`, a `System.Random` seeded at startup from **Random Seed** on `WorldSettingsSO`. The same seed and inputs give the same run.

## Adding an AI profile

1. An `EntityAI` subclass with its runtime state, whose `Decide(ctx, self)` calls `ctx.RequestMove`.
2. A `[Serializable]` record deriving from `NpcAIData`, holding area-local settings, whose `CreateAI(area)` builds the AI with world tiles.
3. An `NpcAIBehaviour` subclass whose `Bake(baker, localTile)` checks its settings and returns the record.

Other add-ons, such as dialogue later, derive from `NpcAddonBehaviour` and `NpcAddonData` instead, and register their own components in `Register`. `SpinAIBehaviour`, `SpinAIData`, and `SpinAI` are the simplest example.

## Spawn zones

A spawn zone keeps up to a set number of mobs alive inside a rectangle while its area is active. Mobs are spawned at runtime, not baked, and anything can remove them with `Despawn`.

### Placing a zone

1. Add an object to an area scene and give it a `SpawnZoneBehaviour`: a Region Space, a region, an entity definition, **Max Count** (3), and **Respawn Delay Ticks** (300). The region draws as an orange rectangle.
   - Region Space works like a wander region's, but **Relative to zone** measures from the zone object's tile. New zones start relative, with a 5×5 region around the object.
   - With **Area tiles**, the region stays put when you move the object.
2. Optionally, add one mob AI profile to the same object: `SpawnWanderAIBehaviour` or `SpawnSpinAIBehaviour`. Mobs with none stand still. Each has min and max waits and **Pause When Area Inactive**, as for NPCs. Wandering mobs wander inside the zone's region.
3. Save the scene to bake it.

| Problem | Result |
|---|---|
| Empty region, no definition, max count below 1, or a negative delay | Bake error, no record |
| More than one mob AI profile | Bake error, no record |
| Wait max below min | Bake error, no record |
| Region not inside the area's bounds | Throws at startup, naming the area |
| No definition, or the AI's class went missing after the bake | Throws at startup, naming the area |

### Runtime

The bake writes a `SpawnZoneData` record. At startup, `SpawnZoneFeatureType<SpawnZoneData>` checks it and calls its `CreateZone(area)`, which builds a `SpawnZone` with its region in world tiles. Zones live in `WorldInstance.spawnZones`.

`SpawnSystem` runs first in each tick, in its own spawn phase, and calls `Tick` on each zone in order with a `SpawnContext`. A zone's tick:

1. Forgets mobs that no longer exist, whatever removed them, and restarts the delay for each.
2. Stops if the zone is inactive. By default that's when the area holding the region's corner is inactive, so the delay doesn't count down while you're away.
3. Counts the delay down, and once it reaches 0 with fewer than **Max Count** mobs, spawns one and restarts the delay.

Zones start empty, so a zone with a 180-tick delay fills one mob every 3 seconds. A spawn picks a random tile in the region with no entity on it that can be entered from some direction, preferring tiles outside the view so mobs don't appear on screen. If every free tile is in view, it uses one anyway, and if none is free it tries again next tick. Each mob gets a random facing and its own new AI. Randomness comes from `WorldInstance.random`, so the same seed gives the same spawns.

### Extending zones

Each step is a protected virtual method on `SpawnZone`: `IsActive`, `ShouldSpawn`, `TrySpawn`, `TryChooseTile`, `CanSpawnOn`, `ChooseDefinition`, `ChooseFacing`, `CreateAI`, `OnSpawned`, and `OnRemoved`. `Tick` itself is virtual too. Call the base `OnSpawned` when overriding it, because it tracks the mob.

To use a subclass:

1. Subclass `SpawnZone` and override the steps you need.
2. Subclass `SpawnZoneData` with any extra settings, overriding `CreateZone(area)` to return your zone. `ToWorldRegion(area)` converts the region.
3. Subclass `SpawnZoneBehaviour`, overriding `Validate(baker)` and `CreateRecord(localRegion, ai)` to bake your data. `CreateRecord` receives the region already converted to area-local tiles. Its settings are readable through protected getters, including `authoredRegion` and `regionTileSpace` for the region as set in the Inspector.
4. Register a `SpawnZoneFeatureType<YourData>` in the root's catalogue.

A new mob AI profile is a `SpawnAIData` record whose `Create(zoneRegion)` builds the `EntityAI`, plus a `SpawnAIBehaviour` whose `Bake(baker)` returns it. `SpawnSpinAIData` and `SpawnSpinAIBehaviour` are the simplest example.
