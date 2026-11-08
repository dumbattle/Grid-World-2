using LPE;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Creates a new area scene and its data asset. Opened from the menu, a world data asset, or an
    /// area's New Neighbour buttons, each filling in different defaults.
    /// </summary>
    public class NewAreaWindow : EditorWindow {
        NewAreaRequest request = new NewAreaRequest();

        [MenuItem("Grid World/New Area…")]
        static void OpenFromMenu() {
            Open(null);
        }

        public static void Open(WorldDataSO world) {
            var window = Prepare();
            window.SetWorld(world);
        }

        /// <summary>Opens with bounds touching <paramref name="area"/> on the given side, and its folders.</summary>
        public static void OpenNeighbour(AreaBehaviour area, Direction4 direction) {
            var window = Prepare();
            window.SetWorld(area.world);
            window.request.bounds = NewAreaRules.NeighbourBounds(area.bounds, direction);
            window.request.sceneFolder = NewAreaRules.DefaultFolder(area.gameObject.scene.path, AssetDatabase.GetAssetPath(area.world));
            window.request.dataFolder = NewAreaRules.DefaultFolder(AssetDatabase.GetAssetPath(area.data), AssetDatabase.GetAssetPath(area.world));
        }

        static NewAreaWindow Prepare() {
            var window = GetWindow<NewAreaWindow>(true, "New Area");
            window.minSize = new Vector2(420, 260);
            window.request = new NewAreaRequest();
            return window;
        }

        void SetWorld(WorldDataSO world) {
            request.world = world;

            var last = NewAreaRules.LastArea(world);
            var worldPath = world == null ? null : AssetDatabase.GetAssetPath(world);
            request.sceneFolder = NewAreaRules.DefaultFolder(last == null ? null : last.scenePath, worldPath);
            request.dataFolder = NewAreaRules.DefaultFolder(last == null ? null : AssetDatabase.GetAssetPath(last), worldPath);
            request.bounds = NewAreaRules.DefaultBounds(world);
        }

        void OnGUI() {
            var world = (WorldDataSO)EditorGUILayout.ObjectField("World", request.world, typeof(WorldDataSO), false);
            if (world != request.world) {
                SetWorld(world);
            }

            var id = EditorGUILayout.TextField("Area ID", request.id);
            if (id != request.id) {
                // Names follow the ID until they're edited by hand.
                if (request.sceneName == NewAreaRules.SceneName(request.id) || request.sceneName == "") {
                    request.sceneName = NewAreaRules.SceneName(id);
                }
                if (request.dataName == NewAreaRules.DataName(request.id) || request.dataName == "") {
                    request.dataName = NewAreaRules.DataName(id);
                }
                request.id = id;
            }

            request.bounds = EditorGUILayout.RectIntField("Bounds", request.bounds);

            EditorGUILayout.Space();
            request.sceneFolder = FolderField("Scene folder", request.sceneFolder);
            request.sceneName = EditorGUILayout.TextField("Scene name", request.sceneName);
            request.dataFolder = FolderField("Data folder", request.dataFolder);
            request.dataName = EditorGUILayout.TextField("Area data name", request.dataName);

            EditorGUILayout.Space();
            request.openAdditively = EditorGUILayout.Toggle(new GUIContent("Open additively", "Keep the current scenes open and add the new one."), request.openAdditively);

            EditorGUILayout.Space();
            var error = NewAreaCreator.FindError(request);
            if (error != null) {
                EditorGUILayout.HelpBox(error, MessageType.Warning);
            }
            else {
                EditorGUILayout.HelpBox($"Creates {request.scenePath}\nand {request.dataPath}", MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(error != null)) {
                if (GUILayout.Button("Create")
                    && (request.openAdditively || EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())) {
                    var area = NewAreaCreator.Create(request);
                    if (area != null) {
                        Selection.activeGameObject = area.gameObject;
                        Close();
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        static string FolderField(string label, string folder) {
            using (new EditorGUILayout.HorizontalScope()) {
                folder = EditorGUILayout.TextField(label, folder);
                if (GUILayout.Button("…", GUILayout.Width(28))) {
                    var picked = EditorUtility.OpenFolderPanel(label, folder, "");
                    if (!string.IsNullOrEmpty(picked)) {
                        var relative = FileUtil.GetProjectRelativePath(picked);
                        if (string.IsNullOrEmpty(relative)) {
                            Debug.LogWarning($"'{picked}' is outside the project.");
                        }
                        else {
                            folder = relative;
                            GUI.FocusControl(null);
                        }
                    }
                }
            }
            return folder;
        }
    }
}
