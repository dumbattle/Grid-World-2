using LPE;

namespace GridWorld {
    public static class DefinitionFunctions {
        public static EntityDefinitionSO GetDefinition<T>(this T ctx, EntityID id)
                    where T : IWorldFunctionContext, IPerm_ReadDefinitions {
            return ctx.data.components.definition.Get(id);
        }
    }
}
