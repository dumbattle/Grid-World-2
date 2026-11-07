using System.Collections.Generic;
using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Resolves move requests in one staged pass, then advances steps already under way. The buffers
    /// are scratch space, cleared every tick, so the system holds no game state.
    /// </summary>
    public class MovementSystem {
        struct StagedMove {
            public EntityID id;
            public Vector2Int tile;
            public bool isPlayer;
            public MoveRequest request;
        }

        // Player first, then lowest y, then lowest x. Two entities never share a tile, so the order is total.
        class TieBreakOrder : IComparer<StagedMove> {
            public int Compare(StagedMove a, StagedMove b) {
                if (a.isPlayer != b.isPlayer) {
                    return a.isPlayer ? -1 : 1;
                }
                if (a.tile.y != b.tile.y) {
                    return a.tile.y.CompareTo(b.tile.y);
                }
                return a.tile.x.CompareTo(b.tile.x);
            }
        }

        static readonly TieBreakOrder tieBreakOrder = new TieBreakOrder();

        List<EntityID> moving = new List<EntityID>();
        List<EntityID> finished = new List<EntityID>();
        List<StagedMove> staged = new List<StagedMove>();
        HashSet<EntityID> started = new HashSet<EntityID>();

        public void Run(MovementPhaseContext ctx) {
            moving.Clear();
            finished.Clear();
            staged.Clear();
            started.Clear();

            // Captured before anything changes, so a step started this tick isn't advanced twice.
            ctx.CollectSteps(moving, finished);

            StageRequests(ctx);
            staged.Sort(tieBreakOrder);
            for (int i = 0; i < staged.Count; i++) {
                Resolve(ctx, staged[i]);
            }
            ctx.ClearMoveRequests();

            for (int i = 0; i < moving.Count; i++) {
                ctx.AdvanceStep(moving[i]);
            }

            // A step that finished last tick clears to idle only if no new step replaced it.
            for (int i = 0; i < finished.Count; i++) {
                if (!started.Contains(finished[i])) {
                    ctx.EndStep(finished[i]);
                }
            }
        }

        void StageRequests(MovementPhaseContext ctx) {
            var player = ctx.GetPlayer();

            foreach (var pair in ctx.GetMoveRequests()) {
                var id = pair.Key;

                // Only idle entities and those that just finished a step can move.
                if (!ctx.HasStep(id) || ctx.GetStepState(id) == StepState.Moving) {
                    continue;
                }

                staged.Add(new StagedMove {
                    id = id,
                    tile = ctx.GetTile(id),
                    isPlayer = id == player,
                    request = pair.Value,
                });
            }
        }

        void Resolve(MovementPhaseContext ctx, StagedMove move) {
            var direction = move.request.direction;

            // Facing first, so a blocked step still turns the entity.
            ctx.SetFacing(move.id, direction);
            if (move.request.turnOnly) {
                return;
            }

            var target = move.tile + direction.ToVector2Int();
            if (!ctx.CanEnter(target, direction) || ctx.GetEntityAt(target).IsValid()) {
                return;
            }

            var definition = ctx.GetDefinition(move.id);
            int duration = move.request.run ? definition.runTicks : definition.walkTicks;

            ctx.BeginStep(move.id, target, duration);
            started.Add(move.id);
        }
    }
}
