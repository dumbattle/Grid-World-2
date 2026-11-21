using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GridWorld {
    /// <summary>
    /// Creates a new area scene from a checked spec and paints it. It only creates areas: once an area
    /// exists, its scene is the source of truth, so a spec for a taken ID is refused.
    /// </summary>
    public static class MapSpecBuilder {
        /// <summary>
        /// Asset problems the parser can't see: the world, tiles, sorting layers, and anything the New
        /// Area checks refuse.
        /// </summary>
        public static List<string> FindErrors(MapSpec spec, MapLegend legend, bool openAdditively) {
            var errors = new List<string>();
            var source = spec.source;

            if (spec.hasRegion) {
                errors.Add($"{source}: 'region:' only works when applying to an existing area. A new area's grids cover its whole bounds.");
            }
            if (MapSpecParser.UsesKeep(spec)) {
                errors.Add($"{source}: '{MapSpecParser.Keep}' keeps a cell of an existing area, so a new area can't use it.");
            }

            var world = AssetDatabase.LoadAssetAtPath<WorldDataSO>(spec.world);
            if (world == null) {
                errors.Add($"{source}: no world data asset at '{spec.world}'.");
            }

            FindLegendErrors(legend, spec.legend, errors);

            if (world != null) {
                var error = NewAreaCreator.FindError(Request(spec, world, openAdditively));
                if (error != null) {
                    errors.Add($"{source}: {error}");
                }
            }
            return errors;
        }

        /// <summary>
        /// Returns the new area, or null after logging every problem. Without
        /// <paramref name="openAdditively"/>, open scenes close without saving, so ask first.
        /// </summary>
        public static AreaBehaviour Build(MapSpec spec, MapLegend legend, bool openAdditively) {
            var errors = FindErrors(spec, legend, openAdditively);
            if (errors.Count > 0) {
                foreach (var error in errors) {
                    Debug.LogError($"Can't build area from spec: {error}");
                }
                return null;
            }

            var placements = MapSpecLayout.Layout(spec, legend);
            var world = AssetDatabase.LoadAssetAtPath<WorldDataSO>(spec.world);
            var area = NewAreaCreator.Create(Request(spec, world, openAdditively));
            if (area == null) {
                return null;
            }

            // Tiles load after the scene opens, because opening it can unload unreferenced assets.
            var path2tile = new Dictionary<string, TileBase>();
            var layer2tilemap = new Dictionary<string, Tilemap>();
            var layer2cells = new Dictionary<string, List<Vector3Int>>();
            var layer2tiles = new Dictionary<string, List<TileBase>>();

            foreach (var placement in placements) {
                if (!layer2cells.ContainsKey(placement.layer)) {
                    layer2tilemap.Add(placement.layer, FindOrCreateTilemap(area, placement.layer, legend));
                    layer2cells.Add(placement.layer, new List<Vector3Int>());
                    layer2tiles.Add(placement.layer, new List<TileBase>());
                }
                if (!path2tile.TryGetValue(placement.tilePath, out var tile)) {
                    tile = AssetDatabase.LoadAssetAtPath<TileBase>(placement.tilePath);
                    path2tile.Add(placement.tilePath, tile);
                }
                layer2cells[placement.layer].Add((Vector3Int)placement.cell);
                layer2tiles[placement.layer].Add(tile);
            }

            foreach (var pair in layer2tilemap) {
                pair.Value.SetTiles(layer2cells[pair.Key].ToArray(), layer2tiles[pair.Key].ToArray());
            }

            // Saving bakes the painted tiles into the area's data.
            EditorSceneManager.SaveScene(area.gameObject.scene);
            return area;
        }

        static NewAreaRequest Request(MapSpec spec, WorldDataSO world, bool openAdditively) {
            var worldPath = AssetDatabase.GetAssetPath(world);
            var last = NewAreaRules.LastArea(world);

            return new NewAreaRequest {
                id = spec.id,
                world = world,
                sceneFolder = spec.sceneFolder ?? NewAreaRules.DefaultFolder(last == null ? null : last.scenePath, worldPath),
                dataFolder = spec.dataFolder ?? NewAreaRules.DefaultFolder(last == null ? null : AssetDatabase.GetAssetPath(last), worldPath),
                sceneName = NewAreaRules.SceneName(spec.id),
                dataName = NewAreaRules.DataName(spec.id),
                bounds = spec.bounds,
                openAdditively = openAdditively,
            };
        }

        /// <summary>Sorting layers and tile assets the legend names that don't exist.</summary>
        internal static void FindLegendErrors(MapLegend legend, string legendName, List<string> errors) {
            foreach (var layer in legend.layers) {
                if (!SortingLayerExists(layer.sortingLayer)) {
                    errors.Add($"legend '{legendName}': layer '{layer.name}' uses sorting layer '{layer.sortingLayer}', which doesn't exist.");
                }
            }
            foreach (var entry in legend.entries) {
                CheckTile(entry.tile, entry, legendName, errors);
                if (entry.hasLogic) {
                    CheckTile(entry.logic, entry, legendName, errors);
                }
            }
        }

        internal static Tilemap FindOrCreateTilemap(AreaBehaviour area, string layerName, MapLegend legend) {
            if (layerName == MapSpecParser.LogicLayer) {
                return area.GetComponentInChildren<LogicLayerBehaviour>(true).GetComponent<Tilemap>();
            }

            var child = area.transform.Find(layerName);
            var tilemapObject = child != null && child.GetComponent<Tilemap>() != null ? child.gameObject : NewAreaCreator.CreateTilemap(area.gameObject, layerName, "");

            var layer = Array.Find(legend.layers, l => l.name == layerName);
            var renderer = tilemapObject.GetComponent<TilemapRenderer>();
            renderer.sortingLayerName = layer.sortingLayer;
            // Tall tiles on the Y-sorted layer sort against entities one by one, not as one tilemap.
            if (layer.sortingLayer == SortingLayerNames.EntityOnGround) {
                renderer.mode = TilemapRenderer.Mode.Individual;
            }
            return tilemapObject.GetComponent<Tilemap>();
        }

        static void CheckTile(string path, MapLegendEntry entry, string legendName, List<string> errors) {
            var type = AssetDatabase.GetMainAssetTypeAtPath(path);
            if (type == null) {
                errors.Add($"legend '{legendName}': symbol '{entry.symbol}' uses '{path}', which doesn't exist.");
            }
            else if (!typeof(TileBase).IsAssignableFrom(type)) {
                errors.Add($"legend '{legendName}': symbol '{entry.symbol}' uses '{path}', which is a {type.Name}, not a tile.");
            }
        }

        static bool SortingLayerExists(string name) {
            foreach (var layer in SortingLayer.layers) {
                if (layer.name == name) {
                    return true;
                }
            }
            return false;
        }
    }
}
