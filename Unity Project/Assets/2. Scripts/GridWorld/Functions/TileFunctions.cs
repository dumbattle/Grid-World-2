using LPE;
using UnityEngine;

namespace GridWorld {
    public static class TileFunctions {
        public static bool CanEnter<T>(this T ctx, Vector2Int tile, Direction4 direction)
                    where T : IWorldFunctionContext, IPerm_ReadTiles {
            return ctx.data.tiles.CanEnter(tile, direction);
        }

        public static bool CanEnterFromAnyDirection<T>(this T ctx, Vector2Int tile)
                    where T : IWorldFunctionContext, IPerm_ReadTiles {
            return ctx.CanEnter(tile, Direction4.up) || ctx.CanEnter(tile, Direction4.down)
                || ctx.CanEnter(tile, Direction4.left) || ctx.CanEnter(tile, Direction4.right);
        }

        /// <summary>The entity on a tile, or <see cref="EntityID.INVALID"/> if it's free.</summary>
        public static EntityID GetEntityAt<T>(this T ctx, Vector2Int tile)
                    where T : IWorldFunctionContext, IPerm_ReadTiles {
            return ctx.data.occupancy.Get(tile);
        }
    }
}
