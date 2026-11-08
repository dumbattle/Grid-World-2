using System.Collections.Generic;
using System.IO;
using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Defaults and checks for the New Area window, kept free of the asset database so they can be tested.
    /// </summary>
    public static class NewAreaRules {
        static readonly Vector2Int FallbackSize = new Vector2Int(16, 16);

        public static string SceneName(string id) {
            return $"Area_{id}";
        }

        public static string DataName(string id) {
            return $"AreaData_{id}";
        }

        /// <summary>Bounds the same size as <paramref name="current"/>, touching it on the given side.</summary>
        public static RectInt NeighbourBounds(RectInt current, Direction4 direction) {
            var position = current.position;

            if (direction == Direction4.right) {
                position.x = current.xMax;
            }
            else if (direction == Direction4.left) {
                position.x = current.xMin - current.width;
            }
            else if (direction == Direction4.up) {
                position.y = current.yMax;
            }
            else if (direction == Direction4.down) {
                position.y = current.yMin - current.height;
            }
            return new RectInt(position, current.size);
        }

        /// <summary>The last listed area that still exists, or null.</summary>
        public static AreaDataSO LastArea(WorldDataSO world) {
            if (world == null) {
                return null;
            }
            for (int i = world.areas.Count - 1; i >= 0; i--) {
                if (world.areas[i] != null) {
                    return world.areas[i];
                }
            }
            return null;
        }

        /// <summary>
        /// To the right of the last area and the same size. With no areas, the view size at the origin.
        /// </summary>
        public static RectInt DefaultBounds(WorldDataSO world) {
            var last = LastArea(world);
            if (last != null) {
                return NeighbourBounds(last.worldBounds, Direction4.right);
            }

            var size = world != null && world.settings != null ? world.settings.viewTiles : FallbackSize;
            return new RectInt(Vector2Int.zero, size);
        }

        /// <summary>The folder of the first non-empty asset path, or "Assets".</summary>
        public static string DefaultFolder(params string[] assetPaths) {
            foreach (var path in assetPaths) {
                if (!string.IsNullOrEmpty(path)) {
                    return Path.GetDirectoryName(path).Replace('\\', '/');
                }
            }
            return "Assets";
        }

        /// <summary>A message for an empty or taken ID, empty bounds, or an overlap, or null if valid.</summary>
        public static string FindError(string id, RectInt bounds, IReadOnlyList<AreaDataSO> areas) {
            if (string.IsNullOrWhiteSpace(id)) {
                return "Enter an area ID.";
            }
            if (bounds.width <= 0 || bounds.height <= 0) {
                return "Bounds need a width and height.";
            }

            foreach (var area in areas) {
                if (area == null) {
                    continue;
                }
                if (area.areaId == id) {
                    return $"Area ID '{id}' is already used by '{area.name}'.";
                }
                if (area.worldBounds.Overlaps(bounds)) {
                    return $"Bounds overlap area '{area.areaId}' {area.worldBounds}.";
                }
            }
            return null;
        }
    }
}
