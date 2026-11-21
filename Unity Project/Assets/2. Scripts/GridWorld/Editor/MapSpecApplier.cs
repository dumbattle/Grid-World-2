using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace GridWorld {
    /// <summary>
    /// Applies a spec to an existing area, painting only the cells that differ. Changes are recorded
    /// for Undo and the scene is left unsaved, so they can be checked before saving bakes them.
    /// Building a new area paints through the same path, from an empty area.
    /// </summary>
    public static class MapSpecApplier {
        const string UndoName = "Apply Spec to Area";

        /// <summary>
        /// Checks the spec, opens the area's scene if it isn't open, and works out the changes. If the
        /// plan has errors, any scene it opened is closed again.
        /// </summary>
        public static MapSpecApplyPlan Prepare(MapSpec spec, MapLegend legend) {
            var plan = new MapSpecApplyPlan { spec = spec, legend = legend };
            plan.errors.AddRange(MapSpecChecks.Find(spec, legend, MapSpecMode.Apply));
            if (plan.errors.Count > 0) {
                return plan;
            }

            var world = AssetDatabase.LoadAssetAtPath<WorldDataSO>(spec.world);
            var data = MapSpecChecks.FindArea(world, spec.id);
            plan.scene = SceneManager.GetSceneByPath(data.scenePath);
            if (!plan.scene.isLoaded) {
                if (NewAreaCreator.HasUntitledScene()) {
                    plan.errors.Add("Unity can't open the area's scene next to an untitled one. Save it first, or open the area's scene.");
                    return plan;
                }
                plan.scene = EditorSceneManager.OpenScene(data.scenePath, OpenSceneMode.Additive);
                plan.openedScene = true;
            }

            plan.area = FindInScene(plan.scene, spec.id);
            MapAreaSnapshot snapshot = null;
            if (plan.area == null) {
                plan.errors.Add($"{data.scenePath} has no {nameof(AreaBehaviour)} with ID '{spec.id}'.");
            }
            else {
                snapshot = Snapshot(plan.area, plan.errors, plan.warnings);
            }
            if (snapshot == null) {
                Cancel(plan);
                return plan;
            }

            plan.changes = MapSpecDiff.Compute(spec, legend, snapshot);
            plan.warnings.AddRange(plan.changes.warnings);
            return plan;
        }

        /// <summary>Paints the plan's changes as one Undo step and marks the scene dirty. Doesn't save.</summary>
        public static void Commit(MapSpecApplyPlan plan) {
            Undo.SetCurrentGroupName(UndoName);
            int group = Undo.GetCurrentGroup();
            Paint(plan.area, plan.legend, plan.changes, true);
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(plan.area.gameObject.scene);
        }

        /// <summary>Closes the area's scene if <see cref="Prepare"/> opened it.</summary>
        public static void Cancel(MapSpecApplyPlan plan) {
            if (plan.openedScene && plan.scene.isLoaded) {
                EditorSceneManager.CloseScene(plan.scene, true);
            }
            plan.openedScene = false;
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

        /// <summary>
        /// Sets each changed cell, making any tilemap the area doesn't have yet. With
        /// <paramref name="recordUndo"/>, the caller groups the Undo records.
        /// </summary>
        internal static void Paint(AreaBehaviour area, MapLegend legend, MapSpecChanges changes, bool recordUndo) {
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
                    if (recordUndo) {
                        Undo.RegisterCompleteObjectUndo(tilemap, UndoName);
                    }
                }
                else if (name2tilemap.TryGetValue(pair.Key, out tilemap)) {
                    if (recordUndo) {
                        Undo.RegisterCompleteObjectUndo(tilemap, UndoName);
                    }
                }
                else {
                    tilemap = MapSpecBuilder.FindOrCreateTilemap(area, pair.Key, legend);
                    if (recordUndo) {
                        Undo.RegisterCreatedObjectUndo(tilemap.gameObject, UndoName);
                    }
                }
                tilemap.SetTiles(pair.Value.ToArray(), layer2tiles[pair.Key].ToArray());
            }
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
