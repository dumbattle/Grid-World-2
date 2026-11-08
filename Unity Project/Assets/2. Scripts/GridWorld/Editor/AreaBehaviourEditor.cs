using LPE;
using UnityEditor;
using UnityEngine;

namespace GridWorld {
    [CustomEditor(typeof(AreaBehaviour))]
    public class AreaBehaviourEditor : Editor {
        public override void OnInspectorGUI() {
            DrawDefaultInspector();

            var area = (AreaBehaviour)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("New Neighbour", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(area.world == null || targets.Length > 1)) {
                using (new EditorGUILayout.HorizontalScope()) {
                    NeighbourButton(area, "Left", Direction4.left);
                    NeighbourButton(area, "Right", Direction4.right);
                    NeighbourButton(area, "Up", Direction4.up);
                    NeighbourButton(area, "Down", Direction4.down);
                }
            }
            if (area.world == null) {
                EditorGUILayout.HelpBox("Set the world data to add neighbours.", MessageType.None);
            }
        }

        static void NeighbourButton(AreaBehaviour area, string label, Direction4 direction) {
            if (GUILayout.Button(label)) {
                NewAreaWindow.OpenNeighbour(area, direction);
            }
        }
    }
}
