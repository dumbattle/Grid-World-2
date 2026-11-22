using UnityEngine;

namespace GridWorld {
    /// <summary>Converts authored tiles and regions between <see cref="TileSpace"/>s.</summary>
    public static class TileSpaces {
        /// <param name="origin">The authoring object's tile, in area-local tiles.</param>
        public static RectInt ToArea(RectInt region, TileSpace space, Vector2Int origin) {
            return Convert(region, space, TileSpace.Area, origin);
        }

        /// <param name="origin">The authoring object's tile, in area-local tiles.</param>
        public static RectInt Convert(RectInt region, TileSpace from, TileSpace to, Vector2Int origin) {
            return new RectInt(Convert(region.position, from, to, origin), region.size);
        }

        /// <param name="origin">The authoring object's tile, in area-local tiles.</param>
        public static Vector2Int Convert(Vector2Int tile, TileSpace from, TileSpace to, Vector2Int origin) {
            if (from == to) {
                return tile;
            }
            return to == TileSpace.Area ? tile + origin : tile - origin;
        }

        /// <summary>
        /// The tile an authoring object sits on, for gizmos and the Inspector. The bake uses the
        /// area's grid instead, which gives the same tile for a grid at the origin with one-unit cells.
        /// </summary>
        public static Vector2Int TileOf(Transform transform) {
            var position = transform.position;
            return new Vector2Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
        }
    }
}
