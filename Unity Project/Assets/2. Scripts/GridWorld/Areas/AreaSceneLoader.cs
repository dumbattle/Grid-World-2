using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GridWorld {
    /// <summary>
    /// Loads and unloads area scenes additively to follow the areas' active flags. Runs outside the
    /// phases and never changes world state, so a late load only makes visuals appear late. Logs each
    /// load and unload with how long it took.
    /// </summary>
    public class AreaSceneLoader {
        enum SceneState { Unloaded, Loading, Loaded, Unloading }

        class AreaScene {
            public string id;
            public string path;
            public SceneState state;
            public float startTime;
        }

        // Indexed like the world's areas.
        List<AreaScene> scenes = new List<AreaScene>();

        public AreaSceneLoader(RenderContext ctx, IReadOnlyList<AreaDataSO> areas) {
            var id2path = new Dictionary<string, string>();
            for (int i = 0; i < areas.Count; i++) {
                id2path[areas[i].areaId] = areas[i].scenePath;
            }

            int count = ctx.GetAreaCount();
            for (int i = 0; i < count; i++) {
                var id = ctx.GetAreaId(i);
                var path = id2path[id];

                // A scene already open, such as one loaded in the Editor before Play, counts as loaded.
                var state = SceneManager.GetSceneByPath(path).isLoaded ? SceneState.Loaded : SceneState.Unloaded;
                scenes.Add(new AreaScene { id = id, path = path, state = state });
            }
        }

        /// <summary>Once per frame, after the ticks.</summary>
        public void Update(RenderContext ctx) {
            for (int i = 0; i < scenes.Count; i++) {
                var scene = scenes[i];
                bool active = ctx.IsAreaActive(i);

                // A scene that's mid-load or mid-unload finishes first, then catches up next frame.
                if (active && scene.state == SceneState.Unloaded) {
                    Load(scene);
                }
                else if (!active && scene.state == SceneState.Loaded) {
                    Unload(scene);
                }
            }
        }

        void Load(AreaScene scene) {
            var operation = SceneManager.LoadSceneAsync(scene.path, LoadSceneMode.Additive);
            if (operation == null) {
                Debug.LogError($"Grid World: couldn't load area '{scene.id}' from {scene.path}. Is it in the build's scene list?");
                return;
            }

            scene.state = SceneState.Loading;
            scene.startTime = Time.realtimeSinceStartup;
            operation.completed += _ => {
                scene.state = SceneState.Loaded;
                Debug.Log($"Grid World: loaded area '{scene.id}' in {ElapsedMs(scene)} ms.");
            };
        }

        void Unload(AreaScene scene) {
            var operation = SceneManager.UnloadSceneAsync(scene.path);
            if (operation == null) {
                Debug.LogError($"Grid World: couldn't unload area '{scene.id}' from {scene.path}.");
                return;
            }

            scene.state = SceneState.Unloading;
            scene.startTime = Time.realtimeSinceStartup;
            operation.completed += _ => {
                scene.state = SceneState.Unloaded;
                Debug.Log($"Grid World: unloaded area '{scene.id}' in {ElapsedMs(scene)} ms.");
            };
        }

        static int ElapsedMs(AreaScene scene) {
            return Mathf.RoundToInt((Time.realtimeSinceStartup - scene.startTime) * 1000f);
        }
    }
}
