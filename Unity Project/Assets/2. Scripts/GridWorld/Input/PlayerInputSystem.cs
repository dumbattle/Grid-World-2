namespace GridWorld {
    /// <summary>
    /// Turns held input into the player's move requests. From idle, pressing a new direction turns
    /// the player, and they walk once it's held for <see cref="TurnHoldTicks"/>. Facing that way
    /// already, or coming off a step, they walk at once.
    /// </summary>
    public class PlayerInputSystem {
        public const int TurnHoldTicks = 6;

        public void Run(DecisionPhaseContext ctx) {
            var player = ctx.GetPlayer();
            if (!player.IsValid() || !ctx.HasStep(player)) {
                return;
            }

            var control = ctx.GetPlayerControl();
            var direction = ctx.GetHeldDirection();
            bool run = ctx.IsRunHeld();

            if (direction == null) {
                control.Reset();
                return;
            }

            var state = ctx.GetStepState(player);

            // While walking, any held direction carries on with no delay.
            if (state != StepState.Idle) {
                if (control.direction != direction) {
                    control.direction = direction;
                    control.heldTicks = 0;
                }
                control.waitingToWalk = false;

                if (state == StepState.Finished) {
                    ctx.RequestMove(player, direction, run: run);
                }
                return;
            }

            if (control.direction != direction) {
                bool facing = ctx.GetFacing(player) == direction;

                control.direction = direction;
                control.heldTicks = 0;
                control.waitingToWalk = !facing;
                ctx.RequestMove(player, direction, turnOnly: !facing, run: run);
                return;
            }

            control.heldTicks++;
            if (control.waitingToWalk && control.heldTicks < TurnHoldTicks) {
                return;
            }

            control.waitingToWalk = false;
            ctx.RequestMove(player, direction, run: run);
        }
    }
}
