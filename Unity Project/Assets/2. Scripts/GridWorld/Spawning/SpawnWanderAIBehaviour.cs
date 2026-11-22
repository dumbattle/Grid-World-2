using UnityEngine;

namespace GridWorld {
    /// <summary>Each mob takes random steps within the zone's region, waiting between them.</summary>
    public class SpawnWanderAIBehaviour : SpawnAIBehaviour {
        [Tooltip("Shortest wait between steps, in ticks. The game runs 60 ticks a second.")]
        [SerializeField, Min(0)] int minWaitTicks = 30;
        [Tooltip("Longest wait between steps, in ticks.")]
        [SerializeField, Min(0)] int maxWaitTicks = 120;

        public override SpawnAIData Bake(WorldFeatureBaker baker) {
            if (maxWaitTicks < minWaitTicks) {
                baker.LogError(this, $"Spawn zone '{name}' has a wander wait whose max is less than its min.");
                return null;
            }
            return new SpawnWanderAIData(pauses, minWaitTicks, maxWaitTicks);
        }
    }
}
