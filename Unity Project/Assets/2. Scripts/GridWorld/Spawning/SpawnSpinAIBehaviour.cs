using UnityEngine;

namespace GridWorld {
    /// <summary>Each mob stays on the tile it spawned on and turns to a random direction now and then.</summary>
    public class SpawnSpinAIBehaviour : SpawnAIBehaviour {
        [Tooltip("Shortest wait between turns, in ticks. The game runs 60 ticks a second.")]
        [SerializeField, Min(0)] int minWaitTicks = 60;
        [Tooltip("Longest wait between turns, in ticks.")]
        [SerializeField, Min(0)] int maxWaitTicks = 180;

        public override SpawnAIData Bake(WorldFeatureBaker baker) {
            if (maxWaitTicks < minWaitTicks) {
                baker.LogError(this, $"Spawn zone '{name}' has a spin wait whose max is less than its min.");
                return null;
            }
            return new SpawnSpinAIData(pauses, minWaitTicks, maxWaitTicks);
        }
    }
}
