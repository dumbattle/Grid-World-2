using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace GridWorld {
    /// <summary>
    /// Converts an area scene's logic tilemaps and world features into its area data asset.
    /// </summary>
    public static class AreaBaker {
        /// <summary>The scene's area, or null if it has none. Logs an error if it has more than one.</summary>
        public static AreaBehaviour FindArea(Scene scene) {
            AreaBehaviour found = null;

            foreach (var root in scene.GetRootGameObjects()) {
                foreach (var area in root.GetComponentsInChildren<AreaBehaviour>(true)) {
                    if (found != null) {
                        Debug.LogError($"Scene '{scene.path}' has more than one area. Each area needs its own scene.", area);
                        return null;
                    }
                    found = area;
                }
            }
            return found;
        }

        /// <summary>Returns false if anything failed. Every problem is logged.</summary>
        public static bool Bake(AreaBehaviour area) {
            var scene = area.gameObject.scene;
            var label = $"Area scene '{scene.path}'";

            if (string.IsNullOrEmpty(scene.path)) {
                Debug.LogError("Save the area scene before baking it.", area);
                return false;
            }
            if (string.IsNullOrEmpty(area.areaId)) {
                Debug.LogError($"{label} has no area ID.", area);
                return false;
            }
            if (area.data == null || area.world == null) {
                Debug.LogError($"{label} needs both an area data asset and a world data asset.", area);
                return false;
            }
            if (area.world.tileProperties == null) {
                Debug.LogError($"World data '{area.world.name}' has no tile property table.", area.world);
                return false;
            }
            if (area.bounds.width <= 0 || area.bounds.height <= 0) {
                Debug.LogError($"{label} has empty bounds {area.bounds}.", area);
                return false;
            }

            var tiles = BakeTiles(area, label);
            var records = new List<WorldFeatureData>();
            var baker = new WorldFeatureBaker(area.areaId, area.grid, records);

            foreach (var root in scene.GetRootGameObjects()) {
                foreach (var feature in root.GetComponentsInChildren<WorldFeatureBehaviour>(true)) {
                    feature.Bake(baker);
                }
            }

            // Areas are authored at their real world positions for now. Interiors get offsets with doors.
            area.data.SetBakedData(area.areaId, scene.path, area.bounds, Vector2Int.zero, tiles, records);
            EditorUtility.SetDirty(area.data);
            if (area.world.AddArea(area.data)) {
                EditorUtility.SetDirty(area.world);
            }

            var worldError = WorldValidation.FindError(area.world.areas);
            if (worldError != null) {
                Debug.LogError($"World data '{area.world.name}': {worldError}", area.world);
            }

            AssetDatabase.SaveAssets();
            BuildSceneList.Add(scene.path);
            Debug.Log($"Baked area '{area.areaId}': {area.bounds.width}x{area.bounds.height} tiles, {records.Count} feature records.", area.data);
            return !baker.hasErrors && worldError == null;
        }

        /// <summary>Rebakes every area listed in every world data asset, opening scenes as needed.</summary>
        public static bool BakeAll() {
            bool succeeded = true;

            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(WorldDataSO)}")) {
                var world = AssetDatabase.LoadAssetAtPath<WorldDataSO>(AssetDatabase.GUIDToAssetPath(guid));
                var areas = new List<AreaDataSO>(world.areas);

                foreach (var data in areas) {
                    if (data == null) {
                        Debug.LogError($"World data '{world.name}' lists a missing area.", world);
                        succeeded = false;
                        continue;
                    }
                    succeeded &= BakeScene(data);
                }
            }
            return succeeded;
        }

        static bool BakeScene(AreaDataSO data) {
            var path = data.scenePath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) {
                Debug.LogError($"Area data '{data.name}' has no scene at '{path}'. Open and save its area scene to bake it.", data);
                return false;
            }

            var scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.isLoaded;
            if (opened) {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }

            var area = FindArea(scene);
            bool succeeded = area != null && Bake(area);
            if (area == null) {
                Debug.LogError($"Scene '{path}' has no area behaviour.", data);
            }

            if (opened) {
                EditorSceneManager.CloseScene(scene, true);
            }
            return succeeded;
        }

        static ushort[] BakeTiles(AreaBehaviour area, string label) {
            var bounds = area.bounds;
            var table = area.world.tileProperties;
            var layers = new List<LogicLayerBehaviour>(area.GetComponentsInChildren<LogicLayerBehaviour>(true));

            // Topmost first, so the first tile found wins.
            layers.Sort((a, b) => {
                int byLayer = b.sortingLayerValue.CompareTo(a.sortingLayerValue);
                return byLayer != 0 ? byLayer : b.sortingOrder.CompareTo(a.sortingOrder);
            });

            if (layers.Count == 0) {
                Debug.LogWarning($"{label} has no logic layers, so every tile bakes as blocked.", area);
            }

            var tiles = new ushort[bounds.width * bounds.height];
            var unlisted = new HashSet<TileBase>();

            for (int y = 0; y < bounds.height; y++) {
                for (int x = 0; x < bounds.width; x++) {
                    var cell = new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0);

                    foreach (var layer in layers) {
                        var tile = layer.tilemap.GetTile(cell);
                        if (tile == null) {
                            continue;
                        }
                        if (table.TryGetValue(tile, out var value)) {
                            tiles[x + y * bounds.width] = value;
                        }
                        else {
                            unlisted.Add(tile);
                        }
                        break;
                    }
                }
            }

            foreach (var tile in unlisted) {
                Debug.LogWarning($"{label}: tile '{tile.name}' isn't in tile property table '{table.name}', so it bakes as blocked.", tile);
            }
            return tiles;
        }
    }
}
