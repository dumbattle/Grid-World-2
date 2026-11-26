using System;
using System.Collections.Generic;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Runs in the cutscene phase. Starts queued cutscenes in order, dropping any whose entities are
    /// already locked, then advances every running cutscene once and releases the ones that finish.
    /// A cutscene that throws is stopped and released, so it can't break every tick after it.
    /// </summary>
    public class CutsceneSystem {
        List<CutsceneRequest> requests = new List<CutsceneRequest>();

        public void Run(CutsceneContext ctx) {
            requests.Clear();
            ctx.TakeCutsceneRequests(requests);
            for (int i = 0; i < requests.Count; i++) {
                Start(ctx, requests[i]);
            }
            requests.Clear();

            int index = 0;
            while (index < ctx.GetRunningCutsceneCount()) {
                if (Advance(ctx.GetRunningCutscene(index))) {
                    ctx.EndCutsceneAt(index);
                }
                else {
                    index++;
                }
            }
        }

        void Start(CutsceneContext ctx, CutsceneRequest request) {
            if (!ctx.TryGetCutscene(request.id, out var build)) {
                Debug.LogError($"Grid World: no cutscene is registered as '{request.id}'.");
                return;
            }

            var cutscene = new RunningCutscene(request.id);
            var builder = new CutsceneBuilder(ctx, cutscene);
            try {
                build(builder, request.start);
            }
            catch (Exception e) {
                Debug.LogError($"Grid World: cutscene '{request.id}' didn't start, because building it failed. {e}");
                return;
            }
            var root = builder.Build();

            for (int i = 0; i < cutscene.entities.Count; i++) {
                var id = cutscene.entities[i];
                string reason = null;
                if (!ctx.EntityExists(id)) {
                    reason = "doesn't exist";
                }
                else if (ctx.IsLocked(id)) {
                    reason = "is in another cutscene";
                }

                if (reason != null) {
                    Debug.Log($"Grid World: cutscene '{request.id}' didn't start, because {id} {reason}.");
                    root.OnComplete();
                    return;
                }
            }

            ctx.BeginCutscene(cutscene, root);
        }

        // True once the cutscene is over, whether it finished or failed.
        static bool Advance(RunningCutscene cutscene) {
            bool over;
            try {
                cutscene.root.Next();
                over = cutscene.root.Done();
            }
            catch (Exception e) {
                Debug.LogError($"Grid World: cutscene '{cutscene.id}' stopped after an error. {e}");
                over = true;
            }

            if (over) {
                try {
                    cutscene.root.OnComplete();
                }
                catch (Exception e) {
                    Debug.LogError($"Grid World: cutscene '{cutscene.id}' failed to clean up. {e}");
                }
            }
            return over;
        }
    }
}
