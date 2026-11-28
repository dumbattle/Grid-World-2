using System.Collections.Generic;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Every interaction in the world. A tile's interaction stays on its tile. An entity's is a
    /// component, found through occupancy wherever the entity stands, so nothing moves it.
    /// </summary>
    public class WorldInteractions {
        Dictionary<Vector2Int, ICutscene> tile2interaction = new Dictionary<Vector2Int, ICutscene>();

        // Kept here rather than with the other components, but listed in the cleanup registry, so
        // a despawn still clears it.
        public readonly EntityComponent<ICutscene> entities = new EntityComponent<ICutscene>();

        public WorldInteractions(EntityComponentManager components) {
            components.AddStore(entities);
        }

        public int tileCount => tile2interaction.Count;

        /// <summary>
        /// Adds the cutscene to every tile, or to none: if a tile already has an interaction, returns
        /// false with the first one found in <paramref name="clash"/>.
        /// </summary>
        public bool TryAddTiles(RectInt tiles, ICutscene cutscene, out Vector2Int clash) {
            for (int y = tiles.yMin; y < tiles.yMax; y++) {
                for (int x = tiles.xMin; x < tiles.xMax; x++) {
                    var tile = new Vector2Int(x, y);
                    if (tile2interaction.ContainsKey(tile)) {
                        clash = tile;
                        return false;
                    }
                }
            }

            for (int y = tiles.yMin; y < tiles.yMax; y++) {
                for (int x = tiles.xMin; x < tiles.xMax; x++) {
                    tile2interaction.Add(new Vector2Int(x, y), cutscene);
                }
            }
            clash = default;
            return true;
        }

        public bool TryGetTile(Vector2Int tile, out ICutscene cutscene) {
            return tile2interaction.TryGetValue(tile, out cutscene);
        }
    }
}
