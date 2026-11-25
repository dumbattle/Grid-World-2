using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Reads and writes a serialized field as edit-file tokens, in the same text that
    /// <c>mapread.py features</c> prints: numbers separated by spaces, enums by name, true or false,
    /// and asset references by path or "none".
    /// </summary>
    public static class FeatureEditValues {
        public const string None = "none";

        static readonly CultureInfo invariant = CultureInfo.InvariantCulture;

        /// <summary>The property's value as one line of text, or null for a type edits don't support.</summary>
        public static string Describe(SerializedProperty property) {
            switch (property.propertyType) {
                case SerializedPropertyType.Integer:
                    return property.longValue.ToString(invariant);
                case SerializedPropertyType.ArraySize:
                    return property.intValue.ToString(invariant);
                case SerializedPropertyType.Boolean:
                    return property.boolValue ? "true" : "false";
                case SerializedPropertyType.Float:
                    return Format(property.floatValue);
                case SerializedPropertyType.String:
                    return property.stringValue;
                case SerializedPropertyType.Enum:
                    int index = property.enumValueIndex;
                    return index >= 0 && index < property.enumNames.Length ? property.enumNames[index] : property.intValue.ToString(invariant);
                case SerializedPropertyType.Vector2Int:
                    var v2i = property.vector2IntValue;
                    return $"{v2i.x} {v2i.y}";
                case SerializedPropertyType.Vector3Int:
                    var v3i = property.vector3IntValue;
                    return $"{v3i.x} {v3i.y} {v3i.z}";
                case SerializedPropertyType.Vector2:
                    var v2 = property.vector2Value;
                    return $"{Format(v2.x)} {Format(v2.y)}";
                case SerializedPropertyType.Vector3:
                    var v3 = property.vector3Value;
                    return $"{Format(v3.x)} {Format(v3.y)} {Format(v3.z)}";
                case SerializedPropertyType.RectInt:
                    var ri = property.rectIntValue;
                    return $"{ri.x} {ri.y} {ri.width} {ri.height}";
                case SerializedPropertyType.Rect:
                    var r = property.rectValue;
                    return $"{Format(r.x)} {Format(r.y)} {Format(r.width)} {Format(r.height)}";
                case SerializedPropertyType.ObjectReference:
                    var value = property.objectReferenceValue;
                    if (value == null) {
                        return None;
                    }
                    var path = AssetDatabase.GetAssetPath(value);
                    return string.IsNullOrEmpty(path) ? $"scene object '{value.name}'" : path;
                default:
                    return null;
            }
        }

        /// <summary>
        /// The tokens as the text <see cref="Describe"/> would give for that value, so the two can be
        /// compared. Returns null and sets <paramref name="error"/> if they don't fit the property's type.
        /// </summary>
        public static string Normalize(SerializedProperty property, string[] tokens, out string error) {
            error = null;
            switch (property.propertyType) {
                case SerializedPropertyType.Integer:
                    return Longs(tokens, 1, out error);
                case SerializedPropertyType.ArraySize:
                    var size = Longs(tokens, 1, out error);
                    if (size != null && size.StartsWith("-")) {
                        error = "an array size can't be negative.";
                        return null;
                    }
                    return size;
                case SerializedPropertyType.Boolean:
                    if (tokens.Length == 1 && (tokens[0].Equals("true", StringComparison.OrdinalIgnoreCase) || tokens[0].Equals("false", StringComparison.OrdinalIgnoreCase))) {
                        return tokens[0].ToLowerInvariant();
                    }
                    error = "expected true or false.";
                    return null;
                case SerializedPropertyType.Float:
                    return Floats(tokens, 1, out error);
                case SerializedPropertyType.String:
                    if (tokens.Length != 1) {
                        error = "expected one value. Put text with spaces in \"double quotes\".";
                        return null;
                    }
                    return tokens[0];
                case SerializedPropertyType.Enum:
                    if (tokens.Length == 1) {
                        foreach (var name in property.enumNames) {
                            if (name.Equals(tokens[0], StringComparison.OrdinalIgnoreCase)) {
                                return name;
                            }
                        }
                    }
                    error = $"expected one of: {string.Join(", ", property.enumNames)}.";
                    return null;
                case SerializedPropertyType.Vector2Int:
                    return Longs(tokens, 2, out error);
                case SerializedPropertyType.Vector3Int:
                    return Longs(tokens, 3, out error);
                case SerializedPropertyType.RectInt:
                    return Longs(tokens, 4, out error);
                case SerializedPropertyType.Vector2:
                    return Floats(tokens, 2, out error);
                case SerializedPropertyType.Vector3:
                    return Floats(tokens, 3, out error);
                case SerializedPropertyType.Rect:
                    return Floats(tokens, 4, out error);
                case SerializedPropertyType.ObjectReference:
                    if (tokens.Length != 1) {
                        error = "expected an asset path or none. Put a path with spaces in \"double quotes\".";
                        return null;
                    }
                    if (tokens[0].Equals(None, StringComparison.OrdinalIgnoreCase)) {
                        return None;
                    }
                    var path = tokens[0].Replace('\\', '/');
                    if (AssetDatabase.LoadMainAssetAtPath(path) == null) {
                        error = $"there's no asset at '{path}'.";
                        return null;
                    }
                    return path;
                default:
                    error = $"fields of type {property.propertyType} can't be edited from text.";
                    return null;
            }
        }

        /// <summary>
        /// Writes the tokens into the property without applying its SerializedObject. Returns an error,
        /// or null. Call <see cref="Normalize"/> first, so the tokens are known to fit.
        /// </summary>
        public static string Write(SerializedProperty property, string[] tokens) {
            var text = Normalize(property, tokens, out string error);
            if (text == null) {
                return error;
            }
            var parts = text.Split(' ');
            switch (property.propertyType) {
                case SerializedPropertyType.Integer:
                    property.longValue = long.Parse(text, invariant);
                    break;
                case SerializedPropertyType.ArraySize:
                    property.intValue = int.Parse(text, invariant);
                    break;
                case SerializedPropertyType.Boolean:
                    property.boolValue = text == "true";
                    break;
                case SerializedPropertyType.Float:
                    property.floatValue = float.Parse(text, invariant);
                    break;
                case SerializedPropertyType.String:
                    property.stringValue = text;
                    break;
                case SerializedPropertyType.Enum:
                    property.enumValueIndex = Array.IndexOf(property.enumNames, text);
                    break;
                case SerializedPropertyType.Vector2Int:
                    property.vector2IntValue = new Vector2Int(Int(parts[0]), Int(parts[1]));
                    break;
                case SerializedPropertyType.Vector3Int:
                    property.vector3IntValue = new Vector3Int(Int(parts[0]), Int(parts[1]), Int(parts[2]));
                    break;
                case SerializedPropertyType.RectInt:
                    property.rectIntValue = new RectInt(Int(parts[0]), Int(parts[1]), Int(parts[2]), Int(parts[3]));
                    break;
                case SerializedPropertyType.Vector2:
                    property.vector2Value = new Vector2(Float(parts[0]), Float(parts[1]));
                    break;
                case SerializedPropertyType.Vector3:
                    property.vector3Value = new Vector3(Float(parts[0]), Float(parts[1]), Float(parts[2]));
                    break;
                case SerializedPropertyType.Rect:
                    property.rectValue = new Rect(Float(parts[0]), Float(parts[1]), Float(parts[2]), Float(parts[3]));
                    break;
                case SerializedPropertyType.ObjectReference:
                    var asset = text == None ? null : AssetDatabase.LoadMainAssetAtPath(text);
                    property.objectReferenceValue = asset;
                    // Unity silently refuses an asset of the wrong type.
                    if (property.objectReferenceValue != asset) {
                        return $"'{text}' isn't a type this field can hold.";
                    }
                    break;
            }
            return null;
        }

        static string Longs(string[] tokens, int count, out string error) {
            error = null;
            if (tokens.Length != count) {
                error = count == 1 ? "expected one whole number." : $"expected {count} whole numbers.";
                return null;
            }
            var texts = new string[count];
            for (int i = 0; i < count; i++) {
                if (!int.TryParse(tokens[i], NumberStyles.Integer, invariant, out int value)) {
                    error = $"'{tokens[i]}' isn't a whole number.";
                    return null;
                }
                texts[i] = value.ToString(invariant);
            }
            return string.Join(" ", texts);
        }

        static string Floats(string[] tokens, int count, out string error) {
            error = null;
            if (tokens.Length != count) {
                error = count == 1 ? "expected one number." : $"expected {count} numbers.";
                return null;
            }
            var texts = new string[count];
            for (int i = 0; i < count; i++) {
                if (!float.TryParse(tokens[i], NumberStyles.Float, invariant, out float value)) {
                    error = $"'{tokens[i]}' isn't a number.";
                    return null;
                }
                texts[i] = Format(value);
            }
            return string.Join(" ", texts);
        }

        static string Format(float value) {
            return value.ToString("R", invariant);
        }

        static int Int(string text) {
            return int.Parse(text, invariant);
        }

        static float Float(string text) {
            return float.Parse(text, invariant);
        }
    }
}
