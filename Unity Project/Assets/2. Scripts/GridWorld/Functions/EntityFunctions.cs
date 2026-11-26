using LPE;
using UnityEngine;

namespace GridWorld {
    public static class EntityFunctions {
        public static EntityID GetPlayer<T>(this T ctx)
                    where T : IWorldFunctionContext, IPerm_ReadEntities {
            return ctx.data.player;
        }

        /// <summary>False once the entity has been despawned.</summary>
        public static bool EntityExists<T>(this T ctx, EntityID id)
                    where T : IWorldFunctionContext, IPerm_ReadEntities {
            return ctx.data.entityIds.Exists(id);
        }

        public static Vector2Int GetTile<T>(this T ctx, EntityID id)
                    where T : IWorldFunctionContext, IPerm_ReadEntities {
            return ctx.data.components.tile.Get(id);
        }

        public static Direction4 GetFacing<T>(this T ctx, EntityID id)
                    where T : IWorldFunctionContext, IPerm_ReadEntities {
            return ctx.data.components.facing.Get(id);
        }

        public static bool HasStep<T>(this T ctx, EntityID id)
                    where T : IWorldFunctionContext, IPerm_ReadEntities {
            return ctx.data.components.step.Has(id);
        }

        public static StepState GetStepState<T>(this T ctx, EntityID id)
                    where T : IWorldFunctionContext, IPerm_ReadEntities {
            return ctx.data.components.step.Get(id).state;
        }

        /// <summary>True while a cutscene holds the entity. Decision systems leave it alone.</summary>
        public static bool IsLocked<T>(this T ctx, EntityID id)
                    where T : IWorldFunctionContext, IPerm_ReadEntities {
            return ctx.data.components.scripted.Has(id);
        }
    }
}
