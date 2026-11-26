using LPE;
using UnityEngine;

namespace GridWorld {
    public static class TeleportFunctions {
        // How far a teleport looks for a free tile when its destination is taken, in tiles.
        public const int SearchRange = 8;

        /// <summary>
        /// Moves the entity to <paramref name="tile"/> at once, ignoring anything in the way, and
        /// leaves it idle there. Any move request it made this tick is dropped. If the tile holds
        /// another entity or can't be entered, it lands on the nearest free tile instead, within
        /// <see cref="SearchRange"/>. Returns false and leaves the entity where it is if there's none.
        /// </summary>
        public static bool Teleport<T>(this T ctx, EntityID id, Vector2Int tile, out Vector2Int landed)
                    where T : IWorldFunctionContext, IPerm_Teleport {
            var data = ctx.data;
            if (!FindFreeTile(data, id, tile, out landed)) {
                return false;
            }

            var components = data.components;
            var from = components.tile.Get(id);
            data.occupancy.Release(from, id);
            data.occupancy.Claim(landed, id);
            components.tile.Set(id, landed);
            components.step.Set(id, Step.Idle(landed));
            data.id2moveRequest.Remove(id);
            return true;
        }

        // Nearest by steps, then in a fixed order, so a teleport always lands in the same place.
        static bool FindFreeTile(WorldInstance data, EntityID id, Vector2Int tile, out Vector2Int found) {
            if (IsFree(data, id, tile)) {
                found = tile;
                return true;
            }

            for (int range = 1; range <= SearchRange; range++) {
                for (int dx = -range; dx <= range; dx++) {
                    int dy = range - Mathf.Abs(dx);
                    var below = new Vector2Int(tile.x + dx, tile.y - dy);
                    if (IsFree(data, id, below)) {
                        found = below;
                        return true;
                    }
                    var above = new Vector2Int(tile.x + dx, tile.y + dy);
                    if (dy != 0 && IsFree(data, id, above)) {
                        found = above;
                        return true;
                    }
                }
            }

            found = tile;
            return false;
        }

        static bool IsFree(WorldInstance data, EntityID id, Vector2Int tile) {
            var occupant = data.occupancy.Get(tile);
            if (occupant.IsValid() && occupant != id) {
                return false;
            }
            var tiles = data.tiles;
            return tiles.CanEnter(tile, Direction4.up) || tiles.CanEnter(tile, Direction4.down)
                || tiles.CanEnter(tile, Direction4.left) || tiles.CanEnter(tile, Direction4.right);
        }
    }
}
