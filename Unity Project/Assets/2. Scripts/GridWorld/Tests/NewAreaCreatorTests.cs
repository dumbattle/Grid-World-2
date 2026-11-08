using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace GridWorld.Tests {
    public class NewAreaCreatorTests {
        const string ParentFolder = "Assets";
        const string FolderName = "NewAreaCreatorTestTemp";
        const string Folder = ParentFolder + "/" + FolderName;

        WorldDataSO world;
        Scene scene;

        [SetUp]
        public void SetUp() {
            scene = default;
            if (!AssetDatabase.IsValidFolder(Folder)) {
                AssetDatabase.CreateFolder(ParentFolder, FolderName);
            }

            // Saved as assets, because opening a scene in single mode unloads in-memory ones.
            var table = ScriptableObject.CreateInstance<TilePropertyTableSO>();
            AssetDatabase.CreateAsset(table, $"{Folder}/Table.asset");
            world = ScriptableObject.CreateInstance<WorldDataSO>();
            AssetDatabase.CreateAsset(world, $"{Folder}/World.asset");

            var properties = new SerializedObject(world);
            properties.FindProperty("tilePropertyTable").objectReferenceValue = table;
            properties.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown() {
            // Tests create scenes in single mode, because the Test Runner's own scene is untitled and
            // Unity won't add a scene next to one. Swap to an empty scene before deleting the files.
            // The Test Runner restores the open scenes after the run.
            if (scene.IsValid()) {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            // The bake adds the scene to the build list, so take it out again.
            var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (buildScenes.RemoveAll(s => s.path.StartsWith(Folder + "/")) > 0) {
                EditorBuildSettings.scenes = buildScenes.ToArray();
            }

            AssetDatabase.DeleteAsset(Folder);
        }

        NewAreaRequest Request(string id) {
            return new NewAreaRequest {
                id = id,
                world = world,
                sceneFolder = Folder,
                dataFolder = Folder,
                sceneName = NewAreaRules.SceneName(id),
                dataName = NewAreaRules.DataName(id),
                bounds = new RectInt(2, 3, 5, 4),
                openAdditively = false,
            };
        }

        [Test]
        public void FindError_AdditiveWithUntitledScene_ReturnsError() {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var request = Request("fresh");
            request.openAdditively = true;

            StringAssert.Contains("untitled", NewAreaCreator.FindError(request));
        }

        [Test]
        public void Create_ValidRequest_WiresAreaAndBakesIntoWorld() {
            var request = Request("fresh");

            var area = NewAreaCreator.Create(request);
            scene = area.gameObject.scene;

            // Reload both, since opening the scene may have unloaded the test's references.
            var data = AssetDatabase.LoadAssetAtPath<AreaDataSO>(request.dataPath);
            var world = AssetDatabase.LoadAssetAtPath<WorldDataSO>($"{Folder}/World.asset");
            Assert.IsNotNull(data);
            Assert.AreEqual("fresh", area.areaId);
            Assert.AreEqual(data, area.data);
            Assert.AreEqual(world, area.world);
            Assert.AreEqual(request.bounds, area.bounds);
            Assert.AreEqual(request.scenePath, scene.path);

            Assert.AreEqual(request.scenePath, data.scenePath);
            CollectionAssert.Contains(world.areas, data);
        }

        [Test]
        public void Create_ValidRequest_AddsHiddenLogicLayer() {
            var area = NewAreaCreator.Create(Request("fresh"));
            scene = area.gameObject.scene;

            var logic = area.GetComponentInChildren<LogicLayerBehaviour>(true);
            Assert.IsNotNull(logic);
            Assert.IsFalse(logic.GetComponent<TilemapRenderer>().enabled);
        }

        [Test]
        public void FindError_FileExists_ReportsPath() {
            var first = Request("fresh");
            scene = NewAreaCreator.Create(first).gameObject.scene;

            var second = Request("other");
            second.bounds = new RectInt(50, 0, 4, 4);
            second.sceneName = first.sceneName;

            StringAssert.Contains(first.scenePath, NewAreaCreator.FindError(second));
        }

        [Test]
        public void FindError_NoWorld_ReturnsError() {
            var request = Request("fresh");
            request.world = null;

            Assert.IsNotNull(NewAreaCreator.FindError(request));
        }

        [Test]
        public void FindError_MissingFolder_ReturnsError() {
            var request = Request("fresh");
            request.sceneFolder = Folder + "/Missing";

            StringAssert.Contains("doesn't exist", NewAreaCreator.FindError(request));
        }
    }
}
