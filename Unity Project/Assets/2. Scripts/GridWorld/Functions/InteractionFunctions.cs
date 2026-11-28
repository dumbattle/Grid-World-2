using System;
using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// What plays when the player interacts with a tile. An entity's interaction is wherever the
    /// entity is, found through occupancy, so nothing moves it. A tile's interaction stays on its tile.
    /// </summary>
    public static class InteractionFunctions {
        public static void AddTileInteraction<T>(this T ctx, Vector2Int tile, ICutscene cutscene)
                    where T : IWorldFunctionContext, IPerm_EditInteractions {
            if (cutscene == null) {
                throw new ArgumentNullException(nameof(cutscene));
            }
            var tile2interaction = ctx.data.tile2interaction;
            if (tile2interaction.ContainsKey(tile)) {
                throw new InvalidOperationException($"Tile {tile} already has an interaction. A tile can have only one.");
            }
            tile2interaction.Add(tile, cutscene);
        }

        /// <summary>Does nothing if the tile has no interaction.</summary>
        public static void RemoveTileInteraction<T>(this T ctx, Vector2Int tile)
                    where T : IWorldFunctionContext, IPerm_EditInteractions {
            ctx.data.tile2interaction.Remove(tile);
        }

        public static void AddEntityInteraction<T>(this T ctx, EntityID id, ICutscene cutscene)
                    where T : IWorldFunctionContext, IPerm_EditInteractions {
            if (cutscene == null) {
                throw new ArgumentNullException(nameof(cutscene));
            }
            var data = ctx.data;
            if (!data.entityIds.Exists(id)) {
                throw new InvalidOperationException($"Can't give {id} an interaction: it doesn't exist.");
            }
            var components = data.components;
            if (components.interaction.Has(id)) {
                throw new InvalidOperationException($"{id} already has an interaction. An entity can have only one.");
            }
            components.Register(components.interaction, id, cutscene);
        }

        /// <summary>Does nothing if the entity has no interaction.</summary>
        public static void RemoveEntityInteraction<T>(this T ctx, EntityID id)
                    where T : IWorldFunctionContext, IPerm_EditInteractions {
            var components = ctx.data.components;
            components.Unregister(components.interaction, id);
        }

        /// <summary>
        /// The interaction on a tile: the entity standing there first, then the tile's own. A moving
        /// entity stands on the tile it's stepping into.
        /// </summary>
        public static bool TryGetInteractionAt<T>(this T ctx, Vector2Int tile, out ICutscene cutscene)
                    where T : IWorldFunctionContext, IPerm_ReadInteractions {
            var data = ctx.data;
            var entity = data.occupancy.Get(tile);
            if (entity.IsValid() && data.components.interaction.TryGet(entity, out cutscene)) {
                return true;
            }
            return data.tile2interaction.TryGetValue(tile, out cutscene);
        }

        /// <summary>Only the tile's own interaction, ignoring any entity on it.</summary>
        public static bool HasTileInteraction<T>(this T ctx, Vector2Int tile)
                    where T : IWorldFunctionContext, IPerm_ReadInteractions {
            return ctx.data.tile2interaction.ContainsKey(tile);
        }
    }
}
