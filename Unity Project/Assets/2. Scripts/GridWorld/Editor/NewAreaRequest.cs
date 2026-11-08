using UnityEngine;

namespace GridWorld {
    /// <summary>Everything the New Area window collects. File names have no extension.</summary>
    public class NewAreaRequest {
        public string id = "";
        public WorldDataSO world;
        public string sceneFolder = "Assets";
        public string dataFolder = "Assets";
        public string sceneName = "";
        public string dataName = "";
        public RectInt bounds;
        public bool openAdditively;

        public string scenePath => $"{sceneFolder}/{sceneName}.unity";
        public string dataPath => $"{dataFolder}/{dataName}.asset";
    }
}
