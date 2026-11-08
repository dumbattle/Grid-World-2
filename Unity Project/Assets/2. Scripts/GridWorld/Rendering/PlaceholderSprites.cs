using System;
using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Generated sprites for entities with no visual: a square filling one tile, with a dark bar on
    /// the side it faces. Destroy with <see cref="Dispose"/>.
    /// </summary>
    public class PlaceholderSprites : IDisposable {
        // Only the placeholder's own resolution. It always spans one tile.
        const int Pixels = 16;
        const int Inset = 2;
        const int BarLength = 8;
        const int BarDepth = 3;

        static readonly Color32 clear = new Color32(0, 0, 0, 0);
        static readonly Color32 body = new Color32(232, 112, 72, 255);
        static readonly Color32 outline = new Color32(96, 40, 32, 255);

        // Indexed by Direction4.Enum.
        Texture2D[] textures = new Texture2D[4];
        Sprite[] sprites = new Sprite[4];

        public PlaceholderSprites() {
            CreateSprite(Direction4.up);
            CreateSprite(Direction4.down);
            CreateSprite(Direction4.left);
            CreateSprite(Direction4.right);
        }

        public Sprite Get(Direction4 facing) {
            var index = (int)(Direction4.Enum)facing;
            return index < sprites.Length ? sprites[index] : sprites[(int)Direction4.Enum.down];
        }

        public void Dispose() {
            for (int i = 0; i < sprites.Length; i++) {
                UnityObjects.Destroy(sprites[i]);
                UnityObjects.Destroy(textures[i]);
            }
        }

        void CreateSprite(Direction4 facing) {
            var pixels = new Color32[Pixels * Pixels];
            var forward = facing.ToVector2Int();
            int min = Inset;
            int max = Pixels - 1 - Inset;
            int barStart = (Pixels - BarLength) / 2;

            for (int y = 0; y < Pixels; y++) {
                for (int x = 0; x < Pixels; x++) {
                    Color32 color = clear;

                    if (x >= min && x <= max && y >= min && y <= max) {
                        bool edge = x == min || x == max || y == min || y == max;
                        color = edge || InBar(x, y, forward, min, max, barStart) ? outline : body;
                    }
                    pixels[x + y * Pixels] = color;
                }
            }

            var texture = new Texture2D(Pixels, Pixels, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply();

            int index = (int)(Direction4.Enum)facing;
            textures[index] = texture;
            sprites[index] = Sprite.Create(texture, new Rect(0, 0, Pixels, Pixels), new Vector2(0.5f, 0.5f), Pixels);
            sprites[index].name = $"Placeholder {facing}";
        }

        static bool InBar(int x, int y, Vector2Int forward, int min, int max, int barStart) {
            int along = forward.x != 0 ? y : x;
            if (along < barStart || along >= barStart + BarLength) {
                return false;
            }

            int depth;
            if (forward.x > 0) {
                depth = max - x;
            }
            else if (forward.x < 0) {
                depth = x - min;
            }
            else if (forward.y > 0) {
                depth = max - y;
            }
            else {
                depth = y - min;
            }
            return depth < BarDepth;
        }
    }
}
