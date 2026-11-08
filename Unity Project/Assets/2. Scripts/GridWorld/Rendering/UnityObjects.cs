using UnityEngine;

namespace GridWorld {
    static class UnityObjects {
        // Destroy isn't allowed in edit mode, which EditMode tests run in.
        public static void Destroy(Object obj) {
            if (obj == null) {
                return;
            }
            if (Application.isPlaying) {
                Object.Destroy(obj);
            }
            else {
                Object.DestroyImmediate(obj);
            }
        }
    }
}
