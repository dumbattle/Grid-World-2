using System.IO;
using GridWorld;
using LPE;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Game {
    /// <summary>
    /// <b>Grid World → Test → Create Test World</b>: builds two touching placeholder areas, a main
    /// scene, and their data assets, then bakes them. Everything goes in Game folders and is replaced
    /// on each run. Runs without dialogs in batch mode.
    /// </summary>
    public static class CreateTestWorld {
        const string ScenesFolder = "Assets/1. Scenes/Game";
        const string SpritesFolder = "Assets/3. Sprites/Game";
        const string TilesFolder = "Assets/6. Tiles/Game";
        const string DataFolder = "Assets/8. Data/Game";
        static readonly string[] folders = { ScenesFolder, SpritesFolder, TilesFolder, DataFolder };

        // Constants so the large-area stress test only needs a change here.
        const int AreaWidth = 24;
        const int AreaHeight = 16;
        const int TilePixels = 16;
        static readonly Vector2Int spawnTile = new Vector2Int(5, 5);

        [MenuItem("Grid World/Test/Create Test World")]
        public static void Create() {
            bool batch = Application.isBatchMode;

            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
                return;
            }
            if (!batch && AnyFolderExists() && !EditorUtility.DisplayDialog(
                    "Create Test World", "This replaces the existing test world in the Game folders.", "Replace", "Cancel")) {
                return;
            }

            // Leave the scenes about to be deleted.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            foreach (var folder in folders) {
                if (AssetDatabase.IsValidFolder(folder)) {
                    AssetDatabase.DeleteAsset(folder);
                }
                EnsureFolder(folder);
            }

            var groundA = CreateTile("GroundA", new Color32(92, 160, 72, 255), new Color32(80, 140, 62, 255));
            var groundB = CreateTile("GroundB", new Color32(164, 168, 80, 255), new Color32(146, 150, 68, 255));
            var wall = CreateTile("Wall", new Color32(70, 66, 80, 255), new Color32(110, 104, 122, 255));

            var table = CreateAsset<TilePropertyTableSO>("TilePropertyTable");
            table.SetEntry(groundA, TileBits.EnterAll);
            table.SetEntry(groundB, TileBits.EnterAll);
            table.SetEntry(wall, 0);
            EditorUtility.SetDirty(table);

            var settings = CreateAsset<WorldSettingsSO>("WorldSettings");
            var player = CreateAsset<EntityDefinitionSO>("Player");

            var world = CreateAsset<WorldDataSO>("WorldData");
            var worldProperties = new SerializedObject(world);
            worldProperties.FindProperty("worldSettings").objectReferenceValue = settings;
            worldProperties.FindProperty("tilePropertyTable").objectReferenceValue = table;
            worldProperties.ApplyModifiedPropertiesWithoutUndo();

            CreateAsset<AreaDataSO>("AreaA");
            CreateAsset<AreaDataSO>("AreaB");
            AssetDatabase.SaveAssets();

            // Area scenes are created additively, which needs a saved scene already open.
            var mainPath = $"{ScenesFolder}/TestMain.unity";
            CreateMainScene(mainPath, settings);
            BuildSceneList.Add(mainPath);

            // Opening the main scene as Single unloaded the assets created above, so load them again.
            world = Load<WorldDataSO>(DataFolder, "WorldData");
            player = Load<EntityDefinitionSO>(DataFolder, "Player");
            var dataA = Load<AreaDataSO>(DataFolder, "AreaA");
            var dataB = Load<AreaDataSO>(DataFolder, "AreaB");
            var groundTileA = Load<TileBase>(TilesFolder, "GroundA");
            var groundTileB = Load<TileBase>(TilesFolder, "GroundB");
            var wallTile = Load<TileBase>(TilesFolder, "Wall");

            var boundsA = new RectInt(0, 0, AreaWidth, AreaHeight);
            var boundsB = new RectInt(AreaWidth, 0, AreaWidth, AreaHeight);

            // Walls on the outer edges only, so the two areas join along their shared border.
            CreateAreaScene("test_a", boundsA, dataA, world, groundTileA, wallTile, westWall: true, eastWall: false,
                block: new RectInt(8, 6, 3, 3), player: player);
            CreateAreaScene("test_b", boundsB, dataB, world, groundTileB, wallTile, westWall: false, eastWall: true,
                block: new RectInt(AreaWidth + 8, 9, 4, 2), player: null);

            AssetDatabase.SaveAssets();
            Debug.Log("Grid World: created the test world.");
        }

        static T Load<T>(string folder, string name) where T : Object {
            return AssetDatabase.LoadAssetAtPath<T>($"{folder}/{name}.asset");
        }

        static bool AnyFolderExists() {
            foreach (var folder in folders) {
                if (AssetDatabase.IsValidFolder(folder)) {
                    return true;
                }
            }
            return false;
        }

        static void EnsureFolder(string path) {
            var parts = path.Split('/');
            var current = parts[0];

            for (int i = 1; i < parts.Length; i++) {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next)) {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        static T CreateAsset<T>(string name) where T : ScriptableObject {
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, $"{DataFolder}/{name}.asset");
            return asset;
        }

        static Tile CreateTile(string name, Color32 fill, Color32 edge) {
            var spritePath = $"{SpritesFolder}/{name}.png";
            var texture = new Texture2D(TilePixels, TilePixels);
            var pixels = new Color32[TilePixels * TilePixels];

            for (int y = 0; y < TilePixels; y++) {
                for (int x = 0; x < TilePixels; x++) {
                    bool border = x == 0 || y == 0 || x == TilePixels - 1 || y == TilePixels - 1;
                    pixels[x + y * TilePixels] = border ? edge : fill;
                }
            }
            texture.SetPixels32(pixels);
            File.WriteAllBytes(spritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(spritePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(spritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = TilePixels;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            tile.colliderType = Tile.ColliderType.None;
            AssetDatabase.CreateAsset(tile, $"{TilesFolder}/{name}.asset");
            return tile;
        }

        static void CreateMainScene(string path, WorldSettingsSO settings) {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(spawnTile.x + 0.5f, spawnTile.y + 0.5f, -10);

            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = settings.viewTiles.y / 2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(24, 24, 32, 255);

            EditorSceneManager.SaveScene(scene, path);
        }

        static void CreateAreaScene(string areaId, RectInt bounds, AreaDataSO data, WorldDataSO world, TileBase ground, TileBase wall,
                                    bool westWall, bool eastWall, RectInt block, EntityDefinitionSO player) {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);

            var root = new GameObject($"Area {areaId}");
            root.AddComponent<Grid>();
            var area = root.AddComponent<AreaBehaviour>();

            var areaProperties = new SerializedObject(area);
            areaProperties.FindProperty("id").stringValue = areaId;
            areaProperties.FindProperty("areaData").objectReferenceValue = data;
            areaProperties.FindProperty("worldData").objectReferenceValue = world;
            areaProperties.FindProperty("tileBounds").rectIntValue = bounds;
            areaProperties.ApplyModifiedPropertiesWithoutUndo();

            var groundObject = new GameObject("Ground");
            groundObject.transform.SetParent(root.transform, false);
            var tilemap = groundObject.AddComponent<Tilemap>();
            groundObject.AddComponent<TilemapRenderer>();
            groundObject.AddComponent<LogicLayerBehaviour>();

            for (int y = bounds.yMin; y < bounds.yMax; y++) {
                for (int x = bounds.xMin; x < bounds.xMax; x++) {
                    bool edge = y == bounds.yMin || y == bounds.yMax - 1
                             || (westWall && x == bounds.xMin)
                             || (eastWall && x == bounds.xMax - 1);
                    bool blocked = edge || block.Contains(new Vector2Int(x, y));
                    tilemap.SetTile(new Vector3Int(x, y, 0), blocked ? wall : ground);
                }
            }

            if (player != null) {
                var spawnObject = new GameObject("Player Spawn");
                spawnObject.transform.SetParent(root.transform, false);
                spawnObject.transform.position = new Vector3(spawnTile.x + 0.5f, spawnTile.y + 0.5f);

                var spawn = spawnObject.AddComponent<PlayerSpawnBehaviour>();
                var spawnProperties = new SerializedObject(spawn);
                spawnProperties.FindProperty("definition").objectReferenceValue = player;
                spawnProperties.FindProperty("facing").intValue = (int)Direction4.Enum.down;
                spawnProperties.ApplyModifiedPropertiesWithoutUndo();
            }

            // Saving bakes the area through the save hook.
            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/Area_{areaId}.unity");
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
