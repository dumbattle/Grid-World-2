namespace GridWorld {
    /// <summary>
    /// Turns held input into the player's move requests. From idle, pressing a new direction turns
    /// the player, and they walk once it's held for <see cref="TurnHoldTicks"/>. Facing that way
    /// already, or coming off a step, they walk at once. An idle player with a buffered interact
    /// press records an interact attempt instead of moving that tick.
    /// </summary>
    public class PlayerInputSystem {
        public const int TurnHoldTicks = 6;

        public void Run(DecisionPhaseContext ctx) {
            // Last tick's attempts were read in this tick's cutscene phase.
            ctx.ClearInteractAttempts();
            Decide(ctx);
            // Every tick, whatever Decide did, so a press lasts exactly the buffer.
            ctx.AgeButtonPresses();
        }

        void Decide(DecisionPhaseContext ctx) {
            var player = ctx.GetPlayer();
            if (!player.IsValid() || !ctx.HasStep(player)) {
                return;
            }

            var control = ctx.GetPlayerControl();

            // A cutscene moves the player. Once it ends, a held direction counts as a fresh press,
            // and a press made during it is gone, so it can't start another cutscene straight away.
            if (ctx.IsLocked(player)) {
                control.Reset();
                ctx.ClearInteractPress();
                return;
            }

            var state = ctx.GetStepState(player);

            // Before directions, so it works with one held. A walking player never interacts: a
            // press near the end of a step is still buffered once they stop.
            if (state == StepState.Idle && ctx.IsInteractBuffered()) {
                ctx.ClearInteractPress();
                var tile = ctx.GetTile(player) + ctx.GetFacing(player).ToVector2Int();
                ctx.RecordInteractAttempt(new InteractAttempt(player, tile, ctx.GetEntityAt(tile)));
                return;
            }

            var direction = ctx.GetHeldDirection();
            bool run = ctx.IsRunHeld();

            if (direction == null) {
                control.Reset();
                return;
            }

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
