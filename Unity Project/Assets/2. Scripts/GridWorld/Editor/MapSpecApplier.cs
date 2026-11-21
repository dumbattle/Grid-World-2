using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace GridWorld {
    /// <summary>
    /// Applies a spec to an existing area, painting only the cells that differ. Changes are recorded
    /// for Undo and the scene is left unsaved, so they can be checked before saving bakes them.
    /// </summary>
    public static class MapSpecApplier {
        const string UndoName = "Apply Spec to Area";

        /// <summary>The area's data, or null after adding every problem the spec has with it.</summary>
        public static AreaDataSO FindArea(MapSpec spec, MapLegend legend, List<string> errors) {
            var source = spec.source;
            MapSpecBuilder.FindLegendErrors(legend, spec.legend, errors);

            var world = AssetDatabase.LoadAssetAtPath<WorldDataSO>(spec.world);
            if (world == null) {
                errors.Add($"{source}: no world data asset at '{spec.world}'.");
                return null;
            }

            AreaDataSO data = null;
            foreach (var area in world.areas) {
                if (area != null && area.areaId == spec.id) {
                    data = area;
                }
            }
            if (data == null) {
                errors.Add($"{source}: world '{Path.GetFileNameWithoutExtension(spec.world)}' has no area '{spec.id}'. Use Build Area from Spec for a new area.");
                return null;
            }
            if (!data.worldBounds.Equals(spec.bounds)) {
                var b = data.worldBounds;
                errors.Add($"{source}: the bounds don't match area '{spec.id}' ({b.x} {b.y} {b.width} {b.height}). Changing an area's bounds isn't supported.");
            }
            if (string.IsNullOrEmpty(data.scenePath) || !File.Exists(data.scenePath)) {
                errors.Add($"{source}: area '{spec.id}' has no scene at '{data.scenePath}'. Open and save its scene to fix the path.");
            }
            return errors.Count == 0 ? data : null;
        }

        /// <summary>The scene's area with this ID, or null.</summary>
        public static AreaBehaviour FindInScene(Scene scene, string id) {
            foreach (var root in scene.GetRootGameObjects()) {
                foreach (var area in root.GetComponentsInChildren<AreaBehaviour>(true)) {
                    if (area.areaId == id) {
                        return area;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Reads the area's tilemaps. Returns null after adding errors for tilemaps the spec can't
        /// address: duplicate names, a tilemap away from the origin, or no logic layer.
        /// </summary>
        public static MapAreaSnapshot Snapshot(AreaBehaviour area, List<string> errors, List<string> warnings) {
            var snapshot = new MapAreaSnapshot();
            var logic = LogicTilemap(area, warnings);
            if (logic == null) {
                errors.Add($"area '{area.areaId}' has no tilemap with a {nameof(LogicLayerBehaviour)}.");
            }

            var names = new HashSet<string>();
            foreach (var tilemap in area.GetComponentsInChildren<Tilemap>(true)) {
                // Cells are read as world tiles, which only holds for a tilemap at the origin.
                if (tilemap.transform.position != Vector3.zero) {
                    errors.Add($"tilemap '{tilemap.name}' isn't at the origin, so its cells aren't world tiles.");
                    continue;
                }
                if (tilemap.GetComponent<LogicLayerBehaviour>() != null) {
                    if (tilemap == logic) {
                        Read(tilemap, snapshot.logic);
                    }
                    continue;
                }
                if (!names.Add(tilemap.name)) {
                    errors.Add($"two tilemaps are named '{tilemap.name}'. Rename one, since specs find tilemaps by name.");
                    continue;
                }
                Read(tilemap, snapshot.Layer(tilemap.name));
            }
            return errors.Count == 0 ? snapshot : null;
        }

        /// <summary>Paints the changes, making any tilemap the area doesn't have yet. Doesn't save.</summary>
        public static void Apply(AreaBehaviour area, MapLegend legend, MapSpecChanges changes) {
            Undo.SetCurrentGroupName(UndoName);
            int group = Undo.GetCurrentGroup();

            var name2tilemap = new Dictionary<string, Tilemap>();
            foreach (var tilemap in area.GetComponentsInChildren<Tilemap>(true)) {
                if (tilemap.GetComponent<LogicLayerBehaviour>() == null) {
                    name2tilemap[tilemap.name] = tilemap;
                }
            }

            var layer2cells = new Dictionary<string, List<Vector3Int>>();
            var layer2tiles = new Dictionary<string, List<TileBase>>();
            var path2tile = new Dictionary<string, TileBase>();
            foreach (var change in changes.changes) {
                if (!layer2cells.ContainsKey(change.layer)) {
                    layer2cells.Add(change.layer, new List<Vector3Int>());
                    layer2tiles.Add(change.layer, new List<TileBase>());
                }
                TileBase tile = null;
                if (change.tilePath != null && !path2tile.TryGetValue(change.tilePath, out tile)) {
                    tile = AssetDatabase.LoadAssetAtPath<TileBase>(change.tilePath);
                    path2tile.Add(change.tilePath, tile);
                }
                layer2cells[change.layer].Add((Vector3Int)change.cell);
                layer2tiles[change.layer].Add(tile);
            }

            foreach (var pair in layer2cells) {
                Tilemap tilemap;
                if (pair.Key == MapSpecParser.LogicLayer) {
                    tilemap = LogicTilemap(area, new List<string>());
                    Undo.RegisterCompleteObjectUndo(tilemap, UndoName);
                }
                else if (name2tilemap.TryGetValue(pair.Key, out tilemap)) {
                    Undo.RegisterCompleteObjectUndo(tilemap, UndoName);
                }
                else {
                    tilemap = MapSpecBuilder.FindOrCreateTilemap(area, pair.Key, legend);
                    Undo.RegisterCreatedObjectUndo(tilemap.gameObject, UndoName);
                }
                tilemap.SetTiles(pair.Value.ToArray(), layer2tiles[pair.Key].ToArray());
            }

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(area.gameObject.scene);
        }

        static Tilemap LogicTilemap(AreaBehaviour area, List<string> warnings) {
            var layers = area.GetComponentsInChildren<LogicLayerBehaviour>(true);
            if (layers.Length == 0) {
                return null;
            }
            if (layers.Length > 1) {
                warnings.Add($"area '{area.areaId}' has {layers.Length} logic layers. Logic changes go on '{layers[0].name}', and a layer drawn above it can hide them.");
            }
            return layers[0].tilemap;
        }

        static void Read(Tilemap tilemap, Dictionary<Vector2Int, string> cells) {
            foreach (var position in tilemap.cellBounds.allPositionsWithin) {
                var tile = tilemap.GetTile(position);
                if (tile == null) {
                    continue;
                }
                var path = AssetDatabase.GetAssetPath(tile);
                cells[(Vector2Int)position] = string.IsNullOrEmpty(path) ? null : path;
            }
        }
    }
}
