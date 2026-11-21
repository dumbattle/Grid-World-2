using UnityEngine;

namespace GridWorld {
    /// <summary>The NPC takes random steps within a rectangle of tiles, waiting between them.</summary>
    public class WanderAIBehaviour : NpcAIBehaviour {
        [Tooltip("The tiles the NPC may step onto, in area-local tiles. It must contain the NPC's tile.")]
        [SerializeField] RectInt region;
        [Tooltip("Shortest wait between steps, in ticks. The game runs 60 ticks a second.")]
        [SerializeField, Min(0)] int minWaitTicks = 30;
        [Tooltip("Longest wait between steps, in ticks.")]
        [SerializeField, Min(0)] int maxWaitTicks = 120;

        public override NpcAddonData Bake(WorldFeatureBaker baker, Vector2Int localTile) {
            if (region.width <= 0 || region.height <= 0) {
                baker.LogError(this, $"NPC '{name}' has an empty wander region.");
                return null;
            }
            if (!region.Contains(localTile)) {
                baker.LogError(this, $"NPC '{name}' at {localTile} is outside its wander region {region}.");
                return null;
            }
            if (maxWaitTicks < minWaitTicks) {
                baker.LogError(this, $"NPC '{name}' has a wander wait whose max is less than its min.");
                return null;
            }
            return new WanderAIData(pauses, region, minWaitTicks, maxWaitTicks);
        }

        // Starts the region as 5x5 tiles around the NPC.
        void Reset() {
            var tile = new Vector2Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y));
            region = new RectInt(tile - new Vector2Int(2, 2), new Vector2Int(5, 5));
        }

        void OnDrawGizmosSelected() {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube((Vector2)region.position + (Vector2)region.size / 2f, (Vector2)region.size);
        }
    }
}
