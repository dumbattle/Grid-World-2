using UnityEditor;

namespace GridWorld {
    [CustomEditor(typeof(IdentifiedFeatureBehaviour), true)]
    [CanEditMultipleObjects]
    public class IdentifiedFeatureBehaviourEditor : Editor {
        public override void OnInspectorGUI() {
            FeatureIdInspector.DrawMessages(targets);
            DrawDefaultInspector();
        }
    }
}
