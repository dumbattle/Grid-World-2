using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Fixes feature IDs by hand. The Problems tab steps through clashes, bad formats, and missing
    /// IDs, showing every feature that shares a clashing ID so you choose which keeps it. The All
    /// Features tab lists every feature to browse and rename. Every change is an Undo step, and the
    /// window never saves a scene.
    /// </summary>
    public partial class FeatureIdWindow : EditorWindow {
        enum Tab { Problems, AllFeatures }

        static readonly string[] tabLabels = { "Problems", "All Features" };

        Tab tab;
        Vector2 scroll;

        [MenuItem("Grid World/Fix Feature IDs")]
        static void OpenFromMenu() {
            Open(null);
        }

        /// <summary>Opens the Problems tab, at <paramref name="feature"/>'s problem if it has one.</summary>
        public static void Open(IdentifiedFeatureBehaviour feature) {
            var window = GetWindow<FeatureIdWindow>("Feature IDs");
            window.tab = Tab.Problems;
            window.requested = feature;
            window.shownKey = null;
            window.Repaint();
        }

        void OnEnable() {
            FeatureIdIndex.changed += Repaint;
        }

        void OnDisable() {
            FeatureIdIndex.changed -= Repaint;
        }

        void OnGUI() {
            var chosen = (Tab)GUILayout.Toolbar((int)tab, tabLabels);
            if (chosen != tab) {
                tab = chosen;
                editingKey = null;
                GUIUtility.keyboardControl = 0;
            }
            EditorGUILayout.Space();

            var index = FeatureIdIndex.Current;
            if (tab == Tab.Problems) {
                DrawProblems(index);
            }
            else {
                DrawAllFeatures(index);
            }
        }

        // A stable key for an entry, so selections and drafts survive the index being rebuilt.
        static string KeyOf(FeatureIdIndex.Entry entry, int occurrence) {
            return entry.isOpen ? $"open:{entry.feature.GetInstanceID()}" : $"baked:{entry.scenePath}:{entry.id}:{occurrence}";
        }

        static List<string> KeysOf(IReadOnlyList<FeatureIdIndex.Entry> entries) {
            var keys = new List<string>(entries.Count);
            var base2count = new Dictionary<string, int>();
            foreach (var entry in entries) {
                var baseKey = KeyOf(entry, 0);
                base2count.TryGetValue(baseKey, out int occurrence);
                base2count[baseKey] = occurrence + 1;
                keys.Add(KeyOf(entry, occurrence));
            }
            return keys;
        }

        static void FrameInScene(IdentifiedFeatureBehaviour feature) {
            Selection.activeGameObject = feature.gameObject;
            EditorGUIUtility.PingObject(feature.gameObject);
            if (SceneView.lastActiveSceneView != null) {
                SceneView.lastActiveSceneView.FrameSelected();
            }
        }

        static void OpenScene(string path) {
            if (NewAreaCreator.HasUntitledScene()) {
                EditorUtility.DisplayDialog("Open Scene", "Unity can't open another scene next to an untitled one. Save it first.", "OK");
                return;
            }
            EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }

        static void SetIds(List<KeyValuePair<IdentifiedFeatureBehaviour, string>> changes, string undoName) {
            Undo.SetCurrentGroupName(undoName);
            int group = Undo.GetCurrentGroup();
            foreach (var change in changes) {
                var properties = new SerializedObject(change.Key);
                properties.FindProperty("id").stringValue = change.Value;
                properties.ApplyModifiedProperties();
            }
            Undo.CollapseUndoOperations(group);
            FeatureIdIndex.Invalidate();
        }

        static void SetId(IdentifiedFeatureBehaviour feature, string id) {
            var changes = new List<KeyValuePair<IdentifiedFeatureBehaviour, string>> {
                new KeyValuePair<IdentifiedFeatureBehaviour, string>(feature, id),
            };
            SetIds(changes, "Set Feature ID");
        }

        static bool IsEnter(Event e) {
            return e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter);
        }

        static string TileText(Vector2Int tile) {
            return $"({tile.x},{tile.y})";
        }
    }
}
