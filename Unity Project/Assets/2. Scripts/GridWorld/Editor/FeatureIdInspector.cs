using UnityEditor;
using UnityEngine;

namespace GridWorld {
    /// <summary>Draws a box above a feature's Inspector when its ID is missing or clashes.</summary>
    static class FeatureIdInspector {
        public static void DrawMessages(Object[] targets) {
            if (targets.Length != 1) {
                int count = 0;
                foreach (var target in targets) {
                    if (FeatureIdIndex.Current.HasProblem((IdentifiedFeatureBehaviour)target)) {
                        count++;
                    }
                }
                if (count > 0) {
                    DrawBox($"{count} of the selected features need an ID or a new one.", MessageType.Warning, null);
                }
                return;
            }

            var feature = (IdentifiedFeatureBehaviour)targets[0];
            if (feature.featureId.Length == 0) {
                DrawBox("No ID. Saves, cutscenes, and feature edits find features by ID.", MessageType.Warning, feature);
                return;
            }
            var problem = FeatureIdIndex.Current.FindProblem(feature.featureId, feature);
            if (problem != null) {
                DrawBox(problem, MessageType.Error, feature);
            }
        }

        static void DrawBox(string message, MessageType type, IdentifiedFeatureBehaviour feature) {
            EditorGUILayout.HelpBox(message, type);
            if (GUILayout.Button("Fix Feature IDs…")) {
                FeatureIdWindow.Open(feature);
            }
            EditorGUILayout.Space();
        }
    }
}
