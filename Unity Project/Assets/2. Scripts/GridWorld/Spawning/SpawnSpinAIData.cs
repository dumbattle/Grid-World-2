using System;
using UnityEngine;

namespace GridWorld {
    [Serializable]
    public sealed class SpawnSpinAIData : SpawnAIData {
        [SerializeField] int minWait;
        [SerializeField] int maxWait;

        public SpawnSpinAIData(bool pauseWhenAreaInactive, int minWaitTicks, int maxWaitTicks) : base(pauseWhenAreaInactive) {
            minWait = minWaitTicks;
            maxWait = maxWaitTicks;
        }

        protected override EntityAI Create(RectInt zoneRegion) {
            return new SpinAI(minWait, maxWait);
        }
    }
}
