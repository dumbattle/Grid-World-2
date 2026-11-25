using System;
using System.Collections.Generic;
using UnityEngine;

namespace GridWorld {
    [Serializable]
    public sealed class PatrolAIData : NpcAIData {
        [SerializeField] PatrolPointSettings[] points;
        [SerializeField] PatrolMode mode;

        /// <summary>Tiles are in area-local tiles.</summary>
        public IReadOnlyList<PatrolPointSettings> patrolPoints => points;

        public PatrolAIData(bool pauseWhenAreaInactive, PatrolPointSettings[] points, PatrolMode mode) : base(pauseWhenAreaInactive) {
            this.points = (PatrolPointSettings[])points.Clone();
            this.mode = mode;
        }

        public override void Validate(WorldSetupContext ctx, AreaDataSO area, string npc) {
            var bounds = area.localBounds;
            for (int i = 0; i < points.Length; i++) {
                var tile = points[i].localTile;
                if (!bounds.Contains(tile)) {
                    throw new InvalidOperationException($"{npc} has patrol point {i} at {tile}, outside the area's bounds {bounds}.");
                }
                if (!ctx.CanEnterFromAnyDirection(area.ToWorldTile(tile))) {
                    Debug.LogWarning($"Grid World: {npc} has patrol point {i} at {tile}, which can't be entered from any direction.");
                }
            }
        }

        protected override EntityAI CreateAI(AreaDataSO area) {
            var worldPoints = new PatrolPoint[points.Length];
            for (int i = 0; i < points.Length; i++) {
                worldPoints[i] = points[i].ToWorld(area);
            }
            return new PatrolAI(worldPoints, mode);
        }
    }
}
