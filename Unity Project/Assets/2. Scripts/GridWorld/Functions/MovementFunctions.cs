using System;
using System.Collections.Generic;
using LPE;
using UnityEngine;

namespace GridWorld {
    public static class MovementFunctions {
        public static MoveRequests GetMoveRequests<T>(this T ctx)
                    where T : IWorldFunctionContext, IPerm_ResolveMovement {
            return new MoveRequests(ctx.data.id2moveRequest);
        }

        public static void ClearMoveRequests<T>(this T ctx)
                    where T : IWorldFunctionContext, IPerm_ResolveMovement {
            ctx.data.id2moveRequest.Clear();
        }

        /// <summary>Fills the lists with every entity whose step is moving or finished.</summary>
        public static void CollectSteps<T>(this T ctx, List<EntityID> moving, List<EntityID> finished)
                    where T : IWorldFunctionContext, IPerm_ResolveMovement {
            foreach (var pair in ctx.data.components.step) {
                var state = pair.Value.state;

                if (state == StepState.Moving) {
                    moving.Add(pair.Key);
                }
                else if (state == StepState.Finished) {
                    finished.Add(pair.Key);
                }
            }
        }

        public static void SetFacing<T>(this T ctx, EntityID id, Direction4 facing)
                    where T : IWorldFunctionContext, IPerm_ResolveMovement {
            if (facing == null) {
                throw new ArgumentNullException(nameof(facing));
            }
            ctx.data.components.facing.Set(id, facing);
        }

        /// <summary>
        /// Moves the entity's tile and occupancy to <paramref name="to"/> now, and starts a step that
        /// has already advanced once, because a step advances on the tick it starts.
        /// </summary>
        public static void BeginStep<T>(this T ctx, EntityID id, Vector2Int to, int duration)
                    where T : IWorldFunctionContext, IPerm_ResolveMovement {
            if (duration < 1) {
                throw new ArgumentOutOfRangeException(nameof(duration), $"A step for {id} needs at least 1 tick.");
            }

            var data = ctx.data;
            var from = data.components.tile.Get(id);

            data.occupancy.Release(from, id);
            data.occupancy.Claim(to, id);
            data.components.tile.Set(id, to);
            data.components.step.Set(id, new Step(from, to, duration, 1));
        }

        public static void AdvanceStep<T>(this T ctx, EntityID id)
                    where T : IWorldFunctionContext, IPerm_ResolveMovement {
            var steps = ctx.data.components.step;
            steps.Set(id, steps.Get(id).Advanced());
        }

        public static void EndStep<T>(this T ctx, EntityID id)
                    where T : IWorldFunctionContext, IPerm_ResolveMovement {
            var components = ctx.data.components;
            components.step.Set(id, Step.Idle(components.tile.Get(id)));
        }
    }
}
