using System;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// The baked AI settings for a spawn zone's mobs. <see cref="CreateAI"/> runs once per mob, so
    /// runtime state never reaches baked data. Renaming or moving a subclass breaks the records
    /// already baked, so rebake after.
    /// </summary>
    [Serializable]
    public abstract class SpawnAIData {
        [SerializeField] bool pauseWhenAreaInactive = true;

        protected SpawnAIData(bool pauseWhenAreaInactive) {
            this.pauseWhenAreaInactive = pauseWhenAreaInactive;
        }

        /// <param name="zoneRegion">The zone's region, in world tiles.</param>
        public EntityAI CreateAI(RectInt zoneRegion) {
            var ai = Create(zoneRegion);
            ai.pausesWhenInactive = pauseWhenAreaInactive;
            return ai;
        }

        /// <param name="zoneRegion">The zone's region, in world tiles.</param>
        protected abstract EntityAI Create(RectInt zoneRegion);
    }
}
