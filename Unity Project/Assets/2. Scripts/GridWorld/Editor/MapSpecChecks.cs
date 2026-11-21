using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GridWorld {
    /// <summary>
    /// Checks a parsed spec for the problems the parser can't see: which spec features each mode
    /// allows, the legend's tiles and sorting layers, the world, and the area being built or changed.
    /// A rule for a new header or section goes here, under the modes that allow it.
    /// </summary>
    public static class MapSpecChecks {
        /// <summary>
        /// Every problem with using the spec in this mode. <paramref name="openAdditively"/> only
        /// matters when building, where an untitled scene blocks opening the new one beside it.
        /// </summary>
        public static List<string> Find(MapSpec spec, MapLegend legend, MapSpecMode mode, bool openAdditively = false) {
            var errors = new List<string>();
            var source = spec.source;

            if (mode == MapSpecMode.Build) {
                if (spec.hasRegion) {
                    errors.Add($"{source}: 'region:' only works when applying to an existing area. A new area's grids cover its whole bounds.");
                }
                if (MapSpecParser.UsesKeep(spec)) {
                    errors.Add($"{source}: '{MapSpecParser.Keep}' keeps a cell of an existing area, so a new area can't use it.");
                }
            }

            FindLegendErrors(legend, spec.legend, errors);

            var world = AssetDatabase.LoadAssetAtPath<WorldDataSO>(spec.world);
            if (world == null) {
                errors.Add($"{source}: no world data asset at '{spec.world}'.");
                return errors;
            }

            if (mode == MapSpecMode.Build) {
                var error = NewAreaCreator.FindError(MapSpecBuilder.Request(spec, world, openAdditively));
                if (error != null) {
                    errors.Add($"{source}: {error}");
                }
            }
            else {
                FindAreaErrors(spec, world, errors);
            }
            return errors;
        }

        /// <summary>The world's area with this ID, or null.</summary>
        public static AreaDataSO FindArea(WorldDataSO world, string id) {
            foreach (var area in world.areas) {
                if (area != null && area.areaId == id) {
                    return area;
                }
            }
            return null;
        }

        /// <summary>Sorting layers and tile assets the legend names that don't exist.</summary>
        public static void FindLegendErrors(MapLegend legend, string legendName, List<string> errors) {
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

        static void FindAreaErrors(MapSpec spec, WorldDataSO world, List<string> errors) {
            var source = spec.source;
            var data = FindArea(world, spec.id);
            if (data == null) {
                errors.Add($"{source}: world '{Path.GetFileNameWithoutExtension(spec.world)}' has no area '{spec.id}'. Use Build Area from Spec for a new area.");
                return;
            }
            if (!data.worldBounds.Equals(spec.bounds)) {
                var b = data.worldBounds;
                errors.Add($"{source}: the bounds don't match area '{spec.id}' ({b.x} {b.y} {b.width} {b.height}). Changing an area's bounds isn't supported.");
            }
            if (string.IsNullOrEmpty(data.scenePath) || !File.Exists(data.scenePath)) {
                errors.Add($"{source}: area '{spec.id}' has no scene at '{data.scenePath}'. Open and save its scene to fix the path.");
            }
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
