using UnityEngine;
using UnityEngine.Tilemaps;

namespace GridWorld {
    /// <summary>
    /// Marks a tilemap whose tiles count toward the area's tile data. Where layers overlap, the one
    /// drawn on top wins, by sorting layer and then order in layer. Inert at runtime.
    /// </summary>
    [RequireComponent(typeof(Tilemap))]
    public class LogicLayerBehaviour : MonoBehaviour {
        public Tilemap tilemap => GetComponent<Tilemap>();

        public int sortingLayerValue {
            get {
                var tilemapRenderer = GetComponent<TilemapRenderer>();
                return tilemapRenderer == null ? 0 : SortingLayer.GetLayerValueFromID(tilemapRenderer.sortingLayerID);
            }
        }

        public int sortingOrder {
            get {
                var tilemapRenderer = GetComponent<TilemapRenderer>();
                return tilemapRenderer == null ? 0 : tilemapRenderer.sortingOrder;
            }
        }
    }
}
