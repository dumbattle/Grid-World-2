using System.Collections.Generic;
using LPE;
using UnityEngine;

namespace GridWorld {
    public static class RenderFunctions {
        /// <summary>Fills the list with every entity.</summary>
        public static void CollectEntities<T>(this T ctx, List<EntityID> entities)
                    where T : IWorldFunctionContext, IPerm_ReadEntities {
            foreach (var pair in ctx.data.components.tile) {
                entities.Add(pair.Key);
            }
        }

        /// <summary>
        /// The entity's position in tiles, blended between the last two ticks. The tile's lower-left
        /// corner is at its coordinate, so add half a tile for its centre.
        /// </summary>
        public static Vector2 GetBlendedPosition<T>(this T ctx, EntityID id, float alpha)
                    where T : IWorldFunctionContext, IPerm_ReadEntities {
            var components = ctx.data.components;
            if (components.step.TryGet(id, out var step)) {
                return step.BlendedPosition(alpha);
            }
            return components.tile.Get(id);
        }
    }
}
