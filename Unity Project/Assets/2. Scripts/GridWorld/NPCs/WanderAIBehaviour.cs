using UnityEngine;

namespace GridWorld {
    /// <summary>The NPC takes random steps within a rectangle of tiles, waiting between them.</summary>
    public class WanderAIBehaviour : NpcAIBehaviour {
        [Tooltip("Relative to NPC moves the region with the NPC. Area tiles keeps it in place, so the NPC's start tile can move inside it.")]
        [SerializeField] TileSpace regionSpace = TileSpace.Area;
        [Tooltip("The tiles the NPC may step onto. It must contain the NPC's tile.")]
        [SerializeField] RectInt region;
        [Tooltip("Shortest wait between steps, in ticks. The game runs 60 ticks a second.")]
        [SerializeField, Min(0)] int minWaitTicks = 30;
        [Tooltip("Longest wait between steps, in ticks.")]
        [SerializeField, Min(0)] int maxWaitTicks = 120;

        public override NpcAddonData Bake(WorldFeatureBaker baker, Vector2Int localTile) {
            var areaRegion = TileSpaces.ToArea(region, regionSpace, localTile);
            if (region.width <= 0 || region.height <= 0) {
                baker.LogError(this, $"NPC '{name}' has an empty wander region.");
                return null;
            }
            if (!areaRegion.Contains(localTile)) {
                baker.LogError(this, $"NPC '{name}' at {localTile} is outside its wander region {areaRegion}{DescribeRelative()}.");
                return null;
            }
            if (maxWaitTicks < minWaitTicks) {
                baker.LogError(this, $"NPC '{name}' has a wander wait whose max is less than its min.");
                return null;
            }
            return new WanderAIData(pauses, areaRegion, minWaitTicks, maxWaitTicks);
        }

        string DescribeRelative() {
            return regionSpace == TileSpace.Relative ? $" (set as {region} relative to the NPC)" : "";
        }

        // New components start with a relative 5x5 region around the NPC.
        void Reset() {
            regionSpace = TileSpace.Relative;
            region = new RectInt(-2, -2, 5, 5);
        }

        void OnDrawGizmosSelected() {
            var shown = TileSpaces.ToArea(region, regionSpace, TileSpaces.TileOf(transform));
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube((Vector2)shown.position + (Vector2)shown.size / 2f, (Vector2)shown.size);
        }
    }
}
