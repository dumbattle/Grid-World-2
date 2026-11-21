using System;
using UnityEngine;

namespace GridWorld {
    [Serializable]
    public sealed class SpinAIData : NpcAIData {
        [SerializeField] int minWait;
        [SerializeField] int maxWait;

        public SpinAIData(bool pauseWhenAreaInactive, int minWaitTicks, int maxWaitTicks) : base(pauseWhenAreaInactive) {
            minWait = minWaitTicks;
            maxWait = maxWaitTicks;
        }

        protected override EntityAI CreateAI(AreaDataSO area) {
            return new SpinAI(minWait, maxWait);
        }
    }
}
