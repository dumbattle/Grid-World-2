using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Marks the root of an area scene. Its bounds are in the scene's tile coordinates, and the bake
    /// writes the area's tiles and features to <see cref="data"/>. Inert at runtime.
    /// </summary>
    [RequireComponent(typeof(Grid))]
    public class AreaBehaviour : MonoBehaviour {
        [SerializeField] string id;
        [SerializeField] AreaDataSO areaData;
        [SerializeField] WorldDataSO worldData;
        [SerializeField] RectInt tileBounds = new RectInt(0, 0, 16, 16);

        public string areaId => id;
        public AreaDataSO data => areaData;
        public WorldDataSO world => worldData;
        public RectInt bounds => tileBounds;
        public Grid grid => GetComponent<Grid>();

        void OnDrawGizmos() {
            Gizmos.color = Color.cyan;
            var min = transform.position + new Vector3(tileBounds.xMin, tileBounds.yMin);
            var size = new Vector3(tileBounds.width, tileBounds.height);
            Gizmos.DrawWireCube(min + size / 2, size);
        }
    }
}
