namespace GridWorld {
    /// <summary>
    /// Runs one tick: Spawn (spawn zones), Cutscene, Decision (player input and AI), then Movement.
    /// Deliberately hard-coded. A game that needs different phases writes its own manager and calls
    /// these systems alongside its own.
    /// </summary>
    public class PhaseExecutionManager {
        SpawnSystem spawn = new SpawnSystem();
        CutsceneSystem cutscenes = new CutsceneSystem();
        PlayerInputSystem playerInput = new PlayerInputSystem();
        AISystem ai = new AISystem();
        MovementSystem movement = new MovementSystem();

        public void Tick(WorldInstance world) {
            var spawnPhase = world.GetContext<SpawnContext>();
            var cutscenePhase = world.GetContext<CutsceneContext>();
            var decision = world.GetContext<DecisionPhaseContext>();
            var aiDecision = world.GetContext<AIContext>();
            var movementPhase = world.GetContext<MovementPhaseContext>();

            // Spawning first, so a new mob decides on the tick it appears, and a removed one is
            // forgotten before anything else runs.
            spawn.Run(spawnPhase);

            // Before the decision phase, so a cutscene's locks hold on the tick it starts, and a
            // finished cutscene's entities can decide on the tick it ends.
            cutscenes.Run(cutscenePhase);

            // Both decision systems write requests for different entities, so their order doesn't matter.
            playerInput.Run(decision);
            ai.Run(aiDecision);
            movement.Run(movementPhase);
        }
    }
}
