using System;
using UnityEngine;

namespace GridWorld {
    [Serializable]
    public sealed class SpawnWanderAIData : SpawnAIData {
        [SerializeField] int minWait;
        [SerializeField] int maxWait;

        public SpawnWanderAIData(bool pauseWhenAreaInactive, int minWaitTicks, int maxWaitTicks) : base(pauseWhenAreaInactive) {
            minWait = minWaitTicks;
            maxWait = maxWaitTicks;
        }

        protected override EntityAI Create(RectInt zoneRegion) {
            return new WanderAI(zoneRegion, minWait, maxWait);
        }
    }
}
