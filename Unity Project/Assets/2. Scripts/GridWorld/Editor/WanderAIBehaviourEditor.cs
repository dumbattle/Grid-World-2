using UnityEditor;

namespace GridWorld {
    [CustomEditor(typeof(WanderAIBehaviour), true)]
    [CanEditMultipleObjects]
    public class WanderAIBehaviourEditor : Editor {
        public override void OnInspectorGUI() {
            RegionSpaceInspector.Draw(serializedObject, "regionSpace", "region", "Relative to NPC");
        }
    }
}
