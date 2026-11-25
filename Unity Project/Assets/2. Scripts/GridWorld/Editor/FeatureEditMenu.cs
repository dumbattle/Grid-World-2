using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Grid World → Apply Feature Edits…: reads an edit file, shows what it would do, and applies it
    /// only if no edit conflicts.
    /// </summary>
    public static class FeatureEditMenu {
        const int MaxDialogLines = 10;
        const string Title = "Apply Feature Edits";

        [MenuItem("Grid World/Apply Feature Edits…")]
        static void ApplyFromMenu() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                EditorUtility.DisplayDialog(Title, "Leave Play mode first.", "OK");
                return;
            }
            var folder = Directory.Exists(MapPaths.edits) ? MapPaths.edits : MapPaths.root;
            var path = EditorUtility.OpenFilePanel(Title, folder, "txt");
            if (string.IsNullOrEmpty(path)) {
                return;
            }
            var name = Path.GetFileName(path);

            var errors = new List<string>();
            var file = FeatureEditParser.Parse(File.ReadAllText(path), name, errors);
            if (errors.Count > 0) {
                Fail(errors, errors);
                return;
            }

            var plan = FeatureEditApplier.Prepare(file);
            if (plan.errors.Count > 0) {
                Fail(plan.errors, plan.errors);
                return;
            }

            var conflicts = Describe(plan, FeatureEditStatus.Conflict);
            var changes = Describe(plan, FeatureEditStatus.Change);
            var already = Describe(plan, FeatureEditStatus.AlreadyApplied);
            var warnings = new List<string>();
            foreach (var result in plan.results) {
                foreach (var warning in result.warnings) {
                    warnings.Add($"{name}:{result.edit.line}: {warning}");
                }
            }

            if (conflicts.Count > 0) {
                FeatureEditApplier.Cancel(plan);
                var message = new StringBuilder($"Nothing was applied, because {conflicts.Count} of {plan.results.Count} edits conflict with the scene.");
                message.Append(Lines("\n\nConflicts:\n", conflicts));
                Fail(conflicts, new List<string> { message.ToString() });
                return;
            }

            if (changes.Count == 0) {
                FeatureEditApplier.Cancel(plan);
                EditorUtility.DisplayDialog(Title, $"Every edit in {name} is already applied to '{file.areaId}'." + Lines("\n\n", already), "OK");
                return;
            }

            var summary = new StringBuilder($"Apply {changes.Count} edits from {name} to '{file.areaId}'?");
            summary.Append(Lines("\n\nChanges:\n", changes));
            summary.Append(Lines("\n\nAlready applied:\n", already));
            summary.Append(Lines("\n\nWarnings:\n", warnings));
            summary.Append("\n\nThe scene stays unsaved, and Undo reverts it.");
            foreach (var warning in warnings) {
                Debug.LogWarning($"{Title}: {warning}");
            }
            if (!EditorUtility.DisplayDialog(Title, summary.ToString(), "Apply", "Cancel")) {
                FeatureEditApplier.Cancel(plan);
                return;
            }

            FeatureEditApplier.Commit(plan);
            Selection.activeGameObject = plan.area.gameObject;
            Debug.Log($"Applied {changes.Count} edits from {name} to '{file.areaId}'. Save the scene to bake them.");
        }

        static List<string> Describe(FeatureEditPlan plan, FeatureEditStatus status) {
            var lines = new List<string>();
            foreach (var result in plan.results) {
                if (result.status != status) {
                    continue;
                }
                var detail = result.message != null ? $"\n    {result.message}" : "";
                lines.Add($"line {result.edit.line}: {result.edit.text}{detail}");
            }
            return lines;
        }

        static void Fail(List<string> logged, List<string> shown) {
            foreach (var line in logged) {
                Debug.LogError($"{Title}: {line}");
            }
            EditorUtility.DisplayDialog(Title, Lines("", shown), "OK");
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
