using LPE;
using UnityEngine;

namespace GridWorld {
    public static class TileFunctions {
        public static bool CanEnter<T>(this T ctx, Vector2Int tile, Direction4 direction)
                    where T : IWorldFunctionContext, IPerm_ReadTiles {
            return ctx.data.tiles.CanEnter(tile, direction);
        }
    }
}
