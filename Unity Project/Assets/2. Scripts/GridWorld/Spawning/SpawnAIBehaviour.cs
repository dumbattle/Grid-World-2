using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// The AI profile for the mobs of the <see cref="SpawnZoneBehaviour"/> on the same object. A zone
    /// has at most one. With none, its mobs stand still.
    /// </summary>
    [RequireComponent(typeof(SpawnZoneBehaviour))]
    public abstract class SpawnAIBehaviour : MonoBehaviour {
        [Tooltip("Mobs stop deciding while their area is inactive.")]
        [SerializeField] bool pauseWhenAreaInactive = true;

        protected bool pauses => pauseWhenAreaInactive;

        /// <summary>
        /// Returns this profile's record, or null after logging an error with
        /// <see cref="WorldFeatureBaker.LogError"/>.
        /// </summary>
        public abstract SpawnAIData Bake(WorldFeatureBaker baker);
    }
}
