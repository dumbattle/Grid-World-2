using System.Collections.Generic;

namespace GridWorld {
    /// <summary>What applying one edit would do, from <see cref="FeatureEditApplier.Evaluate"/>.</summary>
    public class FeatureEditResult {
        public FeatureEdit edit;
        public FeatureEditStatus status;
        /// <summary>Why it's already applied or conflicts. Null for a change.</summary>
        public string message;
        /// <summary>Things to check after a change, such as a wander region that stays behind.</summary>
        public List<string> warnings = new List<string>();
    }
}
