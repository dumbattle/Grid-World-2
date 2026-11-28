namespace GridWorld {
    /// <summary>
    /// Runs in the cutscene phase, before <see cref="CutsceneSystem"/>. For each interact attempt
    /// from last tick, starts the interaction on the faced tile, if there is one.
    /// <see cref="CutsceneSystem"/> drops it if one of its entities is locked.
    /// </summary>
    public class InteractionSystem {
        public void Run(InteractionContext ctx) {
            var attempts = ctx.GetInteractAttempts();
            for (int i = 0; i < attempts.Count; i++) {
                if (ctx.TryGetInteractionAt(attempts[i].tile, out var cutscene)) {
                    ctx.StartCutscene(cutscene);
                }
            }
        }
    }
}
