namespace GridWorld {
    /// <summary>
    /// Runs one tick: Decision (player input and AI), then Movement. Deliberately hard-coded. A game that needs different
    /// phases writes its own manager and calls these systems alongside its own.
    /// </summary>
    public class PhaseExecutionManager {
        PlayerInputSystem playerInput = new PlayerInputSystem();
        AISystem ai = new AISystem();
        MovementSystem movement = new MovementSystem();

        public void Tick(WorldInstance world) {
            var decision = world.GetContext<DecisionPhaseContext>();
            var aiDecision = world.GetContext<AIContext>();
            var movementPhase = world.GetContext<MovementPhaseContext>();

            // Both decision systems write requests for different entities, so their order doesn't matter.
            playerInput.Run(decision);
            ai.Run(aiDecision);
            movement.Run(movementPhase);
        }
    }
}
