using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Turns an entity with a turn-only move request, once it isn't partway through a step. Done on
    /// the tick it makes the request.
    /// </summary>
    public class FaceSequence : ISequence {
        readonly CutsceneContext ctx;
        readonly string cutsceneName;
        readonly EntityID id;
        readonly Direction4 direction;
        bool done;

        public FaceSequence(CutsceneContext ctx, string cutsceneName, EntityID id, Direction4 direction) {
            this.ctx = ctx;
            this.cutsceneName = cutsceneName;
            this.id = id;
            this.direction = direction;
        }

        public void Next() {
            if (done) {
                return;
            }
            if (!ctx.EntityExists(id)) {
                Debug.LogWarning($"Grid World: cutscene '{cutsceneName}' skipped a turn, because {id} was despawned.");
                done = true;
                return;
            }
            if (ctx.GetStepState(id) == StepState.Moving) {
                return;
            }
            ctx.RequestMove(id, direction, turnOnly: true);
            done = true;
        }

        public bool Done() {
            return done;
        }

        public void OnComplete() { }
    }
}
