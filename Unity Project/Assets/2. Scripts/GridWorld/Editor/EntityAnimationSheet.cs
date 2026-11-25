using System;
using System.Collections.Generic;
using System.Linq;
using LPE;
using UnityEditor;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Fills an <see cref="EntityAnimationSO"/> from a sliced sprite sheet with one row per facing
    /// direction and one column per frame.
    /// </summary>
    public static class EntityAnimationSheet {
        public static readonly Direction4.Enum[] DefaultRowOrder = {
            Direction4.Enum.down, Direction4.Enum.up, Direction4.Enum.left, Direction4.Enum.right,
        };

        /// <summary>The sprites sliced from a texture asset, in no particular order.</summary>
        public static List<Sprite> LoadSprites(Texture2D sheet) {
            var path = AssetDatabase.GetAssetPath(sheet);
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToList();
        }

        /// <summary>
        /// Groups sprites into rows, top row first, each sorted left to right. A sprite joins the
        /// current row while its bottom edge is within half a sprite's height of the row's first
        /// sprite, so trimmed slices of slightly different sizes still group correctly.
        /// </summary>
        public static List<Sprite[]> SplitRows(IEnumerable<Sprite> sprites) {
            var sorted = sprites.OrderByDescending(s => s.rect.y).ToList();
            var rows = new List<Sprite[]>();
            var row = new List<Sprite>();

            for (int i = 0; i < sorted.Count; i++) {
                var sprite = sorted[i];
                if (row.Count > 0 && row[0].rect.y - sprite.rect.y > row[0].rect.height / 2) {
                    rows.Add(row.OrderBy(s => s.rect.x).ToArray());
                    row.Clear();
                }
                row.Add(sprite);
            }
            if (row.Count > 0) {
                rows.Add(row.OrderBy(s => s.rect.x).ToArray());
            }
            return rows;
        }

        /// <summary>
        /// Replaces the clips for <paramref name="state"/>: row i becomes the clip for
        /// <paramref name="rowOrder"/>[i], with every frame lasting <paramref name="ticksPerFrame"/>.
        /// Undoable.
        /// </summary>
        public static void Fill(EntityAnimationSO animation, EntityAnimationState state, IList<Sprite[]> rows,
                                IList<Direction4.Enum> rowOrder, int ticksPerFrame) {
            CheckRowOrder(rowOrder);
            if (rows.Count != rowOrder.Count) {
                throw new ArgumentException($"The sheet has {rows.Count} rows, but there must be one per direction ({rowOrder.Count}).");
            }
            if (ticksPerFrame < 1) {
                throw new ArgumentException("Ticks per frame must be at least 1.");
            }

            var serialized = new SerializedObject(animation);
            var clipsName = state == EntityAnimationState.Walk ? "walkClips" : "idleClips";
            for (int i = 0; i < rows.Count; i++) {
                var frames = serialized.FindProperty($"{clipsName}.{ClipName(rowOrder[i])}.frames");
                var row = rows[i];
                frames.arraySize = row.Length;
                for (int f = 0; f < row.Length; f++) {
                    var frame = frames.GetArrayElementAtIndex(f);
                    frame.FindPropertyRelative("sprite").objectReferenceValue = row[f];
                    frame.FindPropertyRelative("duration").intValue = ticksPerFrame;
                    frame.FindPropertyRelative("tags").intValue = 0;
                }
            }
            serialized.ApplyModifiedProperties();
        }

        static void CheckRowOrder(IList<Direction4.Enum> rowOrder) {
            var seen = new HashSet<Direction4.Enum>();
            for (int i = 0; i < rowOrder.Count; i++) {
                if (rowOrder[i] == Direction4.Enum.none) {
                    throw new ArgumentException($"Row {i + 1} has no direction.");
                }
                if (!seen.Add(rowOrder[i])) {
                    throw new ArgumentException($"{rowOrder[i]} is used for more than one row.");
                }
            }
        }

        static string ClipName(Direction4.Enum direction) {
            switch (direction) {
                case Direction4.Enum.up:
                    return "up";
                case Direction4.Enum.left:
                    return "left";
                case Direction4.Enum.right:
                    return "right";
                default:
                    return "down";
            }
        }
    }
}
