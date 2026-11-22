using UnityEngine;

namespace GridWorld {
    public static class CameraFunctions {
        /// <summary>The current camera controller, or null if there isn't one.</summary>
        public static ICameraController GetCameraController<T>(this T ctx)
                    where T : IWorldFunctionContext, IPerm_ReadCamera {
            return ctx.data.cameraController;
        }

        /// <summary>
        /// Whether any part of the tile is inside the view, centred on the camera target, plus
        /// <paramref name="marginTiles"/> on every side. False when there's no camera controller.
        /// </summary>
        public static bool IsTileInView<T>(this T ctx, Vector2Int tile, float marginTiles = 0)
                    where T : IWorldFunctionContext, IPerm_ReadCamera {
            var data = ctx.data;
            var controller = data.cameraController;
            if (controller == null) {
                return false;
            }

            var target = controller.GetTarget(data.GetContext<RenderContext>());
            var extent = (Vector2)data.viewTiles / 2f + new Vector2(marginTiles, marginTiles);
            return tile.x < target.x + extent.x && tile.x + 1 > target.x - extent.x
                && tile.y < target.y + extent.y && tile.y + 1 > target.y - extent.y;
        }
    }
}
