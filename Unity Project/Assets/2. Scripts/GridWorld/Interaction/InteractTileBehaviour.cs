using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Plays a cutscene when the player faces one of its tiles and presses interact, such as a sign,
    /// a hidden item, or a shop counter. An entity standing on the tile takes priority.
    /// </summary>
    public class InteractTileBehaviour : IdentifiedFeatureBehaviour {
        [Tooltip("Tiles covered, up and right from this object's tile.")]
        [SerializeField] Vector2Int size = Vector2Int.one;
        [Tooltip("Plays when the player faces one of these tiles and presses interact.")]
        [SerializeReference, CutscenePicker] ICutscene cutscene;

        protected override IdentifiedFeatureData BakeRecord(WorldFeatureBaker baker) {
            if (size.x < 1 || size.y < 1) {
                baker.LogError(this, $"Interact tile '{name}' has a size of {size}. Both sides need at least 1.");
                return null;
            }
            if (cutscene == null) {
                baker.LogError(this, $"Interact tile '{name}' has no cutscene.");
                return null;
            }
            return new InteractTileData(baker.ToLocalTile(transform.position), size, CutsceneCopy.Of(cutscene));
        }

        void OnDrawGizmos() {
            var corner = new Vector3(Mathf.Floor(transform.position.x), Mathf.Floor(transform.position.y));
            var extent = new Vector3(Mathf.Max(size.x, 1), Mathf.Max(size.y, 1));
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(corner + extent * 0.5f, extent - new Vector3(0.1f, 0.1f));
        }
    }
}
