using UnityEditor;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Draws a component's default Inspector with its region's <see cref="TileSpace"/> as a
    /// dropdown. Changing the space converts the region, so it stays where it is in the scene.
    /// </summary>
    static class RegionSpaceInspector {
        public static void Draw(SerializedObject serializedObject, string spaceName, string regionName, string relativeLabel) {
            serializedObject.Update();

            TileSpace? chosen = null;
            var property = serializedObject.GetIterator();
            for (bool enter = true; property.NextVisible(enter); enter = false) {
                if (property.name == spaceName) {
                    chosen = DrawSpace(property, relativeLabel);
                    continue;
                }
                using (new EditorGUI.DisabledScope(property.name == "m_Script")) {
                    EditorGUILayout.PropertyField(property, true);
                }
            }

            serializedObject.ApplyModifiedProperties();
            if (chosen.HasValue) {
                Convert(serializedObject, spaceName, regionName, chosen.Value);
            }
        }

        // Returns the new space if it was changed, without applying it.
        static TileSpace? DrawSpace(SerializedProperty space, string relativeLabel) {
            var labels = new[] { new GUIContent("Area tiles"), new GUIContent(relativeLabel) };
            var label = new GUIContent(space.displayName, space.tooltip);

            EditorGUI.showMixedValue = space.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            int chosen = EditorGUILayout.Popup(label, space.enumValueIndex, labels);
            EditorGUI.showMixedValue = false;
            return EditorGUI.EndChangeCheck() ? (TileSpace)chosen : (TileSpace?)null;
        }

        // Each object converts from its own tile. Applying through SerializedObject records Undo.
        static void Convert(SerializedObject serializedObject, string spaceName, string regionName, TileSpace to) {
            foreach (var target in serializedObject.targetObjects) {
                var single = new SerializedObject(target);
                var space = single.FindProperty(spaceName);
                var region = single.FindProperty(regionName);
                var from = (TileSpace)space.enumValueIndex;
                var origin = TileSpaces.TileOf(((Component)target).transform);

                region.rectIntValue = TileSpaces.Convert(region.rectIntValue, from, to, origin);
                space.enumValueIndex = (int)to;
                single.ApplyModifiedProperties();
            }
            serializedObject.Update();
        }
    }
}
