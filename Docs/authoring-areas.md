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

## Making a world

1. Create a `WorldSettingsSO`, a `TilePropertyTableSO`, an `EntityDefinitionSO` for the player, and a `WorldDataSO` that references the settings and table.
2. Add areas with the New Area window.
3. Make a main scene with a camera and a `WorldRootBehaviour`, setting `worldData`, `viewCamera`, `moveAction` (`Player/Move`), and `runAction` (`Player/Sprint`). Don't add area scenes to it; they stream in.
4. Open the main scene alone and press Play.

## The bake

Runs when an area scene is saved, from **Grid World → Bake All**, and before every build. A failed bake before a build stops the build.

It:

- Writes one `ushort` per tile in the bounds, from the topmost logic layer's tile. An unpainted tile is 0, so it's blocked.
- Calls `Bake` on every `WorldFeatureBehaviour` in the scene, and stores their records in the area data.
- Adds the area to the world data's list, and the scene to the build's scene list.
- Checks the whole world for duplicate IDs and overlaps.

| Problem | Result |
|---|---|
| Scene not saved yet, no ID, missing data asset, or empty bounds | Error, no bake |
| World data has no tile property table | Error, no bake |
| More than one `AreaBehaviour` in the scene | Error, no bake |
| No logic layers | Warning; every tile bakes as blocked |
| A tile missing from the table | Warning naming the tile; it bakes as blocked |
| Duplicate area IDs or overlapping areas | Error naming both areas |
| A feature's own problem, such as a spawn with no definition | Error from the feature |

Every area bakes with a world offset of 0 until doors give interiors their own.

## Adding a world feature type

1. A `[Serializable]` data class deriving from `WorldFeatureData`, holding area-local tiles.
2. A `WorldFeatureBehaviour` whose `Bake(baker)` calls `baker.Add(data)`. Use `baker.ToLocalTile(position)` for tiles and `baker.LogError` for problems.
3. A `WorldFeatureType<TData>` whose `Register(ctx, area, data)` turns the record into world state, converting tiles with `area.ToWorldTile`.
4. Register an instance in the root's `WorldFeatureCatalogue` before startup.

`PlayerSpawnData`, `PlayerSpawnBehaviour`, and `PlayerSpawnFeatureType` are the example.

## The test world

**Grid World → Test → Create Test World** replaces everything in the `Game` subfolders of `1. Scenes`, `3. Sprites`, `6. Tiles`, and `8. Data`, so don't keep your own work there.

- Three 16 px placeholder tiles: `GroundA`, `GroundB`, and `Wall`.
- The table, world settings, a `Player` definition, `WorldData`, and `AreaA` and `AreaB` data.
- `TestMain`: the camera and a `World Root`.
- `Area_test_a` (0,0 to 24,16) and `Area_test_b` (24,0 to 48,16), with walls on the outer edges and one block each. The player spawns in `test_a` at (5, 5), facing down.

Open `TestMain` and press Play. Area sizes are constants in `CreateTestWorld`, to make a large-area test easy.
