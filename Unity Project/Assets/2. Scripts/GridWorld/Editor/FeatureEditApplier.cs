using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GridWorld {
    /// <summary>
    /// Applies a feature edit file to its area's scene. Each edit is checked against the scene first,
    /// and if any conflicts, nothing is applied. Changes are one Undo step and the scene is left
    /// unsaved, so they can be checked before saving bakes them.
    /// </summary>
    public static class FeatureEditApplier {
        const string UndoName = "Apply Feature Edits";
        const string ScriptsFolder = "Assets/2. Scripts/";

        static readonly Regex arrayIndex = new Regex(@"\[(\d+)\]");

        /// <summary>
        /// Finds the area, opens its scene if it isn't open, and evaluates every edit. If the file has
        /// errors, any scene it opened is closed again.
        /// </summary>
        public static FeatureEditPlan Prepare(FeatureEditFile file) {
            var plan = new FeatureEditPlan { file = file };
            var world = AssetDatabase.LoadAssetAtPath<WorldDataSO>(file.world);
            if (world == null) {
                plan.errors.Add($"{file.source}: there's no world data asset at '{file.world}'.");
                return plan;
            }
            var data = MapSpecChecks.FindArea(world, file.areaId);
            if (data == null) {
                plan.errors.Add($"{file.source}: world '{world.name}' has no area '{file.areaId}'.");
                return plan;
            }
            if (string.IsNullOrEmpty(data.scenePath) || !File.Exists(data.scenePath)) {
                plan.errors.Add($"{file.source}: area '{file.areaId}' has no scene at '{data.scenePath}'. Open and save its scene to fix the path.");
                return plan;
            }

            plan.scene = SceneManager.GetSceneByPath(data.scenePath);
            if (!plan.scene.isLoaded) {
                if (NewAreaCreator.HasUntitledScene()) {
                    plan.errors.Add("Unity can't open the area's scene next to an untitled one. Save it first, or open the area's scene.");
                    return plan;
                }
                plan.scene = EditorSceneManager.OpenScene(data.scenePath, OpenSceneMode.Additive);
                plan.openedScene = true;
            }

            plan.area = MapSpecApplier.FindInScene(plan.scene, file.areaId);
            if (plan.area == null) {
                plan.errors.Add($"{data.scenePath} has no {nameof(AreaBehaviour)} with ID '{file.areaId}'.");
                Cancel(plan);
                return plan;
            }

            Evaluate(plan, FeatureIdChecks.InScene(plan.scene), plan.area.grid, plan.area.bounds);
            return plan;
        }

        /// <summary>
        /// Works out each edit's status against these features, in file order, and stages the changes.
        /// Later edits see earlier ones, so an array can grow and then have its new element set.
        /// </summary>
        /// <param name="bounds">The area's bounds in scene tiles, for warning about moves outside them.</param>
        public static void Evaluate(FeatureEditPlan plan, IReadOnlyList<IdentifiedFeatureBehaviour> features, Grid grid, RectInt bounds) {
            var component2serialized = new Dictionary<Object, SerializedObject>();
            var editedProperties = new HashSet<KeyValuePair<Object, string>>();

            foreach (var edit in plan.file.edits) {
                var result = new FeatureEditResult { edit = edit, status = FeatureEditStatus.Change };
                plan.results.Add(result);

                var matches = new List<IdentifiedFeatureBehaviour>();
                foreach (var feature in features) {
                    if (feature != null && FeatureIds.Same(feature.featureId, edit.id)) {
                        matches.Add(feature);
                    }
                }
                if (matches.Count == 0) {
                    if (edit.kind == FeatureEditKind.Remove) {
                        Already(result, $"there's no '{edit.id}', so it's already removed.");
                    }
                    else {
                        Conflict(result, $"there's no feature '{edit.id}' in area '{plan.file.areaId}'.");
                    }
                    continue;
                }
                if (matches.Count > 1) {
                    Conflict(result, $"{matches.Count} features have the ID '{edit.id}'. Fix them with Grid World → Fix Feature IDs.");
                    continue;
                }

                var target = matches[0];
                switch (edit.kind) {
                    case FeatureEditKind.Move:
                        EvaluateMove(plan, result, target, grid, bounds);
                        break;
                    case FeatureEditKind.Remove:
                        EvaluateRemove(plan, result, target, grid);
                        break;
                    case FeatureEditKind.Face:
                        EvaluateField(result, target, "facing", component2serialized, editedProperties);
                        break;
                    case FeatureEditKind.Set:
                        EvaluateSet(result, target, component2serialized, editedProperties);
                        break;
                }
            }

            foreach (var serialized in component2serialized.Values) {
                if (serialized.hasModifiedProperties) {
                    plan.serializedObjects.Add(serialized);
                }
            }
        }

        /// <summary>Makes the staged changes as one Undo step and marks the scenes dirty. Doesn't save.</summary>
        public static void Commit(FeatureEditPlan plan) {
            if (plan.hasConflicts) {
                throw new System.InvalidOperationException("A plan with conflicts can't be committed.");
            }
            if (plan.committed) {
                throw new System.InvalidOperationException("This plan has already been committed.");
            }
            plan.committed = true;

            var scenes = new HashSet<Scene>();
            // A group of its own, so one Undo reverts exactly this file.
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            int group = Undo.GetCurrentGroup();
            foreach (var serialized in plan.serializedObjects) {
                scenes.Add(((Component)serialized.targetObject).gameObject.scene);
                serialized.ApplyModifiedProperties();
            }
            foreach (var move in plan.moves) {
                scenes.Add(move.Key.gameObject.scene);
                Undo.RecordObject(move.Key, UndoName);
                move.Key.position = move.Value;
            }
            foreach (var removal in plan.removals) {
                scenes.Add(removal.scene);
                Undo.DestroyObjectImmediate(removal);
            }
            Undo.CollapseUndoOperations(group);

            foreach (var scene in scenes) {
                if (scene.IsValid()) {
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            }
            FeatureIdIndex.Invalidate();
        }

        /// <summary>Drops the staged changes, and closes the area's scene if <see cref="Prepare"/> opened it.</summary>
        public static void Cancel(FeatureEditPlan plan) {
            if (plan.openedScene && plan.scene.isLoaded) {
                EditorSceneManager.CloseScene(plan.scene, true);
            }
            plan.openedScene = false;
        }

        static void EvaluateMove(FeatureEditPlan plan, FeatureEditResult result, IdentifiedFeatureBehaviour target, Grid grid, RectInt bounds) {
            var edit = result.edit;
            var transform = target.transform;
            var current = Tile(grid, transform.position);
            var from = Tile(edit.from);
            var to = Tile(edit.to);
            if (current == to) {
                Already(result, $"'{edit.id}' is already at {Text(to)}.");
                return;
            }
            if (current != from) {
                Conflict(result, $"'{edit.id}' is at {Text(current)}, not {Text(from)}.");
                return;
            }

            // The tile's centre, keeping the object's depth.
            var center = grid.GetCellCenterWorld(new Vector3Int(to.x, to.y, 0));
            plan.moves.Add(new KeyValuePair<Transform, Vector3>(transform, new Vector3(center.x, center.y, transform.position.z)));

            if (!bounds.Contains(to)) {
                result.warnings.Add($"{Text(to)} is outside area '{plan.file.areaId}', whose bounds are {bounds.x} {bounds.y} {bounds.width} {bounds.height}.");
            }
            foreach (var behaviour in target.GetComponents<MonoBehaviour>()) {
                if (behaviour == null) {
                    continue;
                }
                var regionSpace = new SerializedObject(behaviour).FindProperty("regionSpace");
                if (regionSpace != null && regionSpace.propertyType == SerializedPropertyType.Enum && regionSpace.enumValueIndex == (int)TileSpace.Area) {
                    result.warnings.Add($"{behaviour.GetType().Name}'s region is in area tiles, so it stays where it is. Check '{edit.id}' is still inside it.");
                }
            }
            if (target.GetComponent<PatrolAIBehaviour>() != null) {
                result.warnings.Add($"'{edit.id}' patrols, and its route is relative to it, so the whole route moves too.");
            }
        }

        static void EvaluateRemove(FeatureEditPlan plan, FeatureEditResult result, IdentifiedFeatureBehaviour target, Grid grid) {
            var edit = result.edit;
            var gameObject = target.gameObject;
            var current = Tile(grid, target.transform.position);
            var at = Tile(edit.from);
            if (current != at) {
                Conflict(result, $"'{edit.id}' is at {Text(current)}, not {Text(at)}.");
                return;
            }
            if (target.transform.childCount > 0) {
                Conflict(result, $"'{gameObject.name}' has child objects, which removing would delete too. Remove it by hand.");
                return;
            }
            if (PrefabUtility.IsPartOfPrefabInstance(gameObject)) {
                Conflict(result, $"'{gameObject.name}' is part of a prefab instance. Remove it by hand.");
                return;
            }
            if (gameObject.GetComponents<IdentifiedFeatureBehaviour>().Length > 1) {
                Conflict(result, $"'{gameObject.name}' holds other features too, which removing would delete. Remove it by hand.");
                return;
            }
            plan.removals.Add(gameObject);
        }

        static void EvaluateSet(FeatureEditResult result, IdentifiedFeatureBehaviour target, Dictionary<Object, SerializedObject> component2serialized, HashSet<KeyValuePair<Object, string>> editedProperties) {
            var edit = result.edit;
            int dot = edit.path.IndexOf('.');
            var typeName = edit.path.Substring(0, dot);
            var fieldPath = edit.path.Substring(dot + 1);

            MonoBehaviour component = null;
            int found = 0;
            foreach (var behaviour in target.GetComponents<MonoBehaviour>()) {
                if (behaviour != null && behaviour.GetType().Name == typeName && IsProjectScript(behaviour)) {
                    component = behaviour;
                    found++;
                }
            }
            if (found == 0) {
                Conflict(result, $"'{edit.id}' has no {typeName}.");
                return;
            }
            if (found > 1) {
                Conflict(result, $"'{edit.id}' has {found} {typeName} components, so the field is ambiguous.");
                return;
            }
            if (component == target && fieldPath == "id") {
                Conflict(result, "rename features with Grid World → Fix Feature IDs, which checks the new ID.");
                return;
            }
            EvaluateProperty(result, component, fieldPath, component2serialized, editedProperties);
        }

        static void EvaluateField(FeatureEditResult result, IdentifiedFeatureBehaviour target, string fieldPath, Dictionary<Object, SerializedObject> component2serialized, HashSet<KeyValuePair<Object, string>> editedProperties) {
            if (result.edit.kind == FeatureEditKind.Face && result.edit.to[0].Equals("none", System.StringComparison.OrdinalIgnoreCase)) {
                Conflict(result, "a feature must face up, right, down, or left.");
                return;
            }
            EvaluateProperty(result, target, fieldPath, component2serialized, editedProperties);
        }

        static void EvaluateProperty(FeatureEditResult result, MonoBehaviour component, string fieldPath, Dictionary<Object, SerializedObject> component2serialized, HashSet<KeyValuePair<Object, string>> editedProperties) {
            var edit = result.edit;
            if (!component2serialized.TryGetValue(component, out var serialized)) {
                serialized = new SerializedObject(component);
                component2serialized.Add(component, serialized);
            }

            var property = FindProperty(serialized, fieldPath, out string pathError);
            if (property == null) {
                Conflict(result, $"{component.GetType().Name} {pathError}");
                return;
            }
            if (!editedProperties.Add(new KeyValuePair<Object, string>(component, property.propertyPath))) {
                Conflict(result, $"another edit in the file already changes {component.GetType().Name}.{fieldPath}.");
                return;
            }

            var current = FeatureEditValues.Describe(property);
            var from = FeatureEditValues.Normalize(property, edit.from, out string fromError);
            var to = FeatureEditValues.Normalize(property, edit.to, out string toError);
            if (current == null || from == null || to == null) {
                Conflict(result, $"{component.GetType().Name}.{fieldPath}: {fromError ?? toError ?? $"fields of type {property.propertyType} can't be edited from text."}");
                return;
            }
            if (current == to) {
                Already(result, $"{component.GetType().Name}.{fieldPath} is already {Quote(to)}.");
                return;
            }
            if (current != from) {
                Conflict(result, $"{component.GetType().Name}.{fieldPath} is {Quote(current)}, not {Quote(from)}.");
                return;
            }

            var writeError = FeatureEditValues.Write(property, edit.to);
            if (writeError != null) {
                Conflict(result, $"{component.GetType().Name}.{fieldPath}: {writeError}");
            }
        }

        // Turns mapread's paths, points[1].tile and points.size, into Unity's property paths.
        static SerializedProperty FindProperty(SerializedObject serialized, string fieldPath, out string error) {
            error = null;
            if (fieldPath.StartsWith("m_")) {
                error = $"field '{fieldPath}' belongs to Unity, and edits only change the component's own fields.";
                return null;
            }

            var unityPath = arrayIndex.Replace(fieldPath, ".Array.data[$1]");
            SerializedProperty property = null;
            if (unityPath.EndsWith(".size")) {
                var list = serialized.FindProperty(unityPath.Substring(0, unityPath.Length - ".size".Length));
                if (list != null && list.isArray && list.propertyType != SerializedPropertyType.String) {
                    property = serialized.FindProperty(list.propertyPath + ".Array.size");
                }
            }
            if (property == null) {
                property = serialized.FindProperty(unityPath);
            }

            if (property == null) {
                error = $"has no field '{fieldPath}'. Check the name, and that a list element isn't past the list's size.";
                return null;
            }
            if (property.isArray && property.propertyType != SerializedPropertyType.String) {
                error = $"field '{fieldPath}' is a list. Set '{fieldPath}.size', or an element like '{fieldPath}[0]'.";
                return null;
            }
            if (property.propertyType == SerializedPropertyType.Generic) {
                error = $"field '{fieldPath}' has fields of its own. Set one of them, like '{fieldPath}.<field>'.";
                return null;
            }
            return property;
        }

        static bool IsProjectScript(MonoBehaviour behaviour) {
            var script = MonoScript.FromMonoBehaviour(behaviour);
            return script != null && AssetDatabase.GetAssetPath(script).StartsWith(ScriptsFolder);
        }

        static void Already(FeatureEditResult result, string message) {
            result.status = FeatureEditStatus.AlreadyApplied;
            result.message = message;
        }

        static void Conflict(FeatureEditResult result, string message) {
            result.status = FeatureEditStatus.Conflict;
            result.message = message;
            result.warnings.Clear();
        }

        static Vector2Int Tile(Grid grid, Vector3 position) {
            var cell = grid.WorldToCell(position);
            return new Vector2Int(cell.x, cell.y);
        }

        static Vector2Int Tile(string[] tokens) {
            return new Vector2Int(int.Parse(tokens[0]), int.Parse(tokens[1]));
        }

        static string Text(Vector2Int tile) {
            return $"{tile.x} {tile.y}";
        }

        static string Quote(string value) {
            return value.Length == 0 || value.IndexOf(' ') >= 0 ? $"\"{value}\"" : value;
        }
    }
}
