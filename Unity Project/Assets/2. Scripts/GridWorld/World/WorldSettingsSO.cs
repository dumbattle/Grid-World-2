using UnityEngine;

namespace GridWorld {
    [CreateAssetMenu(fileName = "WorldSettings", menuName = "Grid World/World Settings")]
    public class WorldSettingsSO : ScriptableObject {
        // Both logic (active areas) and the camera need the view size, so it lives here.
        [SerializeField] Vector2Int viewSizeInTiles = new Vector2Int(15, 11);
        // The area the camera target is in, and every area touching it, are always active. The
        // margins are a backup for areas that don't touch, or a target outside every area.
        [Tooltip("Backup to touching areas: an area also becomes active once it's this many tiles from the edge of the view.")]
        [SerializeField, Min(0)] int activateMargin = 4;
        [Tooltip("An area that doesn't touch the target's area goes inactive once it's this many tiles from the edge of the view. Keep it larger than the activate margin.")]
        [SerializeField, Min(0)] int deactivateMargin = 8;

        public Vector2Int viewTiles => viewSizeInTiles;
        public int activateMarginTiles => activateMargin;
        public int deactivateMarginTiles => Mathf.Max(deactivateMargin, activateMargin);
    }
}
