using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// A feature with an ID that's unique across the world. Saves, cutscenes, and feature edits find
    /// the feature by it. The bake copies the ID onto the record that <see cref="BakeRecord"/> returns.
    /// </summary>
    public abstract class IdentifiedFeatureBehaviour : WorldFeatureBehaviour {
        [Tooltip("Unique across the world, ignoring case. Letters, digits, '_' and '-'.")]
        [SerializeField] string id;

        /// <summary>Empty if no ID is set.</summary>
        public string featureId => id ?? "";

        public sealed override void Bake(WorldFeatureBaker baker) {
            var record = BakeRecord(baker);
            if (record == null) {
                return;
            }
            record.SetId(featureId);
            baker.Add(record);
        }

        /// <summary>Returns the feature's record, or null after logging an error with the baker.</summary>
        protected abstract IdentifiedFeatureData BakeRecord(WorldFeatureBaker baker);
    }
}
