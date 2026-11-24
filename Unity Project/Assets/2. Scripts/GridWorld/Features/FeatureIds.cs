using System;

namespace GridWorld {
    /// <summary>
    /// Rules for feature IDs. An ID is unique across the world, ignoring case, and uses only ASCII
    /// letters, digits, '_' and '-'.
    /// </summary>
    public static class FeatureIds {
        public static readonly StringComparer comparer = StringComparer.OrdinalIgnoreCase;

        public static bool Same(string a, string b) {
            return comparer.Equals(a, b);
        }

        /// <summary>Describes what's wrong with the ID's format, or returns null if it's valid.</summary>
        public static string FindFormatError(string id) {
            if (string.IsNullOrEmpty(id)) {
                return "The ID is empty.";
            }
            foreach (char c in id) {
                bool allowed = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == '-';
                if (!allowed) {
                    return $"'{c}' isn't allowed in an ID. Use letters, digits, '_' and '-'.";
                }
            }
            return null;
        }
    }
}
