using System.Collections.Generic;
using UnityEditor;

namespace GridWorld {
    /// <summary>
    /// Keeps area scenes in the build's scene list, because they're loaded by path at runtime.
    /// </summary>
    public static class BuildSceneList {
        public static void Add(string scenePath) {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int index = scenes.FindIndex(s => s.path == scenePath);
            var guid = AssetDatabase.AssetPathToGUID(scenePath);

            if (index >= 0 && scenes[index].enabled && scenes[index].guid.ToString() == guid) {
                return;
            }

            // Replacing an existing entry refreshes its GUID if the scene was recreated at the same path.
            var entry = new EditorBuildSettingsScene(scenePath, true);
            if (index >= 0) {
                scenes[index] = entry;
            }
            else {
                scenes.Add(entry);
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
