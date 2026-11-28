using System;
using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// What plays when the player interacts with a tile. The adds return false instead of throwing
    /// when something is already there, since the contexts that add can't look first.
    /// </summary>
    public static class InteractionFunctions {
        /// <summary>
        /// Adds the cutscene to every tile in <paramref name="worldTiles"/>, or to none: if one
        /// already has an interaction, returns false with it in <paramref name="clash"/>.
        /// </summary>
        public static bool TryAddTileInteractions<T>(this T ctx, RectInt worldTiles, ICutscene cutscene, out Vector2Int clash)
                    where T : IWorldFunctionContext, IPerm_SetupWorld {
            if (cutscene == null) {
                throw new ArgumentNullException(nameof(cutscene));
            }
            if (worldTiles.width < 1 || worldTiles.height < 1) {
                throw new ArgumentException($"Tile interactions need at least one tile, not {worldTiles.size}.", nameof(worldTiles));
            }
            return ctx.data.interactions.TryAddTiles(worldTiles, cutscene, out clash);
        }

        /// <summary>The interaction follows the entity. Returns false if it already has one.</summary>
        public static bool TryAddEntityInteraction<T>(this T ctx, EntityID id, ICutscene cutscene)
                    where T : IWorldFunctionContext, IPerm_SpawnEntities {
            if (cutscene == null) {
                throw new ArgumentNullException(nameof(cutscene));
            }
            var data = ctx.data;
            if (!data.entityIds.Exists(id)) {
                throw new InvalidOperationException($"Can't give {id} an interaction: it doesn't exist.");
            }
            var entities = data.interactions.entities;
            if (entities.Has(id)) {
                return false;
            }
            data.components.Register(entities, id, cutscene);
            return true;
        }

        /// <summary>
        /// The interaction on a tile: the entity standing there first, then the tile's own. A moving
        /// entity stands on the tile it's stepping into.
        /// </summary>
        public static bool TryGetInteractionAt<T>(this T ctx, Vector2Int tile, out ICutscene cutscene)
                    where T : IWorldFunctionContext, IPerm_ReadInteractions {
            var data = ctx.data;
            var entity = data.occupancy.Get(tile);
            if (entity.IsValid() && data.interactions.entities.TryGet(entity, out cutscene)) {
                return true;
            }
            return data.interactions.TryGetTile(tile, out cutscene);
        }
    }
}
