using UnityEngine;

namespace GridWorld {
    [CreateAssetMenu(fileName = "WorldSettings", menuName = "Grid World/World Settings")]
    public class WorldSettingsSO : ScriptableObject {
        // Both logic (active areas) and the camera need the view size, so it lives here.
        [SerializeField] Vector2Int viewSizeInTiles = new Vector2Int(15, 11);

        public Vector2Int viewTiles => viewSizeInTiles;
    }
}
