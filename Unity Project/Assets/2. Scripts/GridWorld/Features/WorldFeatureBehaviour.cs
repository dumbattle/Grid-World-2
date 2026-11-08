using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// An authored feature in an area scene. The bake calls <see cref="Bake"/> to export its records,
    /// and it stays in the scene as an inert authoring object.
    /// </summary>
    public abstract class WorldFeatureBehaviour : MonoBehaviour {
        public abstract void Bake(WorldFeatureBaker baker);
    }
}
