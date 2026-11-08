# Architecture

The goal is a base that other games can be built on, so these rules favor a clean boundary and simple, testable logic.

## Framework and game are separate assemblies

| Assembly | Folder | Contains | May reference |
|---|---|---|---|
| `LPE_ASM` | `2. Scripts/LPE/` | Existing library | Unity |
| `GridWorld` | `2. Scripts/GridWorld/` | Reusable overworld framework | LPE, Unity |
| `Game` | `2. Scripts/Game/` | Optional: test code that exercises the framework | GridWorld, LPE, Unity |

- **The framework never references `Game`.** The `.asmdef` files enforce this. Don't work around them with reflection, string lookups, or statics.
- **`Game` is for testing only, if it's used at all.** Real functionality goes in `GridWorld`, and no real game is built in this project. Create `Game` only when a test needs it.
- **The test for framework code:** would a different game on this base want it unchanged? If it names a specific character, place, item, or story beat, it doesn't belong in `GridWorld`.
- The framework exposes interfaces, events, and ScriptableObject definitions. Games plug into them.
- **No speculative extension points.** Add one when real code needs it, not because a future game might.

## The grid is the truth

- An entity's logical position is an integer tile coordinate (`Vector2Int`). Sprite positions are derived from it and never read back into logic.
- A move claims the target tile and releases the old tile when the step begins, then animates over a fixed number of ticks. See [Movement](#movement).
- **No Unity physics for movement or collision.** Walkability and occupancy are grid queries.
- Tilemaps are the authoring format. Tile properties (walkable, ledge, grass, and so on) are data the framework queries through one interface, so logic doesn't depend on `Tilemap` directly.
- Directions use `LPE.Direction4`. Characters always have a facing direction, and interaction targets the tile they face.

## World and areas

- A game has one world with one integer tile coordinate space. There are no separate maps.
- The world is made of hand-authored rectangular areas of any size, each in its own scene. Outdoor areas touch their neighbours and use real world positions.
- Interiors, caves, and upper floors are areas too. Each is authored at (0,0), and the bake step gives it a world offset. A door is a cutscene: fade, teleport, fade.
- Doors and saves store an area ID plus a local tile, never raw world coordinates, because offsets can change.

### Data is always loaded, visuals stream

- A bake step converts each area's tilemaps into plain tile data. It runs on scene save and before builds.
- All tile data and all entity state stay in `WorldInstance`. Logic never depends on an area's scene being loaded.
- Tile changes during play are stored separately from the baked data and saved.
- All tile lookups go through one query interface. Don't build region paging unless measurements show a need.

### Loading

- `WorldInstance` tracks which areas are active, updated once per tick after the phases. The area the camera target stands in is always active, along with every area touching it at an edge or corner.
- As a backup, an area also becomes active within the target's view plus a margin. It goes inactive only once it doesn't touch the target's area and is beyond a larger margin, so stepping across a border doesn't cause reloads. Both margins live in the world settings, defaulting to 4 and 8 tiles.
- Area scenes load additively and asynchronously to follow those flags. A persistent main scene holds the root, camera, and UI. A scene already open when Play starts counts as loaded.
- Each load and unload is logged with its time, so hitches show up.
- Actual loading runs outside the phases and doesn't change world state. A late load only makes visuals appear late.
- Entity sprites exist only in active areas.
- A door's fade holds until the destination area is loaded.

### Simulation

- There's no global rule for pausing distant entities. An NPC's AI can read whether its area is active and choose to pause. Logic reads the active flag, never the actual load state.

### Editor

- A "Go to area" window jumps the Scene view to any area.

## Tile data

- Each area stores one `ushort` per tile. The low 8 bits belong to the framework and the high 8 belong to the game, which names them in its own code.
- Framework bits: 4 for whether a tile can be entered by a step moving in each direction, and 4 reserved. For example, `EnterRight` lets a step moving right enter the tile.
- All logic goes through one lookup: tile changes first, then the area, then its array.
- A tile outside every area is blocked, and so is an unpainted tile inside one.
- Overlapping areas are a bake error, so every tile belongs to at most one area.
- There's one level per tile, with no height. Upper floors and caves are separate areas, and bridges are walk-over only.

### Authoring

- A `TilePropertyTableSO` on the world data maps each tile asset to its tile data, so tiles aren't subclassed and rule tiles work unchanged. An unlisted tile on a logic layer bakes as blocked, with a warning.
- Only tilemaps marked with `LogicLayerBehaviour` count, and the topmost tile wins, by sorting layer and then order in layer.
- An area scene has one `AreaBehaviour` on its grid, holding the area ID, bounds, and data assets. The bake adds the area to the world data and its scene to the build's scene list.
- An optional hidden override tilemap handles exceptions, such as an invisible wall.

## Entities

- An entity is just an `EntityID`. Its data lives in `EntityComponent<T>` stores keyed by ID, owned by `EntityComponentManager`. This follows the RTS project's pattern, but the code is rewritten to our style rather than copied.
- A few common components cover most cases: `tile`, `facing`, `definition`, `stableId`, `step`, `scripted`, `absent`, `ai`, and `dialogue`. Specialized behaviour gets its own component.
- Marker components signal by presence alone and hold no data.
- A read exposes a whole struct only when its fields are always read together.
- Components are added only through `EntityComponentManager.Register`. A store's `Add` is `internal`.
- An entity's area is worked out from its tile, so nothing updates when it crosses a border.
- The player is a normal entity. `WorldInstance` stores its ID for input and tie-breaks.
- Stores start out backed by a `Dictionary`, hidden behind the store's API. Don't use `FreeList` for them: it can't report which slots are occupied, and entities are rarely removed. If profiling shows a need, switch to arrays indexed by ID, which needs an LPE change to read the int inside `EntityID`.
- There's no entity type index or `EntityTypeIdentifier`. Components say what an entity is, and a `tile2entity` occupancy lookup covers spatial queries.

### Placement and IDs

- Entities are placed as marker objects in area scenes. The bake step exports each one to the area's data: definition, local tile, facing, and stable ID.
- All baked entities are created in `WorldInstance` when the game starts. Marker objects don't take part at runtime.
- When an area loads, pooled sprites are created for the entities currently in it, wherever they've walked to.
- `EntityID` is a runtime handle. It's never reused and never saved. Saves and cutscenes refer to baked entities by stable ID.

### Absent entities

- An entity that only appears under certain conditions is still created at startup, with the `absent` marker.
- It keeps its components and stable ID, releases its tile, has no sprite, and is skipped by AI.
- If its tile is occupied when it returns, it stays absent and retries each tick.
- There's no despawn until runtime spawning needs one. Because all components go through `Register`, a cleanup registry can be added then.

## Timing is in ticks

- One root game-loop behaviour drives all gameplay logic in integer ticks. Individual components don't run their own gameplay `Update()`.
- Movement durations and animation frames are measured in ticks, not `deltaTime`. This matches how `LPE.Animation` counts frames and keeps logic deterministic and testable.
- Scripted events (cutscenes, dialogue flow) are composed with `LPE` sequences, not Unity coroutines.

### Tick loop

- The game runs a fixed 60 ticks per second, from an accumulator of elapsed time. At most 4 ticks run in one frame, and extra time is dropped, keeping the fraction of a tick so the blend stays smooth.
- Pausing stops ticks from running.
- Sprites are drawn once per frame after any ticks have run. The view blends between the last two tick states, and nothing reads that back into logic.

### Movement timing

- Walk and run durations are data on each entity's definition. The starting values are 16 ticks per tile for walking and 8 for running.
- A step advances on the tick it starts. Consecutive steps then keep a constant rate: the old step finishes on one tick, and the next one is requested and starts on the following tick.
- A finished step clears to idle only in the following tick's movement phase, and only if no new step started.
- The walk animation continues across consecutive steps, and resets only after the entity goes idle.

## State and contexts

- `WorldInstance` owns all overworld state. Only the trusted root holds it; holding it means full authority.
- Systems receive contexts, never the instance. Each context is stateless and cached per type, and its `IPerm_*` markers define what it can do.
- Functions are extension methods constrained on a permission. They read or write state and keep it valid, with no gameplay policy. Systems own the logic.
- Never cast a context to `IWorldFunctionContext`.
- Code outside the tick, such as menus, is plain code and doesn't use contexts.

## Phases

Each tick: Cutscene → Decision → Movement, then UI and render outside the phases.

- `PhaseExecutionManager` is deliberately simple and hard-coded. A game that needs different phases writes its own manager.
- Add a phase only for a new permission or a new queue between systems.
- No system may depend on another having already run.
- Within a phase, a piece of state is read or written, never both. Exceptions: staging into a local buffer and applying in a second pass, and state owned by one phase where each entity touches only its own.
- Systems communicate through command queues, and each queue defines its own merge rule.
- Systems hold no game state. Stateless singletons are allowed.

## Game extension

- **Phases and systems:** a game writes its own root and `PhaseExecutionManager`, calling the framework's systems alongside its own. The framework ships a default manager.
- **State:** a game has its own state class, held by its trusted root next to `WorldInstance`. Don't subclass `WorldInstance`, make it generic, or add an `object` slot to it.
- **Access:** a game defines a context interface that extends `IWorldFunctionContext` and adds its state. Game functions are extension methods constrained on that interface plus the game's own `IPerm_*` markers.
- **Framework isolation:** framework systems keep using their own contexts, so they can't touch game state.
- **Components:** a game's component stores live in its state class and go through the same `Register`.

## Movement

### Requests

- The player, NPC AI, and cutscenes submit the same request: a direction, a turn-only option, and a run option. Each entity gets one per tick, and the latest wins. Only idle entities and entities that just finished a step can make one.
- The movement phase resolves requests in one staged pass, in tie-break order: player first, then lowest y, then lowest x. It applies the facing first, then tries the step, so a blocked step still turns the entity.
- A step claims the target tile and releases the old one when it begins. An entity blocked by one that moves later in the pass retries next tick.
- There's one entity per tile, and every present entity occupies its tile. A game that needs more, such as flyers, keeps its own grid.
- A step is blocked if the target tile doesn't allow entry from that direction, or holds an entity. Only the target tile's bits are checked. Swaps are impossible, because each entity's target is still occupied.
- `step` stores a from tile, a to tile, a duration, and a visual style. Movement only advances it. A cutscene can start any distance with `ScriptedStep(...)`.
- A teleport ignores anything in the way, but lands on the nearest free tile if its destination is occupied.

### Player movement

- When idle, pressing a direction the player isn't facing turns them. If it's still held after 6 ticks, they walk. If they already face that way, they walk immediately.
- While walking, holding any direction starts the next step with no delay.
- Running is read when a step starts, so speed never changes mid-step.
- When several directions are held, the most recently pressed one wins.

## Hooks

- The movement phase writes failed moves to `failedMoves`: entity, direction, target tile, and whether a tile or an entity blocked it.
- The player's interact press writes to `interactAttempts`: entity, target tile, and any entity on it.
- Finished steps go to `stepsCompleted`.
- Any system in the next tick's cutscene phase can read these queues and react. There's nothing to register.

## World features

- Authored features derive from `WorldFeatureBehaviour`, one instance per feature or one manager for several. The bake step scans each area scene for them and stores their data classes in a `[SerializeReference]` list on the area's data asset, so records can hold asset references. They stay in the scene as inert authoring objects.
- The root registers each `WorldFeatureType` in a catalogue keyed by its data type, before the world loads, passing any dependencies to its constructor. A game adds its own the same way.
- At world startup, each record's type registers its data through an initialization context. Local tiles convert to world tiles using the area's offset.
- Startup adds every area's tiles before registering any feature. Overlapping areas, duplicate area IDs, an unknown data type, and a record whose class is missing are errors naming the area. A duplicate type registration in the catalogue is an error naming both types.
- Entity placement uses this same path.

## Ledges

- Ledges aren't in the raw tile data. A ledge tile is blocked from all directions, and a separate system owns a `tile2ledge` lookup, filled by `LedgeBehaviour` records.
- When a failed move matches a ledge's direction and the landing tile is free, the system plays a small cutscene using `ScriptedStep(...)`.

## Triggers, props, and interaction

- The movement phase also writes finished steps to `stepsCompleted`: entity, from tile, and to tile. A game can read this queue directly for its own mechanics.
- `TriggerBehaviour` is a world feature with a set of tiles and a cutscene. It fires when the player finishes a step on one of its tiles, or an NPC if the trigger allows it. Entities with the `scripted` lock never fire triggers. If its cutscene can't start, the trigger does nothing and can fire again later.
- Items aren't a framework concept. `PropBehaviour` is a world feature with a stable ID, a tile, an optional sprite, a blocking option, and an interact cutscene. Ground items, signs, and hidden items are all props, and inventory belongs to the game.
- Prop state stays in `WorldInstance`, in a `tile2prop` lookup. A blocking prop marks its tile blocked through the tile changes data. Cutscenes can remove props, and removal is saved by stable ID.
- `WorldView` draws props from state with pooled sprites in active areas, on the `World` layer.
- Framework systems read `interactAttempts` and start the interact cutscene for the entity or prop on the target tile. An entity takes priority over a non-blocking prop. The player can interact only when idle.

## NPC AI

- `EntityAI` has `Decide(ctx, self)`, and is stored in the `ai` component with its own state. A stateless `AISystem` runs it in the decision phase, skipping entities that are mid-step, absent, or locked.
- AI submits move requests directly. There's no action layer.
- NPC behaviour is hand-authored and predictable. An NPC with no `ai` component stands still. The framework ships `SpinAI` (random turning), `WanderAI` (random steps within a set of tiles), and `PatrolAI` (loop or ping-pong).
- A patrol is a list of points: a tile, an optional facing, and an optional wait. Two points on the same tile turn in place. Consecutive points must share a row or column, and anything else is a bake error.
- A blocked patrol waits and retries. If an NPC is out of line with its next point, it stands still and logs a warning, unless the opt-in `returnToRoute` option is on. A cutscene that moves an NPC is responsible for putting it back.
- Shipped AIs pause when their area is inactive, with an option to turn that off.
- Randomness comes from a seeded generator on `WorldInstance`.
- An AI that needs game state uses the game's own component and system.

## Pathfinding

- A stateless A* utility in `GridWorld` searches tile data using `LPE.PriorityQueue`, with a node limit. Only cutscene `Walk(id, tile)` and `returnToRoute` use it.
- Paths ignore entities. `PathFollower` waits when an entity blocks it, and plans a new path after a set number of blocked ticks.
- Ledges are blocked tiles, and interiors aren't connected to outdoor tiles, so paths never use ledges or cross doors.

## Rendering

- `WorldView` reads state only through a read-only `RenderContext`, and never sees `WorldInstance`. The root calls it explicitly.
- `Tick()` runs once per tick: it adds and removes pooled sprites keyed by `EntityID`, and advances animations. `Render(alpha)` runs once per frame and only positions sprites.
- Sprite positions come from `step` (from, to, progress, plus `alpha`). The blend is worked out from the current step alone, treating the previous tick as `elapsed - 1`, so the view keeps no copy of the last tick. The follow camera uses the same calculation, so it can't drift from the sprite.
- Animation state comes from `step` and `facing`, not from position changes.
- Visuals come from each entity's definition. An entity with no visual gets a generated placeholder.
- A step's visual style is an optional SO reference with a clip and an arc height. Movement ignores it.
- Sorting layers are `Ground`, `World`, and `Above`. `World` sorts by Y, using a custom axis on the 2D renderer. Entity sprites and tall tilemaps share it, with bottom pivots.

## Camera

- `WorldInstance` holds the current `ICameraController`. `GetTarget` runs once per tick, and active areas are measured from it. `GetViewPosition` runs once per frame and places the Unity camera.
- The framework ships two controllers: follow an entity, and look at a position. Both keep the target exactly centred. By default the camera follows the player, using the sprite's blended position.
- Camera effects are controllers that wrap another controller and change only `GetViewPosition`, so they can't affect active areas. A game adds its own controllers the same way.
- The cutscene context exposes `CameraFollow(entity)`, `CameraLookAt(position)`, and `CameraPan(tile, ticks)`, a helper sequence that sets the look-at position each tick.
- A cutscene locks the camera when it uses it, and doesn't start if it's already locked. It restores the controller it replaced when it ends.
- There's no clamping. Art handles the world's edge, with scenery painted past the walkable tiles, and a room smaller than the view shows background around it.
- The camera isn't saved. On load, it follows the player.
- View size lives in a world settings asset, because both logic and the camera need it.
- `CameraView` updates the Unity camera in `Render(alpha)`, after sprites, through `RenderContext`. Don't use Cinemachine.

## Cutscenes and dialogue

- Dialogue is a type of cutscene. Talking to an NPC plays a small cutscene, and the cutscene context exposes `PlayDialogue(...)` or similar.
- Cutscenes are LPE sequences, advanced once per tick in the cutscene phase, and they use the cutscene context to modify the world.
- Cutscenes are hard-coded, because most are one-offs. A game registers each as a build function under a string ID, and triggers and props store that ID. An unregistered ID is an error at world startup, naming the area and ID. There's no visual editor or data format for cutscenes.
- Common cutscenes, such as door transitions and ledge hops, belong to their own systems, which build them directly. Simple NPC talk and signs belong to a talk system, which plays the text in an NPC's `dialogue` component or on a prop.
- `CutsceneBuilder` is a thin layer over LPE sequences, with linear and parallel blocks. Instant steps run with no gap between them. A game adds steps as extension methods, or passes any `ISequence`.
- `b.Extend(...)` runs a function when the steps before it finish, and that function adds more steps using up-to-date state. Decisions are plain `if`s, not `BranchSequence`.
- The builder records the entities its steps act on, and the camera if used, and `b.Lock(id)` adds others up front. If any is already locked, the cutscene is dropped. Otherwise the cutscene system locks them, advances the cutscene once per tick from `WorldInstance`'s active list, and releases them when it finishes. An extender acting on an unlocked entity is an error naming the cutscene.
- `PlayDialogue` posts a request to state on `WorldInstance`. The text box reads it outside the phases and reports back when it's done.
- Player-facing text lives in `DialogueSO` assets, never in code. Only the text box reads lines from them, so a game can add localization in one place. The framework has no localization system.
- A cutscene locks the entities it uses when it starts, and doesn't start if one is already locked. Decision systems skip locked entities, and movement keeps running. Several cutscenes can run at once.
- The framework has no world lock. To freeze the world, a game's system sets a flag in its own state, and its root stops ticking the world.
- The dialogue text box and its input run outside the phases and don't touch the world.
- `Walk(id, direction, count)` follows exact steps and is the usual choice for hand-authored walks. `Walk(id, tile)` pathfinds. A walk blocked for too many ticks times out and teleports the entity to its destination. If no path exists, it teleports immediately and logs a warning. Other stuck cases aren't solved yet: watch for them and raise them when they come up.

## Player input

- Unity input is stored on `WorldInstance`, and the player input system reads it during the decision phase through a permission.
- The root maps `Player/Move` to four held directions, where a stick counts past 0.5 on an axis, and `Player/Sprint` to run.
- Button presses stay buffered for 6 ticks, or until a tick consumes them. Held directions aren't buffered: a tick reads their current state.

## Logic is plain C#

- Game logic and state live in plain C# classes that can be tested in EditMode without a scene.
- MonoBehaviours are thin gateways: they handle transforms, rendering, input, audio, and scene references, not rules.
- Definitions and tuning data are ScriptableObjects. Runtime state is never written back to them.
- Pass dependencies explicitly. Avoid static or singleton game state, since it couples systems and leaks between tests. Stateless singletons are fine.
- Save data is kept separate from runtime objects and must not reference scene objects.

## Scope

**In scope:** grid movement and collision, tile properties, world areas and streaming, camera, NPCs and interaction, dialogue, scripted events, player and world state, saving.

**Out of scope unless I ask:** battles, creature collection, multiplayer, procedural generation. Don't add hooks for them either.
