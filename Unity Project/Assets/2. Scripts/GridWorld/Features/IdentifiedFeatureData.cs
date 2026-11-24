using System;
using UnityEngine;

namespace GridWorld {
    /// <summary>The baked record of an <see cref="IdentifiedFeatureBehaviour"/>.</summary>
    [Serializable]
    public abstract class IdentifiedFeatureData : WorldFeatureData {
        [SerializeField] string featureId;

        /// <summary>Empty for a feature baked without an ID.</summary>
        public string id => featureId ?? "";

        internal void SetId(string id) {
            featureId = id;
        }
    }
}
