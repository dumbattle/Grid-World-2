using UnityEditor;

namespace GridWorld {
    [CustomEditor(typeof(SpawnZoneBehaviour), true)]
    [CanEditMultipleObjects]
    public class SpawnZoneBehaviourEditor : Editor {
        public override void OnInspectorGUI() {
            RegionSpaceInspector.Draw(serializedObject, "regionSpace", "region", "Relative to zone");
        }
    }
}
