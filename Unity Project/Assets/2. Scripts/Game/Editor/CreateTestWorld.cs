using System.IO;
using GridWorld;
using LPE;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Game {
    /// <summary>
    /// <b>Grid World → Test → Create Test World</b>: builds two touching placeholder areas, a main
    /// scene, and their data assets, then bakes them. Everything goes in Test folders, named with a
    /// <c>TEST_</c> prefix, and is replaced on each run. Runs without dialogs in batch mode.
    /// </summary>
    public static class CreateTestWorld {
        const string ScenesFolder = "Assets/1. Scenes/Test";
        const string SpritesFolder = "Assets/3. Sprites/Test";
        const string TilesFolder = "Assets/6. Tiles/Test";
        const string DataFolder = "Assets/8. Data/Test";
        const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";
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
                    "Create Test World", "This replaces the existing test world in the Test folders.", "Replace", "Cancel")) {
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

            var groundA = CreateTile("TEST_GroundA", new Color32(92, 160, 72, 255), new Color32(80, 140, 62, 255));
            var groundB = CreateTile("TEST_GroundB", new Color32(164, 168, 80, 255), new Color32(146, 150, 68, 255));
            var wall = CreateTile("TEST_Wall", new Color32(70, 66, 80, 255), new Color32(110, 104, 122, 255));

            var table = CreateAsset<TilePropertyTableSO>("TEST_TilePropertyTable");
            table.SetEntry(groundA, TileBits.EnterAll);
            table.SetEntry(groundB, TileBits.EnterAll);
            table.SetEntry(wall, 0);
            EditorUtility.SetDirty(table);

            var settings = CreateAsset<WorldSettingsSO>("TEST_WorldSettings");
            CreateAsset<EntityDefinitionSO>("TEST_Player");
            CreateDefinition("TEST_Villager", new Color32(88, 148, 232, 255), 16);
            CreateDefinition("TEST_Guard", new Color32(168, 96, 216, 255), 20);

            var world = CreateAsset<WorldDataSO>("TEST_WorldData");
            var worldProperties = new SerializedObject(world);
            worldProperties.FindProperty("worldSettings").objectReferenceValue = settings;
            worldProperties.FindProperty("tilePropertyTable").objectReferenceValue = table;
            worldProperties.ApplyModifiedPropertiesWithoutUndo();

            CreateAsset<AreaDataSO>("TEST_AreaA");
            CreateAsset<AreaDataSO>("TEST_AreaB");
            AssetDatabase.SaveAssets();

            // Area scenes are created additively, which needs a saved scene already open.
            var mainPath = $"{ScenesFolder}/TEST_Main.unity";
            CreateMainScene(mainPath);
            BuildSceneList.Add(mainPath);

            // Opening the main scene as Single unloaded the assets created above, so load them again.
            world = Load<WorldDataSO>(DataFolder, "TEST_WorldData");
            var player = Load<EntityDefinitionSO>(DataFolder, "TEST_Player");
            var villager = Load<EntityDefinitionSO>(DataFolder, "TEST_Villager");
            var guard = Load<EntityDefinitionSO>(DataFolder, "TEST_Guard");
            var dataA = Load<AreaDataSO>(DataFolder, "TEST_AreaA");
            var dataB = Load<AreaDataSO>(DataFolder, "TEST_AreaB");
            var groundTileA = Load<TileBase>(TilesFolder, "TEST_GroundA");
            var groundTileB = Load<TileBase>(TilesFolder, "TEST_GroundB");
            var wallTile = Load<TileBase>(TilesFolder, "TEST_Wall");

            var boundsA = new RectInt(0, 0, AreaWidth, AreaHeight);
            var boundsB = new RectInt(AreaWidth, 0, AreaWidth, AreaHeight);

            // Walls on the outer edges only, so the two areas join along their shared border.
            CreateAreaScene("TEST_a", "a", boundsA, dataA, world, groundTileA, wallTile, westWall: true, eastWall: false,
                block: new RectInt(8, 6, 3, 3), player: player, addNpcs: root => {
                    AddSpinner(root, villager, new Vector2Int(3, 10));
                    AddWanderer(root, villager, new Vector2Int(15, 4), new RectInt(12, 2, 7, 5));
                    AddPatroller(root, guard, PatrolMode.PingPong,
                        new PatrolPointSettings(new Vector2Int(12, 12), Direction4.Enum.left, 60),
                        new PatrolPointSettings(new Vector2Int(20, 12), Direction4.Enum.right, 60));
                });
            CreateAreaScene("TEST_b", "b", boundsB, dataB, world, groundTileB, wallTile, westWall: false, eastWall: true,
                block: new RectInt(AreaWidth + 8, 9, 4, 2), player: null, addNpcs: root => {
                    AddPatroller(root, guard, PatrolMode.Loop,
                        new PatrolPointSettings(new Vector2Int(AreaWidth + 4, 4), Direction4.Enum.none, 0),
                        new PatrolPointSettings(new Vector2Int(AreaWidth + 16, 4), Direction4.Enum.up, 30),
                        new PatrolPointSettings(new Vector2Int(AreaWidth + 16, 7), Direction4.Enum.none, 0),
                        new PatrolPointSettings(new Vector2Int(AreaWidth + 4, 7), Direction4.Enum.down, 30));
                    AddWanderer(root, villager, new Vector2Int(AreaWidth + 14, 12), new RectInt(AreaWidth + 12, 11, 8, 3));
                });

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

        static void CreateDefinition(string name, Color32 color, int walkTicks) {
            var definition = CreateAsset<EntityDefinitionSO>(name);
            var properties = new SerializedObject(definition);
            properties.FindProperty("placeholderColor").colorValue = color;
            properties.FindProperty("walkTicksPerTile").intValue = walkTicks;
            properties.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject AddNpc(Transform root, string name, EntityDefinitionSO definition, Vector2Int tile) {
            var npcObject = new GameObject(name);
            npcObject.transform.SetParent(root, false);
            npcObject.transform.position = new Vector3(tile.x + 0.5f, tile.y + 0.5f);

            var npc = npcObject.AddComponent<NpcBehaviour>();
            var properties = new SerializedObject(npc);
            properties.FindProperty("definition").objectReferenceValue = definition;
            properties.FindProperty("facing").intValue = (int)Direction4.Enum.down;
            properties.ApplyModifiedPropertiesWithoutUndo();
            return npcObject;
        }

        static void AddSpinner(Transform root, EntityDefinitionSO definition, Vector2Int tile) {
            AddNpc(root, "TEST_Spinner", definition, tile).AddComponent<SpinAIBehaviour>();
        }

        static void AddWanderer(Transform root, EntityDefinitionSO definition, Vector2Int tile, RectInt region) {
            var wander = AddNpc(root, "TEST_Wanderer", definition, tile).AddComponent<WanderAIBehaviour>();
            var properties = new SerializedObject(wander);
            properties.FindProperty("region").rectIntValue = region;
            properties.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddPatroller(Transform root, EntityDefinitionSO definition, PatrolMode mode, params PatrolPointSettings[] points) {
            var patrol = AddNpc(root, $"TEST_Patroller ({mode})", definition, points[0].localTile).AddComponent<PatrolAIBehaviour>();
            var properties = new SerializedObject(patrol);
            var array = properties.FindProperty("points");
            array.arraySize = points.Length;
            for (int i = 0; i < points.Length; i++) {
                array.GetArrayElementAtIndex(i).boxedValue = points[i];
            }
            properties.FindProperty("mode").enumValueIndex = (int)mode;
            properties.ApplyModifiedPropertiesWithoutUndo();
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

        static void CreateMainScene(string path) {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Opening a scene as Single unloads the assets created before it, so load them after.
            var settings = Load<WorldSettingsSO>(DataFolder, "TEST_WorldSettings");
            var world = Load<WorldDataSO>(DataFolder, "TEST_WorldData");

            var cameraObject = new GameObject("TEST_Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(spawnTile.x + 0.5f, spawnTile.y + 0.5f, -10);

            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = settings.viewTiles.y / 2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(24, 24, 32, 255);

            var rootObject = new GameObject("TEST_World Root");
            var root = rootObject.AddComponent<WorldRootBehaviour>();
            var rootProperties = new SerializedObject(root);
            rootProperties.FindProperty("worldData").objectReferenceValue = world;
            rootProperties.FindProperty("viewCamera").objectReferenceValue = camera;
            rootProperties.FindProperty("moveAction").objectReferenceValue = FindAction("Move");
            rootProperties.FindProperty("runAction").objectReferenceValue = FindAction("Sprint");
            rootProperties.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, path);
        }

        static InputActionReference FindAction(string name) {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(InputActionsPath)) {
                if (asset is InputActionReference reference && reference.action != null
                        && reference.action.actionMap.name == "Player" && reference.action.name == name) {
                    return reference;
                }
            }
            Debug.LogError($"Grid World: no Player/{name} action in {InputActionsPath}.");
            return null;
        }

        static void CreateAreaScene(string areaId, string suffix, RectInt bounds, AreaDataSO data, WorldDataSO world, TileBase ground, TileBase wall,
                                    bool westWall, bool eastWall, RectInt block, EntityDefinitionSO player,
                                    System.Action<Transform> addNpcs) {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);

            var root = new GameObject($"TEST_Area {suffix}");
            root.AddComponent<Grid>();
            var area = root.AddComponent<AreaBehaviour>();

            var areaProperties = new SerializedObject(area);
            areaProperties.FindProperty("id").stringValue = areaId;
            areaProperties.FindProperty("areaData").objectReferenceValue = data;
            areaProperties.FindProperty("worldData").objectReferenceValue = world;
            areaProperties.FindProperty("tileBounds").rectIntValue = bounds;
            areaProperties.ApplyModifiedPropertiesWithoutUndo();

            var groundObject = new GameObject("TEST_Ground");
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
                var spawnObject = new GameObject("TEST_Player Spawn");
                spawnObject.transform.SetParent(root.transform, false);
                spawnObject.transform.position = new Vector3(spawnTile.x + 0.5f, spawnTile.y + 0.5f);

                var spawn = spawnObject.AddComponent<PlayerSpawnBehaviour>();
                var spawnProperties = new SerializedObject(spawn);
                spawnProperties.FindProperty("definition").objectReferenceValue = player;
                spawnProperties.FindProperty("facing").intValue = (int)Direction4.Enum.down;
                spawnProperties.ApplyModifiedPropertiesWithoutUndo();
            }

            addNpcs(root.transform);

            // Saving bakes the area through the save hook.
            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/TEST_Area_{suffix}.unity");
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
