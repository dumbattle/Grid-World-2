using System;
using System.Collections.Generic;
using LPE;

namespace GridWorld {
    public static class CutsceneFunctions {
        /// <summary>
        /// Queues a cutscene to start in the next cutscene phase, which is this tick's if it hasn't
        /// run yet. It's dropped then if its ID isn't registered or one of its entities is locked.
        /// </summary>
        public static void StartCutscene<T>(this T ctx, string id, CutsceneStart start)
                    where T : IWorldFunctionContext, IPerm_StartCutscenes {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentException("A cutscene request needs an ID.", nameof(id));
            }
            ctx.data.cutsceneRequests.Add(new CutsceneRequest(id, start));
        }

        /// <summary>Moves every queued request into <paramref name="into"/>, in order, and clears the queue.</summary>
        public static void TakeCutsceneRequests<T>(this T ctx, List<CutsceneRequest> into)
                    where T : IWorldFunctionContext, IPerm_RunCutscenes {
            var requests = ctx.data.cutsceneRequests;
            into.AddRange(requests);
            requests.Clear();
        }

        public static bool TryGetCutscene<T>(this T ctx, string id, out Action<CutsceneBuilder, CutsceneStart> build)
                    where T : IWorldFunctionContext, IPerm_RunCutscenes {
            return ctx.data.cutscenes.TryGet(id, out build);
        }

        /// <summary>Locks the cutscene's entities and adds it to the running list. None may be locked already.</summary>
        public static void BeginCutscene<T>(this T ctx, RunningCutscene cutscene, ISequence root)
                    where T : IWorldFunctionContext, IPerm_RunCutscenes {
            if (cutscene.started) {
                throw new InvalidOperationException($"Cutscene '{cutscene.id}' has already started.");
            }
            if (root == null) {
                throw new ArgumentNullException(nameof(root));
            }

            var components = ctx.data.components;
            for (int i = 0; i < cutscene.entities.Count; i++) {
                if (components.scripted.Has(cutscene.entities[i])) {
                    throw new InvalidOperationException($"Cutscene '{cutscene.id}' can't lock {cutscene.entities[i]}: it's already locked.");
                }
            }
            for (int i = 0; i < cutscene.entities.Count; i++) {
                components.Register(components.scripted, cutscene.entities[i], new Marker());
            }

            cutscene.root = root;
            cutscene.started = true;
            ctx.data.runningCutscenes.Add(cutscene);
        }

        public static int GetRunningCutsceneCount<T>(this T ctx)
                    where T : IWorldFunctionContext, IPerm_RunCutscenes {
            return ctx.data.runningCutscenes.Count;
        }

        public static RunningCutscene GetRunningCutscene<T>(this T ctx, int index)
                    where T : IWorldFunctionContext, IPerm_RunCutscenes {
            return ctx.data.runningCutscenes[index];
        }

        /// <summary>
        /// Removes a running cutscene and unlocks its entities. Despawned entities have already lost
        /// their lock. The caller completes the sequence.
        /// </summary>
        public static void EndCutsceneAt<T>(this T ctx, int index)
                    where T : IWorldFunctionContext, IPerm_RunCutscenes {
            var data = ctx.data;
            var cutscene = data.runningCutscenes[index];
            var components = data.components;

            for (int i = 0; i < cutscene.entities.Count; i++) {
                components.Unregister(components.scripted, cutscene.entities[i]);
            }
            data.runningCutscenes.RemoveAt(index);
        }
    }
}
