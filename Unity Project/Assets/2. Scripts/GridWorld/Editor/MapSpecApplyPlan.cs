using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace GridWorld {
    /// <summary>
    /// What applying a spec would do, from <see cref="MapSpecApplier.Prepare"/>. With no errors, the
    /// area's scene is open, so finish with <see cref="MapSpecApplier.Commit"/> or
    /// <see cref="MapSpecApplier.Cancel"/>, which closes it again if preparing opened it.
    /// </summary>
    public class MapSpecApplyPlan {
        public MapSpec spec;
        public MapLegend legend;
        public AreaBehaviour area;
        public Scene scene;
        public bool openedScene;
        public MapSpecChanges changes;
        public List<string> errors = new List<string>();
        public List<string> warnings = new List<string>();

        public bool hasChanges => changes != null && changes.changes.Count > 0;
    }
}
