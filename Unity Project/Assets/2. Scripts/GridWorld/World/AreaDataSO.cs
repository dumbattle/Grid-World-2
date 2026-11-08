using System.Collections.Generic;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// One area's baked data. Everything here is written by the bake, so don't edit it by hand.
    /// </summary>
    [CreateAssetMenu(fileName = "AreaData", menuName = "Grid World/Area Data")]
    public class AreaDataSO : ScriptableObject {
        [SerializeField] string id;
        [SerializeField] string sceneFilePath;
        [SerializeField] RectInt localTileBounds;
        [SerializeField] Vector2Int worldOffset;
        [SerializeField] ushort[] tiles = new ushort[0];
        [SerializeReference] List<WorldFeatureData> features = new List<WorldFeatureData>();

        public string areaId => id;
        public string scenePath => sceneFilePath;
        public RectInt localBounds => localTileBounds;
        public Vector2Int offset => worldOffset;
        public RectInt worldBounds => new RectInt(localTileBounds.position + worldOffset, localTileBounds.size);
        public IReadOnlyList<WorldFeatureData> featureRecords => features;

        public Vector2Int ToWorldTile(Vector2Int localTile) {
            return localTile + worldOffset;
        }

        public AreaTiles CreateTiles() {
            return new AreaTiles(worldBounds, tiles);
        }

        public void SetBakedData(string areaId, string scenePath, RectInt localBounds, Vector2Int offset, ushort[] tiles, List<WorldFeatureData> features) {
            id = areaId;
            sceneFilePath = scenePath;
            localTileBounds = localBounds;
            worldOffset = offset;
            this.tiles = tiles;
            this.features = features;
        }
    }
}
