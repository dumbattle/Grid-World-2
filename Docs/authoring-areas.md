# Authoring areas

How to build an area scene, what the bake produces, and what the test world contains. The design rules are in [architecture.md](../.claude/rules/architecture.md#authoring) and [World features](../.claude/rules/architecture.md#world-features).

Code: `GridWorld/Authoring/`, `World/`, `Features/`, `Editor/`, and `Game/Editor/CreateTestWorld.cs`.

## Data assets

| Asset | Holds | Made by |
|---|---|---|
| `WorldDataSO` | World settings, the tile property table, and the list of areas | You. The bake adds areas |
| `WorldSettingsSO` | View size in tiles (15×11), activate margin (4), deactivate margin (8) | You |
| `TilePropertyTableSO` | Per tile asset: four enter checkboxes and 8 game bits | You |
| `AreaDataSO` | One area's ID, scene path, bounds, offset, tiles, and feature records | You create it empty. Only the bake writes it |
| `EntityDefinitionSO` | Walk and run ticks per tile (16 and 8) | You |

All of them are under **Create → Grid World**.

## Making an area scene

Use the **New Area** window. It opens from:

- **Grid World → New Area…**, with nothing filled in.
- The **New Area…** button on a `WorldDataSO`, which fills in the world, folders beside its last area, and bounds to the right of that area.
- The **New Neighbour** buttons (Left, Right, Up, Down) on an `AreaBehaviour`, which fill in its world and folders, and bounds of the same size touching that side.

Fill in the ID. The scene and data names follow it (`Area_<id>`, `AreaData_<id>`) until you edit them. **Create** is disabled, with a message, while the ID is empty or taken, the bounds overlap an area, a folder is missing, or a file already exists.

**Create** makes the scene, with a `Grid` and filled-in `AreaBehaviour`, and three tilemaps: `Ground` on the Ground sorting layer, `Doodads` on Ground Doodad, and `Logic` on EDITOR OVERLAY, with `LogicLayerBehaviour` and its renderer off. It creates the `AreaDataSO`, saves the scene so it bakes into the world's list, and leaves it open. **Open additively** keeps the current scenes open; it can't be used while an untitled scene is open. Without it, you're asked to save changed scenes first.

Code: `Editor/NewAreaWindow`, `NewAreaCreator`, `NewAreaRules`, and the `WorldDataSO` and `AreaBehaviour` inspectors.

Then:

1. Paint art on `Ground`, `Doodads`, or other tilemaps, which the bake ignores.
2. Paint the `Logic` tilemap, turning its renderer on while painting. Every walkable tile must be painted, since unpainted tiles are blocked. Add more logic layers if needed; where they overlap, the one drawn on top wins, by sorting layer and then order in layer.
3. Add every tile used on a logic layer to the tile property table.
4. Add world features, such as a `PlayerSpawnBehaviour` with a definition and facing. A world needs exactly one player spawn.
5. Save the scene. Saving bakes it.

Bounds are in tile coordinates, and the cyan gizmo shows them. Areas use real world positions, and touching areas share an edge. To change an area's ID, data asset, or bounds later, edit its `AreaBehaviour` and save.

## Deleting an area

Choose **Delete Area…** from the three-dot menu on the area's `AreaBehaviour`, then click **Delete**. Only clicking **Delete** deletes: Enter and Esc pick **Cancel** and **Keep Area**. Unity's Undo can't bring the area back.

It removes the area from every `WorldDataSO` that lists it and the scene from the build's scene list, closes the scene without saving, and moves the scene and the `AreaDataSO` to the Recycle Bin. If the scene was the only one open, an empty scene replaces it.

It refuses, with a message, in Play mode, with more than one object selected, for a scene that's never been saved, for an area with no data asset, and when the data asset's scene path doesn't match the scene. If the scene has moved, saving it fixes the path.

It doesn't check for the player spawn, so check whether the area holds it first. Neighbouring areas are left with a blocked edge where they touched it.

To restore an area, put both files and their `.meta` files back from the Recycle Bin, then open and save the scene, which bakes it back into the lists.

Code: `Editor/AreaDeleter`.

## Drafting areas from specs

A text spec describes an area as ASCII grids. One Editor command builds a new area from a spec as a painted, baked scene to polish by hand, and another applies an edited spec to an existing area, painting only the cells that changed. The scene stays the source of truth: export a fresh spec from it before editing, since hand edits since the last export would otherwise be overwritten.

Files live in `Maps/` at the repo root, outside `Assets/`, so Unity doesn't import them:

| Path | Holds |
|---|---|
| `Maps/legends/<name>.json` | A theme's legend, reused across areas |
| `Maps/areas/<id>.txt` | One area's spec |
| `Maps/previews/` | Exported PNGs. Gitignored |

### Legends

A legend lists tilemap layers bottom to top, then one entry per character. Tiles are asset paths.

```json
{
    "layers": [ { "name": "Ground", "sortingLayer": "Ground" },
                { "name": "Trees",  "sortingLayer": "Entity On Ground" } ],
    "entries": [
        { "symbol": "g", "layer": "Ground", "tile": "Assets/.../Grass Tile.asset", "logic": "Assets/.../GroundA.asset" },
        { "symbol": "T", "layer": "Trees",  "tile": "Assets/.../Trees_0.asset",   "logic": "Assets/.../Wall.asset",
          "footprint": { "x": 0, "y": 0, "width": 2, "height": 1 } } ]
}
```

- `symbol` is one character, unique in the legend. `.`, `?`, `#` and `[` are reserved.
- `logic` is optional. The Logic layer is filled from it: each cell takes the logic tile of the topmost layer whose entry covers it, so a tree over grass blocks. A cell no entry covers stays unpainted, so it's blocked. Logic tiles still need to be in the tile property table.
- `footprint` is optional, measured from the painted cell, for art that covers more than one tile. Without it, logic covers just the painted cell. Footprint cells outside the bounds are dropped.
- `Logic` can't be a legend layer name.
- Two entries can share a tile, for example the same art with different logic. A cell the spec writes uses its own symbol's entry. When applying, other cells are matched to an entry by tile, which takes the first entry listed.

`grassland.json` covers the `SAMPLE` tiles. Its trees are 2×2 sprites drawn up and right from the painted cell, with a 2×1 footprint so the player can walk behind the upper canopy.

### Specs

```
# Comments start with '#'.
id: trial_meadow
world: Assets/8. Data/Manual Test/Manual WorldData.asset
bounds: 0 16 24 10
legend: grassland
[Ground]
gggg====
gggg==gg
[Trees]
T.......
........
```

- `bounds` is x, y, width, height in world tiles. `sceneFolder:` and `dataFolder:` are optional; by default they follow the New Area window.
- Each `[Layer]` grid has one row per tile of height and one character per tile of width. The top row is the highest y. `.` leaves a cell empty.
- A symbol must belong to the layer it's used in. A `[Logic]` section isn't allowed.
- `region: x y width height` is optional, and only for applying. The grids then cover just that rectangle, which must lie inside the bounds.
- `?` keeps a cell as it is. It's only for applying, too.

### Building and previewing

**Grid World → Build Area from Spec…** picks a spec, checks it, and lists every problem with its file and line. If it's valid, it creates the area as the New Area window does, opening it additively unless an untitled scene is open. It then gives each spec layer a tilemap on the legend's sorting layer, including the ones New Area made, paints every layer and the Logic layer, and saves, which bakes. Painting works the same way as applying, compared against the empty area. Tilemaps on `Entity On Ground` sort tile by tile. To rebuild an area from its spec, delete it first.

**Export Preview** in the `AreaBehaviour` three-dot menu, or **Grid World → Export All Area Previews**, writes two PNGs to `Maps/previews/`:

- `<id>.png` draws the bounds through a camera, at the sprites' pixels per unit. Other loaded scenes show where their art overlaps.
- `<id>_logic.png` adds a tile grid, with blocked tiles in red and partly blocked ones in yellow. It reads the baked data, so it shows the scene as of its last save.

Export All opens each area scene additively and closes it again. It can't run while an untitled scene is open.

### Editing existing areas

1. Export the area's current spec with `py Tools/mapread.py spec <id>`. It writes `Maps/areas/<id>.txt`, and a legend if the area needs one.
2. Edit the grids. Use `?` for cells to leave alone, and `region` to edit only part of a large area.
3. Choose **Grid World → Apply Spec to Area…** and pick the spec.

Applying refuses a spec whose area doesn't exist, whose bounds differ from the area's, or whose area scene can't be read: no logic layer, a tilemap away from the origin, or two tilemaps with the same name. It opens the scene additively if it isn't open, which it can't do while an untitled scene is open.

It compares each cell with the scene, and lists the changes per layer, with any warnings, before you confirm. If nothing differs, it says so. On **Apply**, it paints the changes as one Undo step and marks the scene changed without saving it, so check the result before saving bakes it. Cancelling closes a scene it opened.

Logic is redone only for affected cells: changed cells, plus the footprints of their old and new art. For each one:

- If the legend gives it a logic tile, it's painted.
- If no art is left in the cell, its logic is cleared.
- If art is left but no entry has logic, its logic is kept, with a warning.
- If its logic already differed from what the legend gave before the edit, it was painted by hand. It's kept, with a warning.

Unchanged cells keep their logic, so hand-painted logic elsewhere is never touched.

### Exporting specs

`py Tools/mapread.py spec <id>` writes a spec from an area's scene. It works for areas made by hand, too.

- **Legend:** it uses `--legend NAME`, then the `legend:` header of an existing spec, then none. Tiles the legend lacks are added to a copy named `<id>.json`, or to the legend itself with `--write-legend`. With no legend, it makes `<id>.json` from the scene.
- **New symbols** come from the tile's name where possible, assigned bottom layer first and most-used tiles first.
- **New logic:** a tile gets the logic tile under a strict majority of its cells, checked top layer first. Cells a higher layer already covers aren't counted. Art that covers more than one tile gets a footprint hint as a warning, to check by hand.
- **The spec** keeps an existing spec's opening comments, and adds the Logic grid as comments for reference. `--region X Y W H` writes only that rectangle. Cells it can't name, such as tiles from outside the project, are written as `?`. It notes how many tiles lie outside the bounds.

### Reading the world from Python

`Tools/mapread.py` reads the project's text assets without the Editor, and never writes them. Only `spec` writes files, and only under `Maps/`. It needs only Python's standard library. Run it with `py`:

| Command | Prints |
|---|---|
| `overview` | Every world's areas with IDs and bounds, and an ASCII map of them |
| `area <id> [--legend NAME]` | Each tilemap in an area's scene as an ASCII grid |
| `edges --bounds X Y W H [--depth N] [--legend NAME]` | The strips of neighbouring areas along proposed bounds, three tiles deep by default, and any overlap |
| `tiles <folder>` | Tile assets in a folder, with their type and path |
| `spec <id> [--legend NAME] [--write-legend] [--region X Y W H]` | Writes an area's spec, and a legend if needed. See [Exporting specs](#exporting-specs) |

Grids use the legend's symbols where it has them, and made-up letters with a key for other tiles. The reader assumes tilemaps sit at the area's origin, and skips tiles placed through prefabs.

### Extending specs

Building and applying share one pipeline, so new spec content is added once and both commands get it:

1. **Parse** in `MapSpecParser`: a header key, or a section, plus checks against the legend that don't need assets.
2. **Check** in `MapSpecChecks.Find`, which takes a `MapSpecMode`. Rules that only one mode allows go under that mode, as `region:` and `?` do for applying. Asset checks go here too.
3. **Read the scene** in `MapSpecApplier.Snapshot`, into `MapAreaSnapshot`.
4. **Compare** in `MapSpecDiff.Compute`, adding to `MapSpecChanges`. A new area compares against an empty snapshot.
5. **Paint** in `MapSpecApplier.Paint`, which Build calls directly and `Commit` calls with Undo.

`MapSpecApplier.Prepare` runs steps 2 to 4 for an existing area, and returns a `MapSpecApplyPlan` to pass to `Commit` or `Cancel`. The menu only picks the file and shows the dialogs. If the format changes, update `Tools/mapread.py` to match. `MapSpecFilesTests` reads every committed spec and legend, so a mismatch shows up as a failed test.

Code: `Editor/MapSpecParser`, `MapSpecChecks`, `MapSpecMode`, `MapSpecLayout`, `MapLogicCover`, `MapSpecBuilder`, `MapSpecDiff`, `MapSpecApplier`, `MapSpecApplyPlan`, `MapAreaSnapshot`, `MapSpecMenu`, `AreaPreviewExporter`, and `Tools/mapread.py`.

## Making a world

1. Create a `WorldSettingsSO`, a `TilePropertyTableSO`, an `EntityDefinitionSO` for the player, and a `WorldDataSO` that references the settings and table.
2. Add areas with the New Area window.
3. Make a main scene with a camera and a `WorldRootBehaviour`, setting `worldData`, `viewCamera`, `moveAction` (`Player/Move`), and `runAction` (`Player/Sprint`). Don't add area scenes to it; they stream in. A root with no world data, or world data with no settings, logs an error and doesn't start.
4. Open the main scene alone and press Play.

## The bake

Runs when an area scene is saved, from **Grid World → Bake All**, and before every build. A failed bake before a build stops the build.

It:

- Writes one `ushort` per tile in the bounds, from the topmost logic layer's tile. An unpainted tile is 0, so it's blocked.
- Calls `Bake` on every `WorldFeatureBehaviour` in the scene, and stores their records in the area data.
- Adds the area to the world data's list, and the scene to the build's scene list.
- Checks the whole world for duplicate area IDs, overlaps, and duplicate feature IDs.

| Problem | Result |
|---|---|
| Scene not saved yet, no ID, missing data asset, or empty bounds | Error, no bake |
| World data has no tile property table | Error, no bake |
| More than one `AreaBehaviour` in the scene | Error, no bake |
| No logic layers | Warning; every tile bakes as blocked |
| A tile missing from the table | Warning naming the tile; it bakes as blocked |
| Duplicate area IDs or overlapping areas | Error naming both areas |
| A feature's own problem, such as a spawn with no definition | Error from the feature |
| A feature with no ID | Warning naming it; it still bakes |
| Two features in the scene with the same ID, or an ID with characters that aren't allowed | Error naming them, no bake |
| A feature ID also used in another area | Error naming both areas |

Every area bakes with a world offset of 0 until doors give interiors their own.

## Feature IDs

NPCs, spawn zones, and the player spawn each have an **ID** field. Saves, cutscenes, and feature edits will find a feature by it, so moving, renaming, or regrouping its object doesn't lose it.

- An ID is unique across the whole world, not just its area, and comparisons ignore case, so `Guard_1` and `guard_1` clash.
- It uses only ASCII letters, digits, `_` and `-`.
- Nothing names features automatically. New components start with no ID, and so do components saved before IDs existed.

Problems are reported everywhere you'd look:

| Where | No ID | Duplicate or bad ID |
|---|---|---|
| Inspector | Yellow box | Red box naming the other feature and its area |
| Scene view | Red square and "(no ID)" label | Red square and label. Every other feature shows its ID in white |
| Bake | Warning | Error. A clash inside the area stops it baking; a clash with another area is logged |
| Startup | Nothing | Throws, naming both areas |

The checks cover the open scenes and the baked data of every other area in the same world, so a clash with an area whose scene is closed still shows. Ctrl+D and copy-paste copy the ID, so duplicating a feature always makes a clash to fix.

### Fixing IDs

**Grid World → Fix Feature IDs**, or the button in either Inspector box, opens a window with two tabs. Every change is an Undo step, and the window never saves a scene.

**Problems** steps through clashes, then bad formats, then missing IDs, with **Prev** and **Next**.

- **A clash** shows every feature sharing the ID. Choose the one that **Keeps** the ID, then edit the others' **New ID** fields, which start as the shared ID. Each is checked against the kept ID, the group's other new IDs, and every other feature. **Apply** renames them all in one Undo step. A feature whose scene isn't open has an **Open Scene** button; it can keep the ID without opening.
- **A missing or bad ID** shows the one feature, selected and framed in the Scene view. Type an ID, then press **Set ID** or Enter.

**All Features** lists every feature in the open scenes, with its status, ID, kind, name, area, and tile.

- Click a row to select and frame it, and use the Up and Down arrows to walk through them.
- Double-click an ID to edit it in place. Enter sets it, and Escape cancels.
- Filter with the search box, which matches IDs and names, and **Problems only**. Sort by area, kind, ID, or name.
- **Show unopened areas** adds the baked features of areas whose scenes are closed, greyed out with an **Open Scene** button.

Code: `Features/FeatureIds`, `IdentifiedFeatureBehaviour`, `IdentifiedFeatureData`, and `Editor/FeatureIdChecks`, `FeatureIdIndex`, `FeatureIdInspector`, `FeatureIdGizmos`, `FeatureIdWindow`.

## Adding a world feature type

1. A `[Serializable]` data class deriving from `WorldFeatureData`, holding area-local tiles.
2. A `WorldFeatureBehaviour` whose `Bake(baker)` calls `baker.Add(data)`. Use `baker.ToLocalTile(position)` for tiles and `baker.LogError` for problems.
3. A `WorldFeatureType<TData>` whose `Register(ctx, area, data)` turns the record into world state, converting tiles with `area.ToWorldTile`.
4. Register an instance in the root's `WorldFeatureCatalogue` before startup.

For a feature that needs an ID, derive from `IdentifiedFeatureBehaviour` and `IdentifiedFeatureData` instead, and return the record from `BakeRecord(baker)` rather than calling `baker.Add`. The base bake copies the ID onto the record, and the ID checks cover the new feature automatically.

`PlayerSpawnData`, `PlayerSpawnBehaviour`, and `PlayerSpawnFeatureType` are the example. NPCs and spawn zones are feature types too, with add-on components of their own: see [NPCs and AI](npcs.md).

## The test world

**Grid World → Test → Create Test World** replaces everything in the `Test` subfolders of `1. Scenes`, `3. Sprites`, `6. Tiles`, and `8. Data`, so don't keep your own work there. Everything it makes, including the objects in its scenes, is named with a `TEST_` prefix.

- Three 16 px placeholder tiles: `TEST_GroundA`, `TEST_GroundB`, and `TEST_Wall`.
- `TEST_TilePropertyTable`, `TEST_WorldSettings`, `TEST_WorldData`, and `TEST_AreaA`, `TEST_AreaB`, and `TEST_AreaC` data.
- Definitions: `TEST_Player` (orange), `TEST_Villager` (blue), `TEST_Guard` (purple, walking at 20 ticks a tile), and `TEST_Slime` (pink, walking at 24).
- `TEST_Main`: the camera and a `TEST_World Root`, which is a `TestWorldRootBehaviour`. Pressing `Player/Attack` (Enter or left click) despawns the entity the player faces and logs it.
- Three areas in a row, with walls on the outer edges and one block each: `TEST_Area_c`, area `TEST_c` (-24,0 to 0,16); `TEST_Area_a`, area `TEST_a` (0,0 to 24,16); and `TEST_Area_b`, area `TEST_b` (24,0 to 48,16). C touches A but not B, so standing in C deactivates B. The player spawns in `TEST_a` at (5, 5), facing down.
- NPCs:
  - In `TEST_a`, a spinning villager at (3,10), a wandering villager at (15,4) in the region (12,2) to (18,6), and a guard walking back and forth on row 12 between x = 12 and 20, facing outward and waiting a second at each end.
  - In `TEST_b`, a guard looping the rectangle from (28,4) to (40,7), waiting half a second at two corners, and a wandering villager at (38,12) in the region (36,11) to (43,13).
  - In `TEST_c`, a wandering villager at (-12,7) in the region (-16,4) to (-8,9).
- In `TEST_b`, a spawn zone from (41,1) to (45,9) keeping up to 4 wandering slimes, one every 3 seconds. It stops while you're in `TEST_c`.
- Every feature has an ID: `TEST_player_spawn`, `TEST_a_spinner`, `TEST_a_wanderer`, `TEST_a_patroller`, `TEST_b_patroller`, `TEST_b_wanderer`, `TEST_b_slime_zone`, and `TEST_c_wanderer`.

Each asset gets its settings before it's first written, because values set after `CreateAsset` were sometimes lost when an asset was recreated at the same path. At the end, the tool checks that `TEST_WorldData` has its settings, its tile table, and all three areas, and logs an error instead of the success message if not.

Open `TEST_Main` and press Play. Area sizes are constants in `CreateTestWorld`, to make a large-area test easy.
