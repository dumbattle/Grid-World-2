# Cutscenes

Scripted sequences that move and turn entities, built from LPE sequences and run once per tick in the cutscene phase. The design rules are in [architecture.md](../.claude/rules/architecture.md#cutscenes-and-dialogue). LPE's sequences are documented in [sequence-system.md](../Unity%20Project/Assets/2.%20Scripts/LPE/Docs/sequence-system.md).

Code: `GridWorld/Cutscenes/`, `Functions/CutsceneFunctions.cs`, `Functions/TeleportFunctions.cs`.

## Writing and starting

A cutscene is a class that implements `ICutscene`:

```cs
public interface ICutscene {
    void Build(CutsceneBuildContext ctx, CutsceneBuilder b);
}
```

- **Fields hold its settings,** such as the entities it acts on. There are no parameters: whoever creates the cutscene gives it what it needs. The same instance can be started many times, so it must not change while running. Anything that changes belongs in its sequences.
- **`CutsceneBuildContext`** can only read (ReadEntities, ReadTiles), so a build function describes what happens but can't act. Its steps do the writing.
- **Requesting:** anything with RequestCutscenes calls `RequestCutscene(cutscene)`, which queues it on `cutsceneRequests`. A null cutscene throws.
- **Building:** in the next cutscene phase, `CutsceneSystem` calls `Build` with a fresh `CutsceneBuilder`, so every start gets new sequences built from the state at that moment.

Logs name a cutscene by its type name.

## `CutsceneSystem`

Runs between Spawn and Decision with a `CutsceneSystemContext` (ReadEntities, RunCutscenes). It doesn't act on the world itself. It hands the steps a `CutsceneContext` (ReadEntities, ReadTiles, RequestMove, Teleport) from `GetStepContext()`, and the build functions the `CutsceneBuildContext` from `GetBuildContext()`. Both are under RunCutscenes, so that permission grants everything the two contexts allow. Each tick:

1. **Starts requests in order.** A request is dropped if:
   - `Build` throws (an error)
   - one of its entities doesn't exist or is already locked (a log)

   Otherwise `BeginCutscene` locks its entities and adds it to `runningCutscenes`.
2. **Advances each running cutscene once,** including ones that started this tick. When one is done, it calls `OnComplete`, and `EndCutsceneAt` unlocks its entities and removes it.

A cutscene that throws while running is stopped with an error naming it, and its entities are unlocked, so one broken cutscene can't break every tick.

## Locks

The lock is the `scripted` marker store, with values of the empty `Marker` struct. `IsLocked(id)`, under ReadEntities, reads it.

- `PlayerInputSystem` resets the player's hold timer and requests nothing while the player is locked. A held direction counts as a fresh press once the lock ends.
- `AISystem` skips locked entities.
- Movement still runs, so a cutscene moves entities with the same move requests as everything else.
- Despawning an entity removes its lock through the cleanup registry.

Because the cutscene phase runs before Decision, a cutscene's locks hold on the tick it starts, and its entities can decide on the tick it ends.

## `CutsceneBuilder`

A list of steps over an LPE `SequenceQueue`. Every step is added with `playNextImmediate`, so each one starts on the tick the step before it finishes. Each step calls `Lock(id)` for the entity it acts on, and the cutscene locks all of them when it starts.

| Step | What it does |
|---|---|
| `Walk(id, dir, count, run)` | Walks `count` tiles in a straight line, through normal move requests (below) |
| `Face(id, dir)` | Waits until the entity isn't mid-step, then makes a turn-only request. The new facing shows after that tick's movement phase |
| `Teleport(id, tile)` | Teleports at once (below). Logs an error if there's nowhere to land |
| `Wait(ticks)` | The next step starts exactly `ticks` ticks after this one does. It's a `PauseFramesSequence` of `ticks + 1`, because its first tick is the one the step before it finished on |
| `Do(action)` | Runs an action once, with no gap |
| `Parallel(a, b, ...)` | Each lambda is a branch with its own list of steps. The next step starts once every branch is done. LPE's parallel takes two or three sequences, so more branches nest in pairs |
| `Extend((ctx, b) => ...)` | Runs the function with the build context and a new builder when it's reached, so its steps read up-to-date state, then runs them |
| `Lock(id)` | Adds an entity to the lock without a step |
| `Add(sequence)` | Adds any `ISequence`. Call `Lock` for the entities it acts on |

A game adds its own steps as extension methods that call `Lock` and `Add`. The builder doesn't expose the cutscene context, so a game step's sequence gets any context it needs from the game. `cutsceneName` is there for its messages.

Once a cutscene has started, `Lock` throws for an entity it didn't already lock. So a step built in an `Extend` can only act on entities the cutscene locked when it was built. Lock others up front with `b.Lock(id)`.

### Walk

`WalkStepsSequence` sets its destination on its first tick: the entity's tile plus `dir` × `count`. A moving entity's tile is already its step's target, so a walk that starts mid-step counts from there. Each tick:

- While the entity is moving, it does nothing.
- Once it's on the destination, the walk is done.
- Otherwise it requests the next step.

It requests on the tick a step finishes, so steps keep the normal rate. It's done on the tick after its last step's final advance, which is when that step shows as finished. A 2-tile walk at 16 ticks a tile, started on tick 1, starts its steps on ticks 1 and 17 and ends on tick 33.

A walk that goes `TimeoutTicks` (120) ticks without starting a step teleports to its destination and logs a warning.

### Steps on a despawned entity

Walk, Face and Teleport check that their entity still exists. If it doesn't, the step ends at once with a warning, and the cutscene carries on.

## Teleporting

`Teleport(id, tile, out landed)`, under Teleport, moves the entity's tile and occupancy at once, leaves it idle, and drops any move request it made this tick.

If the tile holds another entity, or can't be entered from any direction, it lands on the nearest free tile instead. The search goes outward by steps, up to `SearchRange` (8), in a fixed order, so the result is always the same. If there's no free tile in range, it returns false and leaves the entity where it is.

## The test cutscene

`Game/TestFaceAndBackAwayCutscene` holds the player and the target. `TestWorldRootBehaviour` starts one when you face an entity in `TEST_Main` and press `Player/Jump` (Space):

1. The target turns to face the player.
2. Half a second later, both back away 2 tiles together.
3. After a pause, both walk back. The player is behind, so it can trail by a tick.
4. An `Extend` turns the target to face the player from wherever it ended up.
