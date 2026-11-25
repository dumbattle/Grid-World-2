using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GridWorld {
    /// <summary>
    /// What applying a feature edit file would do, from <see cref="FeatureEditApplier.Prepare"/> or
    /// <see cref="FeatureEditApplier.Evaluate"/>. The changes are staged but not made. Finish with
    /// <see cref="FeatureEditApplier.Commit"/> or <see cref="FeatureEditApplier.Cancel"/>, which
    /// closes the area's scene again if preparing opened it.
    /// </summary>
    public class FeatureEditPlan {
        public FeatureEditFile file;
        public AreaBehaviour area;
        public Scene scene;
        public bool openedScene;
        /// <summary>Problems with the file as a whole, such as an unknown area. Nothing can be applied.</summary>
        public List<string> errors = new List<string>();
        public List<FeatureEditResult> results = new List<FeatureEditResult>();

        // Staged changes. Serialized objects hold their new values unapplied until Commit.
        internal List<SerializedObject> serializedObjects = new List<SerializedObject>();
        internal List<KeyValuePair<Transform, Vector3>> moves = new List<KeyValuePair<Transform, Vector3>>();
        internal List<GameObject> removals = new List<GameObject>();
        internal bool committed;

        public bool hasConflicts => errors.Count > 0 || Count(FeatureEditStatus.Conflict) > 0;
        public bool hasChanges => !hasConflicts && Count(FeatureEditStatus.Change) > 0;

        public int Count(FeatureEditStatus status) {
            int count = 0;
            foreach (var result in results) {
                if (result.status == status) {
                    count++;
                }
            }
            return count;
        }
    }
}
