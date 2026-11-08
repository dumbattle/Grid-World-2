using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GridWorld {
    /// <summary>
    /// Bakes an area when its scene is saved, bakes everything before a build, and adds the
    /// <b>Grid World → Bake All</b> menu command.
    /// </summary>
    [InitializeOnLoad]
    public class AreaBakeTriggers : IPreprocessBuildWithReport {
        static AreaBakeTriggers() {
            EditorSceneManager.sceneSaved += OnSceneSaved;
        }

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) {
            if (!AreaBaker.BakeAll()) {
                throw new BuildFailedException("Grid World bake failed. See the console for details.");
            }
        }

        [MenuItem("Grid World/Bake All")]
        static void BakeAllMenu() {
            if (AreaBaker.BakeAll()) {
                Debug.Log("Grid World: baked all areas.");
            }
            else {
                Debug.LogError("Grid World: bake finished with errors. See above.");
            }
        }

        static void OnSceneSaved(Scene scene) {
            var area = AreaBaker.FindArea(scene);
            if (area != null) {
                AreaBaker.Bake(area);
            }
        }
    }
}
