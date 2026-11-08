using System;
using System.Collections.Generic;
using UnityEngine;

namespace GridWorld.Tests {
    /// <summary>
    /// Creates temporary assets for a test and destroys them all on dispose.
    /// </summary>
    class TestAssets : IDisposable {
        List<UnityEngine.Object> created = new List<UnityEngine.Object>();

        public T Create<T>() where T : ScriptableObject {
            var asset = ScriptableObject.CreateInstance<T>();
            created.Add(asset);
            return asset;
        }

        public AreaDataSO Area(string id, RectInt localBounds, Vector2Int offset, ushort fill = TileBits.EnterAll, params WorldFeatureData[] features) {
            var tiles = new ushort[localBounds.width * localBounds.height];
            for (int i = 0; i < tiles.Length; i++) {
                tiles[i] = fill;
            }

            var area = Create<AreaDataSO>();
            area.name = id;
            area.SetBakedData(id, $"Assets/{id}.unity", localBounds, offset, tiles, new List<WorldFeatureData>(features));
            return area;
        }

        public void Dispose() {
            foreach (var asset in created) {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }
    }
}
