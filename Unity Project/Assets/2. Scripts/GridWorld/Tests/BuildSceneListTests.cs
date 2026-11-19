using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace GridWorld.Tests {
    public class BuildSceneListTests {
        const string ParentFolder = "Assets";
        const string FolderName = "BuildSceneListTestTemp";
        const string Folder = ParentFolder + "/" + FolderName;
        const string ScenePath = Folder + "/Scene.unity";
        const string SettingsPath = "ProjectSettings/EditorBuildSettings.asset";

        [SetUp]
        public void SetUp() {
            if (!AssetDatabase.IsValidFolder(Folder)) {
                AssetDatabase.CreateFolder(ParentFolder, FolderName);
            }

            // The Test Runner's own scene is untitled, so a single-mode scene is the only option.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        [TearDown]
        public void TearDown() {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (buildScenes.RemoveAll(s => s.path.StartsWith(Folder + "/")) > 0) {
                EditorBuildSettings.scenes = buildScenes.ToArray();
                AssetDatabase.SaveAssets();
            }

            AssetDatabase.DeleteAsset(Folder);
        }

        [Test]
        public void Add_NewScene_WritesListToDisk() {
            BuildSceneList.Add(ScenePath);

            StringAssert.Contains(ScenePath, File.ReadAllText(SettingsPath));
        }

        [Test]
        public void Remove_ListedScene_WritesListToDisk() {
            BuildSceneList.Add(ScenePath);

            BuildSceneList.Remove(ScenePath);

            StringAssert.DoesNotContain(ScenePath, File.ReadAllText(SettingsPath));
        }
    }
}
