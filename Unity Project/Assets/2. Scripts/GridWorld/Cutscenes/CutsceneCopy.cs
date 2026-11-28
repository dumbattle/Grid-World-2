using UnityEngine;

namespace GridWorld {
    // Bakes give each record its own copy, so the scene's object and the baked asset never share
    // one instance. Only plain serialized fields are copied.
    static class CutsceneCopy {
        public static ICutscene Of(ICutscene cutscene) {
            if (cutscene == null) {
                return null;
            }
            return (ICutscene)JsonUtility.FromJson(JsonUtility.ToJson(cutscene), cutscene.GetType());
        }
    }
}
