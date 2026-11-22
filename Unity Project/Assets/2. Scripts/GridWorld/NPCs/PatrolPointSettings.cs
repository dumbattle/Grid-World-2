using System;
using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// One patrol point. On <see cref="PatrolAIBehaviour"/> its tile is an offset from the NPC, and
    /// once baked it's in area-local tiles.
    /// </summary>
    [Serializable]
    public struct PatrolPointSettings {
        [Tooltip("An offset from the NPC's tile. (0, 0) is the tile the NPC is placed on.")]
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

        /// <summary>The same point with <paramref name="offset"/> added to its tile.</summary>
        public PatrolPointSettings Offset(Vector2Int offset) {
            return new PatrolPointSettings(tile + offset, facing, waitTicks);
        }

        public PatrolPoint ToWorld(AreaDataSO area) {
            return new PatrolPoint(area.ToWorldTile(tile), facing, waitTicks);
        }
    }
}
