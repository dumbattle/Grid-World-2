using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GridWorld.Tests {
    public class AreaDeleterTests {
        const string ParentFolder = "Assets";
        const string FolderName = "AreaDeleterTestTemp";
        const string Folder = ParentFolder + "/" + FolderName;
        const string WorldPath = Folder + "/World.asset";

        WorldDataSO world;

        [SetUp]
        public void SetUp() {
            if (!AssetDatabase.IsValidFolder(Folder)) {
                AssetDatabase.CreateFolder(ParentFolder, FolderName);
            }

            // Saved as assets, because opening a scene in single mode unloads in-memory ones.
            var table = ScriptableObject.CreateInstance<TilePropertyTableSO>();
            AssetDatabase.CreateAsset(table, $"{Folder}/Table.asset");
            world = ScriptableObject.CreateInstance<WorldDataSO>();
            AssetDatabase.CreateAsset(world, WorldPath);

            var properties = new SerializedObject(world);
            properties.FindProperty("tilePropertyTable").objectReferenceValue = table;
            properties.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown() {
            // Swap to an empty scene before deleting the files. The Test Runner restores the open
            // scenes after the run.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (buildScenes.RemoveAll(s => s.path.StartsWith(Folder + "/")) > 0) {
                EditorBuildSettings.scenes = buildScenes.ToArray();
            }

            AssetDatabase.DeleteAsset(Folder);
        }

        NewAreaRequest Request(string id, int x, bool additive = false) {
            return new NewAreaRequest {
                id = id,
                world = world,
                sceneFolder = Folder,
                dataFolder = Folder,
                sceneName = NewAreaRules.SceneName(id),
                dataName = NewAreaRules.DataName(id),
                bounds = new RectInt(x, 0, 4, 4),
                openAdditively = additive,
            };
        }

        static WorldDataSO LoadWorld() {
            return AssetDatabase.LoadAssetAtPath<WorldDataSO>(WorldPath);
        }

        [Test]
        public void Delete_ValidArea_RemovesOnlyThatAreaFromWorld() {
            var keepRequest = Request("keep", 0);
            NewAreaCreator.Create(keepRequest);
            var area = NewAreaCreator.Create(Request("gone", 10));

            Assert.IsTrue(AreaDeleter.Delete(area));

            var keep = AssetDatabase.LoadAssetAtPath<AreaDataSO>(keepRequest.dataPath);
            CollectionAssert.AreEqual(new[] { keep }, LoadWorld().areas);
        }

        [Test]
        public void Delete_ValidArea_RemovesSceneFromBuildList() {
            var request = Request("gone", 0);
            var area = NewAreaCreator.Create(request);

            AreaDeleter.Delete(area);

            Assert.IsFalse(Array.Exists(EditorBuildSettings.scenes, s => s.path == request.scenePath));
        }

        [Test]
        public void Delete_ValidArea_RemovesBothFilesAndMetas() {
            var request = Request("gone", 0);
            var area = NewAreaCreator.Create(request);

            AreaDeleter.Delete(area);

            Assert.IsFalse(File.Exists(request.scenePath));
            Assert.IsFalse(File.Exists(request.scenePath + ".meta"));
            Assert.IsFalse(File.Exists(request.dataPath));
            Assert.IsFalse(File.Exists(request.dataPath + ".meta"));
        }

        [Test]
        public void Delete_SecondWorldListsArea_RemovesFromBothWorlds() {
            var request = Request("gone", 0);
            var area = NewAreaCreator.Create(request);

            var other = ScriptableObject.CreateInstance<WorldDataSO>();
            AssetDatabase.CreateAsset(other, $"{Folder}/OtherWorld.asset");
            other.AddArea(area.data);
            EditorUtility.SetDirty(other);
            AssetDatabase.SaveAssets();

            AreaDeleter.Delete(area);

            CollectionAssert.IsEmpty(LoadWorld().areas);
            CollectionAssert.IsEmpty(AssetDatabase.LoadAssetAtPath<WorldDataSO>($"{Folder}/OtherWorld.asset").areas);
        }

        [Test]
        public void Delete_OnlyOpenScene_LeavesAnEmptySceneOpen() {
            var area = NewAreaCreator.Create(Request("gone", 0));

            Assert.IsTrue(AreaDeleter.Delete(area));

            Assert.AreEqual(1, SceneManager.loadedSceneCount);
            Assert.AreEqual("", SceneManager.GetActiveScene().path);
        }

        [Test]
        public void Delete_AnotherSceneOpen_LeavesThatSceneOpen() {
            var keepRequest = Request("keep", 0);
            NewAreaCreator.Create(keepRequest);
            var area = NewAreaCreator.Create(Request("gone", 10, true));

            Assert.IsTrue(AreaDeleter.Delete(area));

            Assert.AreEqual(1, SceneManager.loadedSceneCount);
            Assert.IsTrue(SceneManager.GetSceneByPath(keepRequest.scenePath).isLoaded);
        }

        [Test]
        public void FindError_ValidArea_ReturnsNull() {
            var area = NewAreaCreator.Create(Request("fresh", 0));

            Assert.IsNull(AreaDeleter.FindError(area));
        }

        [Test]
        public void FindError_UnsavedScene_ReturnsError() {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var area = new GameObject("Area").AddComponent<AreaBehaviour>();

            StringAssert.Contains("never been saved", AreaDeleter.FindError(area));
        }

        [Test]
        public void FindError_NoData_ReturnsError() {
            var area = NewAreaCreator.Create(Request("fresh", 0));

            var properties = new SerializedObject(area);
            properties.FindProperty("areaData").objectReferenceValue = null;
            properties.ApplyModifiedPropertiesWithoutUndo();

            StringAssert.Contains("no area data", AreaDeleter.FindError(area));
        }

        [Test]
        public void FindError_DataBelongsToAnotherScene_ReturnsError() {
            var area = NewAreaCreator.Create(Request("fresh", 0));
            area.data.SetBakedData("fresh", "Assets/Other.unity", area.bounds, Vector2Int.zero, new ushort[0], new List<WorldFeatureData>());

            StringAssert.Contains("Assets/Other.unity", AreaDeleter.FindError(area));
        }

        [Test]
        public void Delete_InvalidArea_KeepsWorldListAndFiles() {
            var request = Request("fresh", 0);
            var area = NewAreaCreator.Create(request);
            area.data.SetBakedData("fresh", "Assets/Other.unity", area.bounds, Vector2Int.zero, new ushort[0], new List<WorldFeatureData>());

            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Can't delete area"));
            Assert.IsFalse(AreaDeleter.Delete(area));

            Assert.AreEqual(1, LoadWorld().areas.Count);
            Assert.IsTrue(File.Exists(request.scenePath));
            Assert.IsTrue(File.Exists(request.dataPath));
        }
    }
}
