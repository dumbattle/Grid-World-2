using System;
using System.Collections.Generic;

namespace GridWorld {
    /// <summary>
    /// Build functions for cutscenes, keyed by ID and compared ignoring case. The root fills it before
    /// the world loads, and a cutscene is built fresh from its function each time it starts.
    /// </summary>
    public class CutsceneCatalogue {
        Dictionary<string, Action<CutsceneBuilder, CutsceneStart>> id2build
            = new Dictionary<string, Action<CutsceneBuilder, CutsceneStart>>(StringComparer.OrdinalIgnoreCase);

        public int count => id2build.Count;

        public void Register(string id, Action<CutsceneBuilder, CutsceneStart> build) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentException("A cutscene needs an ID.", nameof(id));
            }
            if (build == null) {
                throw new ArgumentNullException(nameof(build));
            }
            if (id2build.ContainsKey(id)) {
                throw new InvalidOperationException($"Cutscene '{id}' is already registered.");
            }
            id2build.Add(id, build);
        }

        public bool Has(string id) {
            return id != null && id2build.ContainsKey(id);
        }

        public bool TryGet(string id, out Action<CutsceneBuilder, CutsceneStart> build) {
            if (id == null) {
                build = null;
                return false;
            }
            return id2build.TryGetValue(id, out build);
        }
    }
}
