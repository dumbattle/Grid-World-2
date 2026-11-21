using System;
using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Takes one random step after a random wait, only onto free tiles it can enter inside its region.
    /// If none is free, it waits again.
    /// </summary>
    public class WanderAI : EntityAI {
        RectInt region;
        int minWaitTicks;
        int maxWaitTicks;
        // -1 until the first decision, so entities placed together don't all move on the same tick.
        int waitLeft = -1;
        Direction4[] choices = new Direction4[4];

        /// <param name="region">In world tiles.</param>
        public WanderAI(RectInt region, int minWaitTicks, int maxWaitTicks) {
            if (minWaitTicks < 0 || maxWaitTicks < minWaitTicks) {
                throw new ArgumentException($"Wander waits need 0 <= min <= max, not {minWaitTicks} and {maxWaitTicks}.");
            }
            this.region = region;
            this.minWaitTicks = minWaitTicks;
            this.maxWaitTicks = maxWaitTicks;
        }

        public override void Decide(AIContext ctx, EntityID self) {
            if (waitLeft < 0) {
                waitLeft = ctx.RandomRange(minWaitTicks, maxWaitTicks + 1);
                return;
            }
            if (waitLeft > 0) {
                waitLeft--;
                return;
            }

            var tile = ctx.GetTile(self);
            int count = 0;
            foreach (var direction in AIDirections.all) {
                var target = tile + direction.ToVector2Int();
                if (region.Contains(target) && ctx.CanEnter(target, direction) && !ctx.GetEntityAt(target).IsValid()) {
                    choices[count++] = direction;
                }
            }

            if (count > 0) {
                ctx.RequestMove(self, choices[ctx.RandomRange(0, count)]);
            }
            waitLeft = ctx.RandomRange(minWaitTicks, maxWaitTicks + 1);
        }
    }
}
