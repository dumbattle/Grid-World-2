using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>Teleports an entity at once. See <see cref="TeleportFunctions.Teleport{T}"/>.</summary>
    public class TeleportSequence : ISequence {
        readonly CutsceneContext ctx;
        readonly string cutsceneId;
        readonly EntityID id;
        readonly Vector2Int tile;
        bool done;

        public TeleportSequence(CutsceneContext ctx, string cutsceneId, EntityID id, Vector2Int tile) {
            this.ctx = ctx;
            this.cutsceneId = cutsceneId;
            this.id = id;
            this.tile = tile;
        }

        public void Next() {
            if (done) {
                return;
            }
            done = true;

            if (!ctx.EntityExists(id)) {
                Debug.LogWarning($"Grid World: cutscene '{cutsceneId}' skipped a teleport, because {id} was despawned.");
                return;
            }
            if (!ctx.Teleport(id, tile, out _)) {
                Debug.LogError($"Grid World: cutscene '{cutsceneId}' couldn't teleport {id}: there's no free tile near {tile}.");
            }
        }

        public bool Done() {
            return done;
        }

        public void OnComplete() { }
    }
}
