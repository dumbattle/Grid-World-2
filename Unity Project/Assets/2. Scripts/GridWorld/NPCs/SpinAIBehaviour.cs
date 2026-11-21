using UnityEngine;

namespace GridWorld {
    /// <summary>The NPC stays on its tile and turns to a random direction now and then.</summary>
    public class SpinAIBehaviour : NpcAIBehaviour {
        [Tooltip("Shortest wait between turns, in ticks. The game runs 60 ticks a second.")]
        [SerializeField, Min(0)] int minWaitTicks = 60;
        [Tooltip("Longest wait between turns, in ticks.")]
        [SerializeField, Min(0)] int maxWaitTicks = 180;

        public override NpcAddonData Bake(WorldFeatureBaker baker, Vector2Int localTile) {
            if (maxWaitTicks < minWaitTicks) {
                baker.LogError(this, $"NPC '{name}' has a spin wait whose max is less than its min.");
                return null;
            }
            return new SpinAIData(pauses, minWaitTicks, maxWaitTicks);
        }
    }
}
