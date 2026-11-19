using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GridWorld {
    /// <summary>Grid World → Build Area from Spec…: reads a spec and its legend, then builds the area.</summary>
    public static class MapSpecMenu {
        const int MaxDialogErrors = 8;

        [MenuItem("Grid World/Build Area from Spec…")]
        static void BuildFromMenu() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                EditorUtility.DisplayDialog("Build Area from Spec", "Leave Play mode first.", "OK");
                return;
            }

            var folder = Directory.Exists(MapPaths.areas) ? MapPaths.areas : "";
            var path = EditorUtility.OpenFilePanel("Build Area from Spec", folder, "txt");
            if (string.IsNullOrEmpty(path)) {
                return;
            }

            var errors = new List<string>();
            var spec = Read(path, errors, out var legend);
            if (errors.Count == 0) {
                // Unity can't add a scene next to an untitled one, so that case replaces the open scenes.
                bool additive = !NewAreaCreator.HasUntitledScene();
                if (!additive && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
                    return;
                }
                errors = MapSpecBuilder.FindErrors(spec, legend, additive);
                if (errors.Count == 0) {
                    var area = MapSpecBuilder.Build(spec, legend, additive);
                    if (area != null) {
                        Selection.activeGameObject = area.gameObject;
                        Debug.Log($"Built area '{spec.id}' from {Path.GetFileName(path)}.");
                    }
                    return;
                }
            }

            foreach (var error in errors) {
                Debug.LogError($"Can't build area from spec: {error}");
            }
            var shown = errors.Count > MaxDialogErrors ? errors.GetRange(0, MaxDialogErrors) : errors;
            var more = errors.Count > MaxDialogErrors ? $"\n\n…and {errors.Count - MaxDialogErrors} more in the Console." : "";
            EditorUtility.DisplayDialog("Build Area from Spec", string.Join("\n", shown) + more, "OK");
        }

        /// <summary>Parses a spec file and the legend it names, adding every problem to the list.</summary>
        public static MapSpec Read(string specPath, List<string> errors, out MapLegend legend) {
            legend = null;
            var spec = MapSpecParser.Parse(File.ReadAllText(specPath), Path.GetFileName(specPath), errors);
            if (spec == null) {
                return null;
            }

            var legendPath = MapPaths.Legend(spec.legend);
            if (!File.Exists(legendPath)) {
                errors.Add($"{spec.source}: legend '{spec.legend}' not found at {legendPath}.");
                return spec;
            }
            legend = MapSpecParser.ParseLegend(File.ReadAllText(legendPath), Path.GetFileName(legendPath), errors);
            if (legend != null) {
                MapSpecParser.CheckAgainstLegend(spec, legend, errors);
            }
            return spec;
        }
    }
}
