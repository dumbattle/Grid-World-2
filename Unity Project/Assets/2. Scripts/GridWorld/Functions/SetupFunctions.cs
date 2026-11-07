using System;
using LPE;
using UnityEngine;

namespace GridWorld {
    public static class SetupFunctions {
        public static void AddArea<T>(this T ctx, AreaTiles area)
                    where T : IWorldFunctionContext, IPerm_SetupWorld {
            if (area == null) {
                throw new ArgumentNullException(nameof(area));
            }
            ctx.data.tiles.AddArea(area);
        }

        /// <summary>Creates an idle entity that occupies <paramref name="tile"/>.</summary>
        public static EntityID SpawnEntity<T>(this T ctx, EntityDefinitionSO definition, Vector2Int tile, Direction4 facing)
                    where T : IWorldFunctionContext, IPerm_SetupWorld {
            if (definition == null) {
                throw new ArgumentNullException(nameof(definition));
            }
            if (facing == null) {
                throw new ArgumentNullException(nameof(facing));
            }

            var data = ctx.data;
            var existing = data.occupancy.Get(tile);
            if (existing.IsValid()) {
                throw new InvalidOperationException($"Can't spawn {definition.name} at {tile}: {existing} is already there.");
            }

            var id = data.entityIds.Create();
            var components = data.components;

            components.Register(components.tile, id, tile);
            components.Register(components.facing, id, facing);
            components.Register(components.definition, id, definition);
            components.Register(components.step, id, Step.Idle(tile));
            data.occupancy.Claim(tile, id);
            return id;
        }

        public static void SetPlayer<T>(this T ctx, EntityID id)
                    where T : IWorldFunctionContext, IPerm_SetupWorld {
            ctx.data.player = id;
        }
    }
}
