using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GridWorld.Tests {
    public class MapSpecBuilderTests {
        const string ParentFolder = "Assets";
        const string FolderName = "MapSpecBuilderTestTemp";
        const string Folder = ParentFolder + "/" + FolderName;
        const string WorldPath = Folder + "/World.asset";
        const string GrassPath = Folder + "/Grass.asset";
        const string TreePath = Folder + "/Tree.asset";
        const string WalkPath = Folder + "/Walk.asset";
        const string WallPath = Folder + "/Wall.asset";

        [SetUp]
        public void SetUp() {
            if (!AssetDatabase.IsValidFolder(Folder)) {
                AssetDatabase.CreateFolder(ParentFolder, FolderName);
            }

            // Saved as assets, because opening a scene in single mode unloads in-memory ones.
            var table = ScriptableObject.CreateInstance<TilePropertyTableSO>();
            AssetDatabase.CreateAsset(table, $"{Folder}/Table.asset");
            var world = ScriptableObject.CreateInstance<WorldDataSO>();
            AssetDatabase.CreateAsset(world, WorldPath);
            foreach (var path in new[] { GrassPath, TreePath, WalkPath, WallPath }) {
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<Tile>(), path);
            }

            var properties = new SerializedObject(world);
            properties.FindProperty("tilePropertyTable").objectReferenceValue = table;
            properties.ApplyModifiedPropertiesWithoutUndo();
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

        static MapLegend Legend() {
            return new MapLegend {
                layers = new[] {
                    new MapLegendLayer { name = "Ground", sortingLayer = SortingLayerNames.Ground },
                    new MapLegendLayer { name = "Trees", sortingLayer = SortingLayerNames.EntityOnGround },
                },
                entries = new[] {
                    new MapLegendEntry { symbol = "g", layer = "Ground", tile = GrassPath, logic = WalkPath },
                    new MapLegendEntry { symbol = "T", layer = "Trees", tile = TreePath, logic = WallPath },
                },
            };
        }

        static MapSpec Spec(string id = "drafted", string bounds = "4 2 3 2") {
            var text =
                $"id: {id}\n" +
                $"world: {WorldPath}\n" +
                $"bounds: {bounds}\n" +
                "legend: test\n" +
                $"sceneFolder: {Folder}\n" +
                $"dataFolder: {Folder}\n" +
                "[Ground]\n" +
                "ggg\n" +
                "gg.\n" +
                "[Trees]\n" +
                "T..\n" +
                "...\n";
            var errors = new List<string>();
            var spec = MapSpecParser.Parse(text, "test.txt", errors);
            CollectionAssert.IsEmpty(errors);
            return spec;
        }

        static Tilemap TilemapNamed(AreaBehaviour area, string name) {
            return area.transform.Find(name).GetComponent<Tilemap>();
        }

        static TileBase Load(string path) {
            return AssetDatabase.LoadAssetAtPath<TileBase>(path);
        }

        [Test]
        public void Build_ValidSpec_PaintsLayersAtWorldCells() {
            var area = MapSpecBuilder.Build(Spec(), Legend(), false);

            Assert.IsNotNull(area);
            var ground = TilemapNamed(area, "Ground");
            Assert.AreEqual(Load(GrassPath), ground.GetTile(new Vector3Int(4, 3, 0)));
            Assert.AreEqual(Load(GrassPath), ground.GetTile(new Vector3Int(5, 2, 0)));
            Assert.IsNull(ground.GetTile(new Vector3Int(6, 2, 0)));
            Assert.AreEqual(Load(TreePath), TilemapNamed(area, "Trees").GetTile(new Vector3Int(4, 3, 0)));
        }

        [Test]
        public void Build_ValidSpec_CreatesMissingLayerOnItsSortingLayer() {
            var area = MapSpecBuilder.Build(Spec(), Legend(), false);

            var renderer = area.transform.Find("Trees").GetComponent<TilemapRenderer>();
            Assert.AreEqual(SortingLayerNames.EntityOnGround, renderer.sortingLayerName);
            Assert.AreEqual(TilemapRenderer.Mode.Individual, renderer.mode);
        }

        [Test]
        public void Build_ValidSpec_PaintsLogicFromLegend() {
            var area = MapSpecBuilder.Build(Spec(), Legend(), false);

            var logic = TilemapNamed(area, MapSpecParser.LogicLayer);
            Assert.AreEqual(Load(WallPath), logic.GetTile(new Vector3Int(4, 3, 0)));
            Assert.AreEqual(Load(WalkPath), logic.GetTile(new Vector3Int(5, 3, 0)));
            Assert.IsNull(logic.GetTile(new Vector3Int(6, 2, 0)));
        }

        [Test]
        public void Build_ValidSpec_BakesIntoWorld() {
            var area = MapSpecBuilder.Build(Spec(), Legend(), false);

            var world = AssetDatabase.LoadAssetAtPath<WorldDataSO>(WorldPath);
            CollectionAssert.Contains(world.areas, area.data);
            Assert.AreEqual(new RectInt(4, 2, 3, 2), area.data.worldBounds);
        }

        [Test]
        public void FindErrors_TakenId_ReportsIt() {
            MapSpecBuilder.Build(Spec(), Legend(), false);

            var errors = MapSpecBuilder.FindErrors(Spec("drafted", "20 0 3 2"), Legend(), false);

            Assert.IsTrue(errors.Exists(e => e.Contains("already used")));
        }

        [Test]
        public void FindErrors_MissingTileAndSortingLayer_ReportsEach() {
            var legend = Legend();
            legend.entries[0].tile = Folder + "/Missing.asset";
            legend.layers[1].sortingLayer = "No Such Layer";

            var errors = MapSpecBuilder.FindErrors(Spec(), legend, false);

            Assert.IsTrue(errors.Exists(e => e.Contains("Missing.asset")));
            Assert.IsTrue(errors.Exists(e => e.Contains("No Such Layer")));
        }

        [Test]
        public void Export_BuiltArea_WritesArtAndLogicAtTileResolution() {
            var area = MapSpecBuilder.Build(Spec(), Legend(), false);
            var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GridWorldPreviewTest");

            try {
                var written = AreaPreviewExporter.Export(area, folder);

                Assert.AreEqual(2, written.Count);
                var texture = new Texture2D(1, 1);
                // The test tiles have no sprites, so the fallback of 16 pixels per tile applies.
                Assert.IsTrue(texture.LoadImage(System.IO.File.ReadAllBytes(written[1])));
                Assert.AreEqual(3 * 16, texture.width);
                Assert.AreEqual(2 * 16, texture.height);
                Object.DestroyImmediate(texture);
            }
            finally {
                if (System.IO.Directory.Exists(folder)) {
                    System.IO.Directory.Delete(folder, true);
                }
            }
        }

        [Test]
        public void Build_InvalidSpec_CreatesNothing() {
            var legend = Legend();
            legend.entries[0].tile = Folder + "/Missing.asset";

            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Missing.asset"));
            Assert.IsNull(MapSpecBuilder.Build(Spec(), legend, false));

            Assert.IsFalse(System.IO.File.Exists($"{Folder}/Area_drafted.unity"));
        }
    }
}
