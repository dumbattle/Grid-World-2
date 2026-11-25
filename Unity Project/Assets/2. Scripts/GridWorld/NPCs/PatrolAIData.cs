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

        protected override EntityAI CreateAI(AreaDataSO area) {
            var worldPoints = new PatrolPoint[points.Length];
            for (int i = 0; i < points.Length; i++) {
                worldPoints[i] = points[i].ToWorld(area);
            }
            return new PatrolAI(worldPoints, mode);
        }
    }
}
