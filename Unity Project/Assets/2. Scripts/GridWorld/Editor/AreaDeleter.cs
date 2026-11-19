using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GridWorld {
    /// <summary>
    /// Removes an area from every list that names it, then moves its scene and data asset to the
    /// Recycle Bin. To restore an area, put the files back and save its scene, which bakes it again.
    /// </summary>
    public static class AreaDeleter {
        const string CantDeleteTitle = "Can't delete area";

        [MenuItem("CONTEXT/AreaBehaviour/Delete Area…")]
        static void DeleteFromMenu(MenuCommand command) {
            var area = (AreaBehaviour)command.context;

            // Unity runs a context menu once per selected object, so only the active one reports this.
            if (Selection.gameObjects.Length > 1) {
                if (area.gameObject == Selection.activeGameObject) {
                    EditorUtility.DisplayDialog(CantDeleteTitle, "Select one area at a time.", "OK");
                }
                return;
            }

            var error = FindError(area);
            if (error != null) {
                EditorUtility.DisplayDialog(CantDeleteTitle, error, "OK");
                return;
            }

            var message = $"This moves these files to the Recycle Bin:\n\n{area.gameObject.scene.path}\n{AssetDatabase.GetAssetPath(area.data)}\n\n"
                + "It also removes the area from the world and the build's scene list. Unsaved changes in the scene will be lost.";

            // Enter picks the first button and Esc the second, so neither of them can delete.
            const int DeleteButton = 2;
            if (EditorUtility.DisplayDialogComplex($"Delete area '{area.areaId}'?", message, "Cancel", "Keep Area", "Delete") == DeleteButton) {
                Delete(area);
            }
        }

        /// <summary>A message describing why the area can't be deleted, or null if it can.</summary>
        public static string FindError(AreaBehaviour area) {
            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                return "Exit Play mode first.";
            }

            var scenePath = area.gameObject.scene.path;
            if (string.IsNullOrEmpty(scenePath)) {
                return "This scene has never been saved, so there's no area to delete.";
            }
            if (area.data == null) {
                return "This area has no area data asset. Set it, or delete the scene by hand.";
            }
            if (area.data.scenePath != scenePath) {
                return $"Area data '{area.data.name}' belongs to '{area.data.scenePath}', not this scene. If this scene has moved, save it first.";
            }
            return null;
        }

        /// <summary>Returns false if anything failed. Every problem is logged. Closes the scene without saving, so ask first.</summary>
        public static bool Delete(AreaBehaviour area) {
            var error = FindError(area);
            if (error != null) {
                Debug.LogError($"Can't delete area: {error}", area);
                return false;
            }

            var id = area.areaId;
            var scene = area.gameObject.scene;
            var scenePath = scene.path;
            var dataPath = AssetDatabase.GetAssetPath(area.data);

            // The lists go first, so a failure later can't leave a world with a missing entry.
            RemoveFromWorlds(area.data);
            BuildSceneList.Remove(scenePath);

            // Closing doesn't save, so the save hook can't bake the area back in.
            CloseScene(scene);

            bool succeeded = MoveToTrash(scenePath);
            succeeded &= MoveToTrash(dataPath);

            if (succeeded) {
                Debug.Log($"Deleted area '{id}'.");
            }
            return succeeded;
        }

        static void RemoveFromWorlds(AreaDataSO data) {
            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(WorldDataSO)}")) {
                var world = AssetDatabase.LoadAssetAtPath<WorldDataSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (world.RemoveArea(data)) {
                    EditorUtility.SetDirty(world);
                }
            }
            AssetDatabase.SaveAssets();
        }

        static void CloseScene(Scene scene) {
            // Unity can't close the last loaded scene.
            if (SceneManager.loadedSceneCount > 1) {
                EditorSceneManager.CloseScene(scene, true);
            }
            else {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        static bool MoveToTrash(string path) {
            if (AssetDatabase.MoveAssetToTrash(path)) {
                return true;
            }
            Debug.LogError($"Couldn't move '{path}' to the Recycle Bin. Delete it by hand.");
            return false;
        }
    }
}
