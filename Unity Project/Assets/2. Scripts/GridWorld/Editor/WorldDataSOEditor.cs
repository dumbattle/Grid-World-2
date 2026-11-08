using UnityEditor;
using UnityEngine;

namespace GridWorld {
    [CustomEditor(typeof(WorldDataSO))]
    public class WorldDataSOEditor : Editor {
        public override void OnInspectorGUI() {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("New Area…")) {
                NewAreaWindow.Open((WorldDataSO)target);
            }
        }
    }
}
