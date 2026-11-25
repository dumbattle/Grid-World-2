using System;
using UnityEngine;

namespace GridWorld {
    [Serializable]
    public sealed class WanderAIData : NpcAIData {
        [SerializeField] RectInt localRegion;
        [SerializeField] int minWait;
        [SerializeField] int maxWait;

        /// <summary>In area-local tiles.</summary>
        public RectInt region => localRegion;

        public WanderAIData(bool pauseWhenAreaInactive, RectInt localRegion, int minWaitTicks, int maxWaitTicks) : base(pauseWhenAreaInactive) {
            this.localRegion = localRegion;
            minWait = minWaitTicks;
            maxWait = maxWaitTicks;
        }

        public override void Validate(WorldSetupContext ctx, AreaDataSO area, string npc) {
            var bounds = area.localBounds;
            bool inside = localRegion.xMin >= bounds.xMin && localRegion.yMin >= bounds.yMin
                       && localRegion.xMax <= bounds.xMax && localRegion.yMax <= bounds.yMax;
            if (!inside) {
                throw new InvalidOperationException($"{npc} has a wander region {localRegion} that goes outside the area's bounds {bounds}.");
            }
        }

        protected override EntityAI CreateAI(AreaDataSO area) {
            var region = new RectInt(area.ToWorldTile(localRegion.position), localRegion.size);
            return new WanderAI(region, minWait, maxWait);
        }
    }
}
