using System.IO;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// The <c>Maps/</c> folder beside the Unity project, holding legends, specs, and previews. It's
    /// outside <c>Assets/</c> so Unity doesn't import it.
    /// </summary>
    public static class MapPaths {
        public static string root => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Maps"));
        public static string legends => Path.Combine(root, "legends");
        public static string areas => Path.Combine(root, "areas");
        public static string previews => Path.Combine(root, "previews");
        public static string edits => Path.Combine(root, "edits");

        public static string Legend(string name) {
            return Path.Combine(legends, name + ".json");
        }
    }
}
