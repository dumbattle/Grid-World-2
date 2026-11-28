using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GridWorld {
    /// <summary>A class dropdown on the field's first line, then the chosen cutscene's own fields.</summary>
    [CustomPropertyDrawer(typeof(CutscenePickerAttribute))]
    public class CutscenePickerDrawer : PropertyDrawer {
        static List<Type> cutsceneTypes;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if (property.propertyType != SerializedPropertyType.ManagedReference) {
                EditorGUI.LabelField(position, label.text, "Needs [SerializeReference].");
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            // The button goes first, so it takes the click before the foldout under it.
            float labelWidth = EditorGUIUtility.labelWidth + 2;
            var button = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
            var current = property.managedReferenceValue;
            var shown = current == null ? "None" : ObjectNames.NicifyVariableName(current.GetType().Name);
            if (EditorGUI.DropdownButton(button, new GUIContent(shown), FocusType.Keyboard)) {
                ShowMenu(property, current);
            }

            EditorGUI.PropertyField(position, property, label, true);
            EditorGUI.EndProperty();
        }

        static void ShowMenu(SerializedProperty property, object current) {
            var serializedObject = property.serializedObject;
            var path = property.propertyPath;
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("None"), current == null, () => SetType(serializedObject, path, null));
            foreach (var type in GetCutsceneTypes()) {
                var chosen = type;
                bool selected = current != null && current.GetType() == type;
                menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(type.Name)), selected, () => SetType(serializedObject, path, chosen));
            }
            menu.ShowAsContext();
        }

        static void SetType(SerializedObject serializedObject, string path, Type type) {
            serializedObject.Update();
            var property = serializedObject.FindProperty(path);
            property.managedReferenceValue = type == null ? null : Activator.CreateInstance(type);
            serializedObject.ApplyModifiedProperties();
        }

        static List<Type> GetCutsceneTypes() {
            if (cutsceneTypes != null) {
                return cutsceneTypes;
            }

            cutsceneTypes = new List<Type>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<ICutscene>()) {
                bool usable = !type.IsAbstract
                           && !type.IsInterface
                           && !type.ContainsGenericParameters
                           && type.IsSerializable
                           && !typeof(UnityEngine.Object).IsAssignableFrom(type)
                           && type.GetConstructor(Type.EmptyTypes) != null;
                if (usable) {
                    cutsceneTypes.Add(type);
                }
            }
            cutsceneTypes.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return cutsceneTypes;
        }
    }
}
