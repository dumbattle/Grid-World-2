using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// One interact press: who pressed, the tile they faced, and the entity on it, which is invalid
    /// if the tile was empty. Written in the decision phase and read in the next tick's cutscene phase.
    /// </summary>
    public readonly struct InteractAttempt {
        public readonly EntityID entity;
        public readonly Vector2Int tile;
        public readonly EntityID target;

        public InteractAttempt(EntityID entity, Vector2Int tile, EntityID target) {
            this.entity = entity;
            this.tile = tile;
            this.target = target;
        }
    }
}
