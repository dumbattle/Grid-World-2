using System;
using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>One authored patrol point, in area-local tiles.</summary>
    [Serializable]
    public struct PatrolPointSettings {
        [SerializeField] Vector2Int tile;
        [Tooltip("The way to face on arrival. None keeps the facing the NPC arrived with.")]
        [SerializeField] Direction4.Enum facing;
        [Tooltip("How long to wait here, in ticks. The game runs 60 ticks a second.")]
        [SerializeField, Min(0)] int waitTicks;

        public Vector2Int localTile => tile;

        public PatrolPointSettings(Vector2Int tile, Direction4.Enum facing, int waitTicks) {
            this.tile = tile;
            this.facing = facing;
            this.waitTicks = waitTicks;
        }

        public PatrolPoint ToWorld(AreaDataSO area) {
            return new PatrolPoint(area.ToWorldTile(tile), facing, waitTicks);
        }
    }
}
