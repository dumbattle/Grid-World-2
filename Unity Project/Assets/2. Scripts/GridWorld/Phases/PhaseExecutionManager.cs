namespace GridWorld {
    /// <summary>
    /// Runs one tick: Decision, then Movement. Deliberately hard-coded. A game that needs different
    /// phases writes its own manager and calls these systems alongside its own.
    /// </summary>
    public class PhaseExecutionManager {
        PlayerInputSystem playerInput = new PlayerInputSystem();
        MovementSystem movement = new MovementSystem();

        public void Tick(WorldInstance world) {
            var decision = world.GetContext<DecisionPhaseContext>();
            var movementPhase = world.GetContext<MovementPhaseContext>();

            playerInput.Run(decision);
            movement.Run(movementPhase);
        }
    }
}
