using System;
using UnityEngine;

namespace GridWorld {
    [Serializable]
    public sealed class InteractTileData : IdentifiedFeatureData {
        [SerializeField] Vector2Int localTile;
        [SerializeField] Vector2Int tileCount;
        [SerializeReference] ICutscene interaction;

        /// <summary>Area-local tiles.</summary>
        public RectInt tiles => new RectInt(localTile, tileCount);
        public ICutscene cutscene => interaction;

        public InteractTileData(Vector2Int tile, Vector2Int size, ICutscene cutscene) {
            localTile = tile;
            tileCount = size;
            interaction = cutscene;
        }
    }
}
