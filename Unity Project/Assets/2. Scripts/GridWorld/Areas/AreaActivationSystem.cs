using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Activates an area once it's within the view plus the activate margin of the camera target,
    /// and deactivates it only beyond the larger deactivate margin, so walking back and forth across
    /// a border doesn't reload anything.
    /// </summary>
    public class AreaActivationSystem {
        Vector2 halfView;
        float activateMargin;
        float deactivateMargin;

        public AreaActivationSystem(WorldSettingsSO settings) {
            halfView = (Vector2)settings.viewTiles / 2f;
            activateMargin = settings.activateMarginTiles;
            deactivateMargin = settings.deactivateMarginTiles;
        }

        public void Run(AreaActivationContext ctx, Vector2 target) {
            int count = ctx.GetAreaCount();

            for (int i = 0; i < count; i++) {
                var bounds = ctx.GetAreaBounds(i);
                bool active = ctx.IsAreaActive(i);

                if (!active && Overlaps(bounds, target, activateMargin)) {
                    ctx.SetAreaActive(i, true);
                }
                else if (active && !Overlaps(bounds, target, deactivateMargin)) {
                    ctx.SetAreaActive(i, false);
                }
            }
        }

        bool Overlaps(RectInt bounds, Vector2 target, float margin) {
            var extent = halfView + new Vector2(margin, margin);
            return bounds.xMin < target.x + extent.x && bounds.xMax > target.x - extent.x
                && bounds.yMin < target.y + extent.y && bounds.yMax > target.y - extent.y;
        }
    }
}
