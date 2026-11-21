using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Adds behaviour to the <see cref="NpcBehaviour"/> on the same object. The NPC's bake calls
    /// <see cref="Bake"/> and stores the result in its record.
    /// </summary>
    [RequireComponent(typeof(NpcBehaviour))]
    public abstract class NpcAddonBehaviour : MonoBehaviour {
        /// <summary>
        /// Returns this add-on's record, or null after logging an error with
        /// <see cref="WorldFeatureBaker.LogError"/>.
        /// </summary>
        public abstract NpcAddonData Bake(WorldFeatureBaker baker, Vector2Int localTile);
    }
}
