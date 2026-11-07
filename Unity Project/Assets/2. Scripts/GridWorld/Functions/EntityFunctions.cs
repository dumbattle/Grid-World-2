using LPE;
using UnityEngine;

namespace GridWorld {
    public static class EntityFunctions {
        public static EntityID GetPlayer<T>(this T ctx)
                    where T : IWorldFunctionContext, IPerm_ReadEntities {
            return ctx.data.player;
        }

        public static Vector2Int GetTile<T>(this T ctx, EntityID id)
                    where T : IWorldFunctionContext, IPerm_ReadEntities {
            return ctx.data.components.tile.Get(id);
        }

        public static Direction4 GetFacing<T>(this T ctx, EntityID id)
                    where T : IWorldFunctionContext, IPerm_ReadEntities {
            return ctx.data.components.facing.Get(id);
        }

        public static EntityDefinitionSO GetDefinition<T>(this T ctx, EntityID id)
                    where T : IWorldFunctionContext, IPerm_ReadEntities {
            return ctx.data.components.definition.Get(id);
        }

        public static bool HasStep<T>(this T ctx, EntityID id)
                    where T : IWorldFunctionContext, IPerm_ReadEntities {
            return ctx.data.components.step.Has(id);
        }

        public static StepState GetStepState<T>(this T ctx, EntityID id)
                    where T : IWorldFunctionContext, IPerm_ReadEntities {
            return ctx.data.components.step.Get(id).state;
        }

        /// <summary>The entity on a tile, or <see cref="EntityID.INVALID"/> if it's free.</summary>
        public static EntityID GetEntityAt<T>(this T ctx, Vector2Int tile)
                    where T : IWorldFunctionContext, IPerm_ReadEntities {
            return ctx.data.occupancy.Get(tile);
        }
    }
}
