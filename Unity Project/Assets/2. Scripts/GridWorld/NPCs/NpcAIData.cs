using System;
using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// The baked settings for an NPC's AI. Each NPC gets a fresh <see cref="EntityAI"/> from
    /// <see cref="CreateAI"/>, so runtime state never reaches baked data.
    /// </summary>
    [Serializable]
    public abstract class NpcAIData : NpcAddonData {
        [SerializeField] bool pauseWhenAreaInactive = true;

        protected NpcAIData(bool pauseWhenAreaInactive) {
            this.pauseWhenAreaInactive = pauseWhenAreaInactive;
        }

        public sealed override void Register(WorldSetupContext ctx, AreaDataSO area, EntityID id) {
            var ai = CreateAI(area);
            ai.pausesWhenInactive = pauseWhenAreaInactive;
            ctx.AddAI(id, ai);
        }

        protected abstract EntityAI CreateAI(AreaDataSO area);
    }
}
