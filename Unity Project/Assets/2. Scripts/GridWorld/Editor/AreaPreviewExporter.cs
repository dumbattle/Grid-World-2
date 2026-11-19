using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace GridWorld {
    /// <summary>
    /// Writes PNG previews of areas to <c>Maps/previews/</c>: <c>&lt;id&gt;.png</c> drawn through a
    /// camera, and <c>&lt;id&gt;_logic.png</c> with a tile grid and blocked tiles tinted. The logic
    /// preview reads the baked data, so it shows the scene as of its last save.
    /// </summary>
    public static class AreaPreviewExporter {
        // Only used when the area has no sprites to take a resolution from.
        const int FallbackPixelsPerTile = 16;
        const int MaxTextureSize = 8192;

        static readonly Color Blocked = new Color(1f, 0.1f, 0.1f, 0.5f);
        static readonly Color PartlyBlocked = new Color(1f, 0.85f, 0f, 0.45f);
        static readonly Color GridLine = new Color(0f, 0f, 0f, 0.25f);

        [MenuItem("CONTEXT/AreaBehaviour/Export Preview")]
        static void ExportFromMenu(MenuCommand command) {
            var paths = Export((AreaBehaviour)command.context, MapPaths.previews);
            if (paths.Count > 0) {
                Debug.Log($"Exported preview to {string.Join(", ", paths)}");
            }
        }

        [MenuItem("Grid World/Export All Area Previews")]
        static void ExportAllFromMenu() {
            if (NewAreaCreator.HasUntitledScene()) {
                EditorUtility.DisplayDialog("Export All Area Previews", "Unity can't open area scenes next to an untitled one. Save it first.", "OK");
                return;
            }

            int count = 0;
            foreach (var scenePath in AllAreaScenePaths()) {
                var scene = SceneManager.GetSceneByPath(scenePath);
                bool opened = !scene.isLoaded;
                if (opened) {
                    scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                }

                foreach (var root in scene.GetRootGameObjects()) {
                    foreach (var area in root.GetComponentsInChildren<AreaBehaviour>(true)) {
                        count += Export(area, MapPaths.previews).Count > 0 ? 1 : 0;
                    }
                }

                if (opened) {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
            Debug.Log($"Exported {count} area previews to {MapPaths.previews}");
        }

        /// <summary>Returns the files written, which is empty if the area has no ID or empty bounds.</summary>
        public static List<string> Export(AreaBehaviour area, string folder) {
            var written = new List<string>();
            var bounds = area.bounds;
            if (string.IsNullOrEmpty(area.areaId) || bounds.width <= 0 || bounds.height <= 0) {
                Debug.LogWarning($"Can't export a preview of '{area.name}': it needs an ID and bounds.", area);
                return written;
            }

            int pixelsPerTile = Mathf.Min(PixelsPerTile(area), MaxTextureSize / Mathf.Max(bounds.width, bounds.height));
            var art = Render(area, pixelsPerTile);
            Directory.CreateDirectory(folder);

            var artPath = Path.Combine(folder, area.areaId + ".png");
            File.WriteAllBytes(artPath, art.EncodeToPNG());
            written.Add(artPath);

            if (area.data != null && area.data.worldBounds.size == bounds.size) {
                TintLogic(art, area.data, pixelsPerTile);
                var logicPath = Path.Combine(folder, area.areaId + "_logic.png");
                File.WriteAllBytes(logicPath, art.EncodeToPNG());
                written.Add(logicPath);
            }
            else {
                Debug.LogWarning($"Skipped the logic preview of '{area.areaId}': save the scene so it bakes first.", area);
            }

            Object.DestroyImmediate(art);
            return written;
        }

        static Texture2D Render(AreaBehaviour area, int pixelsPerTile) {
            var bounds = area.bounds;
            int width = bounds.width * pixelsPerTile;
            int height = bounds.height * pixelsPerTile;

            var cameraObject = new GameObject("Area Preview Camera") { hideFlags = HideFlags.HideAndDontSave };
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = bounds.height / 2f;
            camera.aspect = (float)bounds.width / bounds.height;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            var center = area.transform.position + new Vector3(bounds.center.x, bounds.center.y);
            cameraObject.transform.position = new Vector3(center.x, center.y, -10f);

            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var request = new RenderPipeline.StandardRequest { destination = target };
            if (RenderPipeline.SupportsRenderRequest(camera, request)) {
                RenderPipeline.SubmitRenderRequest(camera, request);
            }
            else {
                camera.targetTexture = target;
                camera.Render();
                camera.targetTexture = null;
            }

            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;

            target.Release();
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(cameraObject);
            return texture;
        }

        static void TintLogic(Texture2D texture, AreaDataSO data, int pixelsPerTile) {
            var tiles = data.CreateTiles();
            var bounds = data.worldBounds;
            var pixels = texture.GetPixels();
            int width = texture.width;

            for (int ty = 0; ty < bounds.height; ty++) {
                for (int tx = 0; tx < bounds.width; tx++) {
                    ushort enter = (ushort)(tiles.Get(new Vector2Int(bounds.x + tx, bounds.y + ty)) & TileBits.EnterAll);
                    Color? tint = enter == 0 ? Blocked : enter != TileBits.EnterAll ? PartlyBlocked : (Color?)null;

                    for (int py = 0; py < pixelsPerTile; py++) {
                        for (int px = 0; px < pixelsPerTile; px++) {
                            int index = (ty * pixelsPerTile + py) * width + tx * pixelsPerTile + px;
                            var color = pixels[index];
                            if (tint.HasValue) {
                                color = Color.Lerp(color, tint.Value, tint.Value.a);
                            }
                            // One-pixel lines on each tile's bottom and left edges, so tiles can be counted.
                            if (pixelsPerTile >= 4 && (px == 0 || py == 0)) {
                                color = Color.Lerp(color, GridLine, GridLine.a);
                            }
                            color.a = 1f;
                            pixels[index] = color;
                        }
                    }
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
        }

        static int PixelsPerTile(AreaBehaviour area) {
            foreach (var tilemap in area.GetComponentsInChildren<Tilemap>(true)) {
                int count = tilemap.GetUsedSpritesCount();
                if (count == 0) {
                    continue;
                }
                var sprites = new Sprite[count];
                tilemap.GetUsedSpritesNonAlloc(sprites);
                foreach (var sprite in sprites) {
                    if (sprite != null) {
                        // One tile is one unit, so pixels per unit is the tile's size in pixels.
                        return Mathf.Max(1, Mathf.RoundToInt(sprite.pixelsPerUnit));
                    }
                }
            }
            return FallbackPixelsPerTile;
        }

        static IEnumerable<string> AllAreaScenePaths() {
            var paths = new HashSet<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(WorldDataSO))) {
                var world = AssetDatabase.LoadAssetAtPath<WorldDataSO>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (var area in world.areas) {
                    if (area != null && File.Exists(area.scenePath)) {
                        paths.Add(area.scenePath);
                    }
                }
            }
            return paths;
        }
    }
}
