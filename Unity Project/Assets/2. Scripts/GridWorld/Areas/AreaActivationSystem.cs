using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Keeps the area the camera target stands in active, along with every area touching it, edges
    /// and corners alike. As a backup, an area also activates within the view plus the activate
    /// margin of the target. An area deactivates only when neither rule holds and it's beyond the
    /// larger deactivate margin, so walking back and forth across a border doesn't reload anything.
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
            int current = FindCurrentArea(ctx, target);
            var currentBounds = current >= 0 ? ctx.GetAreaBounds(current) : default;

            for (int i = 0; i < count; i++) {
                var bounds = ctx.GetAreaBounds(i);
                bool active = ctx.IsAreaActive(i);
                bool near = current >= 0 && (i == current || Touches(bounds, currentBounds));

                if (!active && (near || Overlaps(bounds, target, activateMargin))) {
                    ctx.SetAreaActive(i, true);
                }
                else if (active && !near && !Overlaps(bounds, target, deactivateMargin)) {
                    ctx.SetAreaActive(i, false);
                }
            }
        }

        /// <summary>True if the two areas share an edge or a corner. Overlapping areas also count.</summary>
        public static bool Touches(RectInt a, RectInt b) {
            return a.xMin <= b.xMax && a.xMax >= b.xMin
                && a.yMin <= b.yMax && a.yMax >= b.yMin;
        }

        static int FindCurrentArea(AreaActivationContext ctx, Vector2 target) {
            var tile = Vector2Int.FloorToInt(target);
            int count = ctx.GetAreaCount();
            for (int i = 0; i < count; i++) {
                if (ctx.GetAreaBounds(i).Contains(tile)) {
                    return i;
                }
            }
            return -1;
        }

        bool Overlaps(RectInt bounds, Vector2 target, float margin) {
            var extent = halfView + new Vector2(margin, margin);
            return bounds.xMin < target.x + extent.x && bounds.xMax > target.x - extent.x
                && bounds.yMin < target.y + extent.y && bounds.yMax > target.y - extent.y;
        }
    }
}
