using System;
using LPE;

namespace GridWorld {
    /// <summary>Stands on its tile and turns to a random new direction after a random wait.</summary>
    public class SpinAI : EntityAI {
        int minWaitTicks;
        int maxWaitTicks;
        // -1 until the first decision, so entities placed together don't all turn on the same tick.
        int waitLeft = -1;
        Direction4[] choices = new Direction4[3];

        public SpinAI(int minWaitTicks, int maxWaitTicks) {
            if (minWaitTicks < 0 || maxWaitTicks < minWaitTicks) {
                throw new ArgumentException($"Spin waits need 0 <= min <= max, not {minWaitTicks} and {maxWaitTicks}.");
            }
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

            var facing = ctx.GetFacing(self);
            int count = 0;
            foreach (var direction in AIDirections.all) {
                if (direction != facing) {
                    choices[count++] = direction;
                }
            }

            ctx.RequestMove(self, choices[ctx.RandomRange(0, count)], turnOnly: true);
            waitLeft = ctx.RandomRange(minWaitTicks, maxWaitTicks + 1);
        }
    }
}
