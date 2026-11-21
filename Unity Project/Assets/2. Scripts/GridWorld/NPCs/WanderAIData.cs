using System;
using UnityEngine;

namespace GridWorld {
    [Serializable]
    public sealed class WanderAIData : NpcAIData {
        [SerializeField] RectInt localRegion;
        [SerializeField] int minWait;
        [SerializeField] int maxWait;

        public WanderAIData(bool pauseWhenAreaInactive, RectInt localRegion, int minWaitTicks, int maxWaitTicks) : base(pauseWhenAreaInactive) {
            this.localRegion = localRegion;
            minWait = minWaitTicks;
            maxWait = maxWaitTicks;
        }

        protected override EntityAI CreateAI(AreaDataSO area) {
            var region = new RectInt(area.ToWorldTile(localRegion.position), localRegion.size);
            return new WanderAI(region, minWait, maxWait);
        }
    }
}
