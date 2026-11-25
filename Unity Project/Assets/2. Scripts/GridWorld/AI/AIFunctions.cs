using System.Collections.Generic;
using LPE;

namespace GridWorld {
    public static class AIFunctions {
        /// <summary>Fills the list with every entity that has an <c>ai</c> component.</summary>
        public static void CollectAIEntities<T>(this T ctx, List<EntityID> entities)
                    where T : IWorldFunctionContext, IPerm_ReadEntities {
            foreach (var pair in ctx.data.components.ai) {
                entities.Add(pair.Key);
            }
        }

        public static EntityAI GetAI<T>(this T ctx, EntityID id)
                    where T : IWorldFunctionContext, IPerm_ReadEntities {
            return ctx.data.components.ai.Get(id);
        }
    }
}
