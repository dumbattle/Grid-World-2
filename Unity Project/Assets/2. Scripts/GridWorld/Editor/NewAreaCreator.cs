using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace GridWorld {
    /// <summary>
    /// Creates an area scene and its data asset from a <see cref="NewAreaRequest"/>, then saves the
    /// scene so the bake adds it to the world.
    /// </summary>
    public static class NewAreaCreator {
        /// <summary>A message describing the first problem with the request, or null if it can be created.</summary>
        public static string FindError(NewAreaRequest request) {
            if (request.world == null) {
                return "Choose a world data asset.";
            }

            var error = NewAreaRules.FindError(request.id, request.bounds, request.world.areas);
            if (error != null) {
                return error;
            }

            if (request.openAdditively && HasUntitledScene()) {
                return "Unity can't add a scene next to an untitled one. Save it first, or turn off Open additively.";
            }

            error = FindFileError("Scene", request.sceneFolder, request.sceneName, request.scenePath);
            if (error != null) {
                return error;
            }
            return FindFileError("Area data", request.dataFolder, request.dataName, request.dataPath);
        }

        /// <summary>
        /// Returns the new scene's area, or null if the request was invalid. Without
        /// <see cref="NewAreaRequest.openAdditively"/>, open scenes close without saving, so ask first.
        /// </summary>
        public static AreaBehaviour Create(NewAreaRequest request) {
            var error = FindError(request);
            if (error != null) {
                Debug.LogError($"Can't create area: {error}");
                return null;
            }

            // Opening a scene in single mode unloads unreferenced assets, so the scene comes first,
            // and the world is reloaded from its path in case it was unloaded.
            var worldPath = AssetDatabase.GetAssetPath(request.world);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, request.openAdditively ? NewSceneMode.Additive : NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);

            var world = string.IsNullOrEmpty(worldPath) ? request.world : AssetDatabase.LoadAssetAtPath<WorldDataSO>(worldPath);
            var data = ScriptableObject.CreateInstance<AreaDataSO>();
            AssetDatabase.CreateAsset(data, request.dataPath);

            var root = new GameObject($"Area {request.id}");
            root.AddComponent<Grid>();
            var area = root.AddComponent<AreaBehaviour>();

            var properties = new SerializedObject(area);
            SetProperty(properties, "id").stringValue = request.id;
            SetProperty(properties, "areaData").objectReferenceValue = data;
            SetProperty(properties, "worldData").objectReferenceValue = world;
            SetProperty(properties, "tileBounds").rectIntValue = request.bounds;
            properties.ApplyModifiedPropertiesWithoutUndo();

            CreateTilemap(root, "Ground");
            var logic = CreateTilemap(root, "Logic");
            logic.AddComponent<LogicLayerBehaviour>();
            // Hidden so it doesn't draw over the art. Turn it on while painting.
            logic.GetComponent<TilemapRenderer>().enabled = false;

            // Saving bakes the area through the save hook, which adds it to the world.
            EditorSceneManager.SaveScene(scene, request.scenePath);
            return area;
        }

        static bool HasUntitledScene() {
            for (int i = 0; i < SceneManager.sceneCount; i++) {
                if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path)) {
                    return true;
                }
            }
            return false;
        }

        static string FindFileError(string label, string folder, string name, string path) {
            if (!AssetDatabase.IsValidFolder(folder)) {
                return $"{label} folder '{folder}' doesn't exist.";
            }
            if (string.IsNullOrWhiteSpace(name)) {
                return $"Enter a {label.ToLowerInvariant()} file name.";
            }
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) {
                return $"{label} file name '{name}' has characters that aren't allowed.";
            }
            if (File.Exists(path)) {
                return $"'{path}' already exists.";
            }
            return null;
        }

        static SerializedProperty SetProperty(SerializedObject properties, string name) {
            var property = properties.FindProperty(name);
            if (property == null) {
                throw new System.InvalidOperationException($"{properties.targetObject.GetType().Name} has no serialized field '{name}'. Update {nameof(NewAreaCreator)}.");
            }
            return property;
        }

        static GameObject CreateTilemap(GameObject root, string name) {
            var tilemapObject = new GameObject(name);
            tilemapObject.transform.SetParent(root.transform, false);
            tilemapObject.AddComponent<Tilemap>();
            tilemapObject.AddComponent<TilemapRenderer>();
            return tilemapObject;
        }
    }
}
