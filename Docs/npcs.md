# NPCs and AI

How NPCs are placed, baked, and moved by their AI. The design rules are in [architecture.md](../.claude/rules/architecture.md#npc-ai).

Code: `GridWorld/NPCs/` (authoring and baked records) and `GridWorld/AI/` (runtime AI).

## Placing an NPC

1. Add an object to an area scene, on the tile where the NPC starts, and give it an `NpcBehaviour` with an entity definition and a facing.
2. Optionally, add one AI profile to the same object: `SpinAIBehaviour`, `WanderAIBehaviour`, or `PatrolAIBehaviour`. An NPC with none stands still.
3. Save the scene to bake it.

Every profile has **Pause When Area Inactive**, on by default. Waits are in ticks, at 60 a second.

| Profile | Settings | Behaviour |
|---|---|---|
| Spin | Min and max wait | Stays on its tile, and turns to a random new direction after each wait |
| Wander | Region, in area-local tiles, plus min and max wait | After each wait, steps once onto a random free tile it can enter inside the region. With none free, it waits again. The region starts as 5×5 tiles around the NPC when the component is added |
| Patrol | Points (tile, facing, wait), and Loop or Ping Pong | Walks to each point in turn. On arrival it turns to the point's facing, if set, then waits. Loop goes from the last point back to the first; Ping Pong walks the route in reverse |

Patrol details:

- Consecutive points must share a row or column, including last to first for Loop. Two points on one tile turn the NPC in place.
- The NPC starts heading for point 0, so it must share a row or column with it.
- A blocked step retries every tick until the way is clear.
- An NPC out of line with its next point, which can only happen at runtime, stands still and logs one warning. There's no `returnToRoute` yet, because it needs pathfinding.

The NPC and its profile draw gizmos: a cyan square with a facing line, the wander region, and the patrol route when selected.

## Bake

`NpcBehaviour.Bake` writes one `NpcData` record: definition, local tile, facing, and a `[SerializeReference]` list of add-on records. Each `NpcAddonBehaviour` on the object adds its own record through `Bake(baker, localTile)`, which returns null after logging an error.

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
