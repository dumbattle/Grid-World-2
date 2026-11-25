using System;
using LPE;
using UnityEditor;
using UnityEngine;

namespace GridWorld {
    [CustomEditor(typeof(EntityAnimationSO))]
    public class EntityAnimationSOEditor : Editor {
        static readonly Direction4[] directions = { Direction4.down, Direction4.up, Direction4.left, Direction4.right };

        bool showFill = true;
        Texture2D idleSheet;
        Texture2D walkSheet;
        int idleTicks = 12;
        int walkTicks = 4;
        Direction4.Enum[] rowOrder = (Direction4.Enum[])EntityAnimationSheet.DefaultRowOrder.Clone();

        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            DrawEmptyClipWarning((EntityAnimationSO)target);

            EditorGUILayout.Space();
            showFill = EditorGUILayout.Foldout(showFill, "Fill From Sheet", true);
            if (showFill) {
                DrawFill((EntityAnimationSO)target);
            }
        }

        static void DrawEmptyClipWarning(EntityAnimationSO animation) {
            var empty = "";
            for (int i = 0; i < directions.Length; i++) {
                if (animation.idle.Get(directions[i]).numFrames == 0) {
                    empty += $" idle {directions[i]},";
                }
                if (animation.walk.Get(directions[i]).numFrames == 0) {
                    empty += $" walk {directions[i]},";
                }
            }
            if (empty.Length > 0) {
                EditorGUILayout.HelpBox($"Empty clips:{empty.TrimEnd(',')}. An empty walk clip plays idle instead, and an empty idle clip draws the placeholder.", MessageType.Warning);
            }
        }

        void DrawFill(EntityAnimationSO animation) {
            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox("Slice each sheet in the Sprite Editor first, one row per direction and one column per frame.", MessageType.None);

            idleSheet = (Texture2D)EditorGUILayout.ObjectField("Idle Sheet", idleSheet, typeof(Texture2D), false);
            idleTicks = Mathf.Max(1, EditorGUILayout.IntField("Idle Ticks Per Frame", idleTicks));
            walkSheet = (Texture2D)EditorGUILayout.ObjectField("Walk Sheet", walkSheet, typeof(Texture2D), false);
            walkTicks = Mathf.Max(1, EditorGUILayout.IntField("Walk Ticks Per Frame", walkTicks));

            for (int i = 0; i < rowOrder.Length; i++) {
                rowOrder[i] = (Direction4.Enum)EditorGUILayout.EnumPopup($"Row {i + 1} (from top)", rowOrder[i]);
            }

            using (new EditorGUI.DisabledScope(idleSheet == null && walkSheet == null)) {
                if (GUILayout.Button("Fill")) {
                    Fill(animation);
                }
            }
            EditorGUI.indentLevel--;
        }

        void Fill(EntityAnimationSO animation) {
            try {
                if (idleSheet != null) {
                    FillFrom(animation, EntityAnimationState.Idle, idleSheet, idleTicks);
                }
                if (walkSheet != null) {
                    FillFrom(animation, EntityAnimationState.Walk, walkSheet, walkTicks);
                }
            }
            catch (ArgumentException e) {
                EditorUtility.DisplayDialog("Fill From Sheet", e.Message, "OK");
            }
        }

        void FillFrom(EntityAnimationSO animation, EntityAnimationState state, Texture2D sheet, int ticks) {
            var sprites = EntityAnimationSheet.LoadSprites(sheet);
            if (sprites.Count == 0) {
                throw new ArgumentException($"{sheet.name} has no sprites. Set its Sprite Mode to Multiple and slice it.");
            }
            var rows = EntityAnimationSheet.SplitRows(sprites);
            EntityAnimationSheet.Fill(animation, state, rows, rowOrder, ticks);
        }
    }
}
