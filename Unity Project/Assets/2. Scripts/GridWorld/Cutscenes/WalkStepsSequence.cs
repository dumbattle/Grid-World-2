using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Walks a set number of tiles in one direction, through normal move requests. It requests the
    /// next step on the tick the last one finishes, so steps keep a constant rate, and it's done on
    /// the tick its last step finishes.
    /// </summary>
    public class WalkStepsSequence : ISequence {
        public const int TimeoutTicks = 120;

        readonly CutsceneContext ctx;
        readonly string cutsceneName;
        readonly EntityID id;
        readonly Direction4 direction;
        readonly int count;
        readonly bool run;

        bool started;
        bool done;
        Vector2Int destination;
        int blockedTicks;

        public WalkStepsSequence(CutsceneContext ctx, string cutsceneName, EntityID id, Direction4 direction, int count, bool run) {
            this.ctx = ctx;
            this.cutsceneName = cutsceneName;
            this.id = id;
            this.direction = direction;
            this.count = count;
            this.run = run;
        }

        public void Next() {
            if (done) {
                return;
            }
            if (!ctx.EntityExists(id)) {
                Debug.LogWarning($"Grid World: cutscene '{cutsceneName}' skipped the rest of a walk, because {id} was despawned.");
                done = true;
                return;
            }

            // A moving entity's tile is already its step's target, so this counts from there.
            if (!started) {
                started = true;
                destination = ctx.GetTile(id) + direction.ToVector2Int() * count;
            }

            if (ctx.GetStepState(id) == StepState.Moving) {
                blockedTicks = 0;
                return;
            }
            if (ctx.GetTile(id) == destination) {
                done = true;
                return;
            }

            if (blockedTicks >= TimeoutTicks) {
                TimeOut();
                done = true;
                return;
            }

            // If this doesn't start a step, the entity is still here next tick and counts as blocked.
            ctx.RequestMove(id, direction, run: run);
            blockedTicks++;
        }

        public bool Done() {
            return done;
        }

        public void OnComplete() { }

        void TimeOut() {
            var from = ctx.GetTile(id);
            if (ctx.Teleport(id, destination, out var landed)) {
                Debug.LogWarning($"Grid World: cutscene '{cutsceneName}' walk for {id} was blocked at {from} for {TimeoutTicks} ticks, so it teleported to {landed}.");
            }
            else {
                Debug.LogError($"Grid World: cutscene '{cutsceneName}' walk for {id} was blocked at {from} for {TimeoutTicks} ticks, and there's no free tile near {destination} to teleport to.");
            }
        }
    }
}
