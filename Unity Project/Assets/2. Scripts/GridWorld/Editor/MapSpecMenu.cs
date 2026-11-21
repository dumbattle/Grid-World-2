using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GridWorld {
    /// <summary>
    /// Grid World → Build Area from Spec… and Apply Spec to Area…: read a spec and its legend, then
    /// build a new area or change an existing one.
    /// </summary>
    public static class MapSpecMenu {
        const int MaxDialogLines = 8;
        const string BuildTitle = "Build Area from Spec";
        const string ApplyTitle = "Apply Spec to Area";

        [MenuItem("Grid World/Build Area from Spec…")]
        static void BuildFromMenu() {
            var path = PickSpec(BuildTitle);
            if (path == null) {
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
            ShowErrors(BuildTitle, "Can't build area from spec", errors);
        }

        [MenuItem("Grid World/Apply Spec to Area…")]
        static void ApplyFromMenu() {
            var path = PickSpec(ApplyTitle);
            if (path == null) {
                return;
            }

            var errors = new List<string>();
            var spec = Read(path, errors, out var legend);
            var data = errors.Count == 0 ? MapSpecApplier.FindArea(spec, legend, errors) : null;
            if (data == null) {
                ShowErrors(ApplyTitle, "Can't apply spec", errors);
                return;
            }

            var scene = SceneManager.GetSceneByPath(data.scenePath);
            bool opened = !scene.isLoaded;
            if (opened) {
                if (NewAreaCreator.HasUntitledScene()) {
                    ShowErrors(ApplyTitle, "Can't apply spec", new List<string> { "Unity can't open the area's scene next to an untitled one. Save it first, or open the area's scene." });
                    return;
                }
                scene = EditorSceneManager.OpenScene(data.scenePath, OpenSceneMode.Additive);
            }

            var area = MapSpecApplier.FindInScene(scene, spec.id);
            var warnings = new List<string>();
            MapAreaSnapshot snapshot = null;
            if (area == null) {
                errors.Add($"{data.scenePath} has no {nameof(AreaBehaviour)} with ID '{spec.id}'.");
            }
            else {
                snapshot = MapSpecApplier.Snapshot(area, errors, warnings);
            }
            if (snapshot == null) {
                CloseIfOpened(scene, opened);
                ShowErrors(ApplyTitle, "Can't apply spec", errors);
                return;
            }

            var changes = MapSpecDiff.Compute(spec, legend, snapshot);
            warnings.AddRange(changes.warnings);
            foreach (var warning in warnings) {
                Debug.LogWarning($"Apply {Path.GetFileName(path)}: {warning}");
            }

            if (changes.changes.Count == 0) {
                CloseIfOpened(scene, opened);
                EditorUtility.DisplayDialog(ApplyTitle, $"'{spec.id}' already matches the spec." + Lines("\n\nWarnings:\n", warnings), "OK");
                return;
            }

            var summary = new StringBuilder($"Apply {Path.GetFileName(path)} to '{spec.id}'?\n");
            foreach (var layer in LayersChanged(changes)) {
                summary.Append($"\n{layer}: {changes.CountOn(layer)} cells");
            }
            summary.Append(Lines("\n\nWarnings:\n", warnings));
            summary.Append("\n\nThe scene stays unsaved, and Undo reverts it.");
            if (!EditorUtility.DisplayDialog(ApplyTitle, summary.ToString(), "Apply", "Cancel")) {
                CloseIfOpened(scene, opened);
                return;
            }

            MapSpecApplier.Apply(area, legend, changes);
            Selection.activeGameObject = area.gameObject;
            Debug.Log($"Applied {Path.GetFileName(path)} to '{spec.id}': {changes.changes.Count} cells changed. Save the scene to bake them.");
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

        static string PickSpec(string title) {
            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                EditorUtility.DisplayDialog(title, "Leave Play mode first.", "OK");
                return null;
            }
            var folder = Directory.Exists(MapPaths.areas) ? MapPaths.areas : "";
            var path = EditorUtility.OpenFilePanel(title, folder, "txt");
            return string.IsNullOrEmpty(path) ? null : path;
        }

        static List<string> LayersChanged(MapSpecChanges changes) {
            var layers = new List<string>();
            foreach (var change in changes.changes) {
                if (!layers.Contains(change.layer)) {
                    layers.Add(change.layer);
                }
            }
            return layers;
        }

        static void CloseIfOpened(Scene scene, bool opened) {
            if (opened) {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        static void ShowErrors(string title, string prefix, List<string> errors) {
            foreach (var error in errors) {
                Debug.LogError($"{prefix}: {error}");
            }
            EditorUtility.DisplayDialog(title, Lines("", errors), "OK");
        }

        /// <summary>Up to <see cref="MaxDialogLines"/> lines after a heading, or nothing if there are none.</summary>
        static string Lines(string heading, List<string> lines) {
            if (lines.Count == 0) {
                return "";
            }
            var shown = lines.Count > MaxDialogLines ? lines.GetRange(0, MaxDialogLines) : lines;
            var more = lines.Count > MaxDialogLines ? $"\n…and {lines.Count - MaxDialogLines} more in the Console." : "";
            return heading + string.Join("\n", shown) + more;
        }
    }
}
