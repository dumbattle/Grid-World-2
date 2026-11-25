using System;
using System.Collections.Generic;
using System.Linq;
using LPE;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GridWorld.Tests {
    public class EntityAnimationTests {
        // A 4x4 sheet of 8-pixel cells. Rows from the top: down, up, left, right.
        const int Cell = 8;
        const int Columns = 4;
        const int Rows = 4;

        Texture2D texture;
        // [row from top][column]
        Sprite[][] sheet;
        List<Object> created = new List<Object>();

        [SetUp]
        public void SetUp() {
            texture = new Texture2D(Cell * Columns, Cell * Rows);
            created.Add(texture);
            sheet = new Sprite[Rows][];
            for (int row = 0; row < Rows; row++) {
                sheet[row] = new Sprite[Columns];
                for (int column = 0; column < Columns; column++) {
                    // Texture y runs upward, so the top row has the highest rect.
                    var rect = new Rect(column * Cell, (Rows - 1 - row) * Cell, Cell, Cell);
                    var sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0f), Cell);
                    sprite.name = $"r{row}c{column}";
                    sheet[row][column] = sprite;
                    created.Add(sprite);
                }
            }
        }

        [TearDown]
        public void TearDown() {
            for (int i = 0; i < created.Count; i++) {
                Object.DestroyImmediate(created[i]);
            }
            created.Clear();
        }

        EntityAnimationSO CreateAnimation(int idleTicks, int walkTicks) {
            var animation = ScriptableObject.CreateInstance<EntityAnimationSO>();
            created.Add(animation);
            var order = EntityAnimationSheet.DefaultRowOrder;
            if (idleTicks > 0) {
                EntityAnimationSheet.Fill(animation, EntityAnimationState.Idle, sheet, order, idleTicks);
            }
            if (walkTicks > 0) {
                EntityAnimationSheet.Fill(animation, EntityAnimationState.Walk, sheet, order, walkTicks);
            }
            return animation;
        }

        static void SetAnimation(EntityDefinitionSO definition, EntityAnimationSO animation) {
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("animationSet").objectReferenceValue = animation;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // --- Rules ---

        [Test]
        public void ChooseState_IdleStep_Idle() {
            Assert.AreEqual(EntityAnimationState.Idle, EntityAnimationRules.ChooseState(Step.Idle(Vector2Int.zero)));
        }

        [TestCase(5)]
        [TestCase(16)]
        public void ChooseState_MovingOrFinishedStep_Walk(int elapsed) {
            var step = new Step(Vector2Int.zero, Vector2Int.right, 16, elapsed);
            Assert.AreEqual(EntityAnimationState.Walk, EntityAnimationRules.ChooseState(step));
        }

        [TestCase(16, 1)]
        [TestCase(8, 2)]
        public void ChooseSpeed_StepDuration_DoubleOnlyWhenFasterThanWalk(int duration, int expected) {
            var definition = ScriptableObject.CreateInstance<EntityDefinitionSO>();
            created.Add(definition);
            var step = new Step(Vector2Int.zero, Vector2Int.right, duration, 1);

            Assert.AreEqual(expected, EntityAnimationRules.ChooseSpeed(step, definition));
        }

        [Test]
        public void ChooseSpeed_IdleStep_One() {
            var definition = ScriptableObject.CreateInstance<EntityDefinitionSO>();
            created.Add(definition);

            Assert.AreEqual(1, EntityAnimationRules.ChooseSpeed(Step.Idle(Vector2Int.zero), definition));
        }

        [Test]
        public void ChooseClip_NoAnimation_Null() {
            Assert.IsNull(EntityAnimationRules.ChooseClip(null, EntityAnimationState.Idle, Direction4.down));
        }

        [Test]
        public void ChooseClip_EmptyWalkClip_FallsBackToIdle() {
            var animation = CreateAnimation(idleTicks: 12, walkTicks: 0);

            var clip = EntityAnimationRules.ChooseClip(animation, EntityAnimationState.Walk, Direction4.left);

            Assert.AreSame(animation.idle.Get(Direction4.left), clip);
        }

        [Test]
        public void ChooseClip_EmptyClips_Null() {
            var animation = CreateAnimation(idleTicks: 0, walkTicks: 0);

            Assert.IsNull(EntityAnimationRules.ChooseClip(animation, EntityAnimationState.Walk, Direction4.up));
        }

        // --- Sheets ---

        [Test]
        public void SplitRows_ShuffledSprites_TopRowFirstAndLeftToRight() {
            var shuffled = sheet.SelectMany(row => row).Reverse().ToList();
            (shuffled[0], shuffled[7]) = (shuffled[7], shuffled[0]);

            var rows = EntityAnimationSheet.SplitRows(shuffled);

            Assert.AreEqual(Rows, rows.Count);
            for (int row = 0; row < Rows; row++) {
                CollectionAssert.AreEqual(sheet[row], rows[row], $"Row {row}");
            }
        }

        [Test]
        public void Fill_DefaultRowOrder_RowsBecomeDownUpLeftRight() {
            var animation = CreateAnimation(idleTicks: 12, walkTicks: 4);

            Assert.AreSame(sheet[0][2], animation.walk.Get(Direction4.down).GetSprite(2));
            Assert.AreSame(sheet[1][0], animation.walk.Get(Direction4.up).GetSprite(0));
            Assert.AreSame(sheet[2][3], animation.idle.Get(Direction4.left).GetSprite(3));
            Assert.AreSame(sheet[3][1], animation.idle.Get(Direction4.right).GetSprite(1));
            Assert.AreEqual(4, animation.walk.Get(Direction4.right).GetFrameDuration(0));
            Assert.AreEqual(12, animation.idle.Get(Direction4.right).GetFrameDuration(0));
        }

        [Test]
        public void Fill_DirectionUsedTwice_Throws() {
            var animation = ScriptableObject.CreateInstance<EntityAnimationSO>();
            created.Add(animation);
            var order = new[] { Direction4.Enum.down, Direction4.Enum.down, Direction4.Enum.left, Direction4.Enum.right };

            Assert.Throws<ArgumentException>(() => EntityAnimationSheet.Fill(animation, EntityAnimationState.Idle, sheet, order, 4));
        }

        [Test]
        public void Fill_WrongRowCount_Throws() {
            var animation = ScriptableObject.CreateInstance<EntityAnimationSO>();
            created.Add(animation);
            var threeRows = sheet.Take(3).ToList();

            Assert.Throws<ArgumentException>(() => EntityAnimationSheet.Fill(animation, EntityAnimationState.Idle, threeRows, EntityAnimationSheet.DefaultRowOrder, 4));
        }

        // --- WorldView ---

        [Test]
        public void WorldViewTick_IdleEntityWithAnimation_ShowsIdleFrameUntinted() {
            var test = new TestWorld();
            var parent = new GameObject("View");
            var view = new WorldView(parent.transform);
            try {
                SetAnimation(test.definition, CreateAnimation(idleTicks: 12, walkTicks: 3));
                test.SetAreaActive(true);
                test.Spawn(Vector2Int.zero, Direction4.left);

                view.Tick(test.world.GetContext<RenderContext>());

                var renderer = parent.GetComponentInChildren<SpriteRenderer>();
                Assert.AreSame(sheet[2][0], renderer.sprite);
                Assert.AreEqual(Color.white, renderer.color);
            }
            finally {
                view.Dispose();
                Object.DestroyImmediate(parent);
                test.Dispose();
            }
        }

        [Test]
        public void WorldViewTick_ChainedSteps_WalkCycleCarriesOnThenIdleRestarts() {
            var test = new TestWorld();
            var parent = new GameObject("View");
            var view = new WorldView(parent.transform);
            try {
                // 3 ticks per frame, so a 16-tick step doesn't line up with the 12-tick cycle.
                SetAnimation(test.definition, CreateAnimation(idleTicks: 12, walkTicks: 3));
                test.SetAreaActive(true);
                var id = test.Spawn(Vector2Int.zero, Direction4.right);
                var render = test.world.GetContext<RenderContext>();

                for (int tick = 1; tick <= 17; tick++) {
                    test.Request(id, Direction4.right);
                    test.Tick();
                    view.Tick(render);
                }

                // Tick 17 starts the second step. A restart would show frame 0; carrying on shows
                // update 17, which is frame (17 - 1) / 3 % 4 = 1.
                var renderer = parent.GetComponentInChildren<SpriteRenderer>();
                Assert.AreEqual(StepState.Moving, test.StepOf(id).state);
                Assert.AreSame(sheet[3][1], renderer.sprite);

                // Let the second step finish and clear to idle.
                for (int tick = 18; tick <= 33; tick++) {
                    test.Tick();
                    view.Tick(render);
                }
                Assert.AreEqual(StepState.Idle, test.StepOf(id).state);
                Assert.AreSame(sheet[3][0], renderer.sprite);
            }
            finally {
                view.Dispose();
                Object.DestroyImmediate(parent);
                test.Dispose();
            }
        }

        [Test]
        public void WorldViewRender_Entity_StandsOnTheBottomCentreOfItsTile() {
            var test = new TestWorld();
            var parent = new GameObject("View");
            var view = new WorldView(parent.transform);
            try {
                test.SetAreaActive(true);
                test.Spawn(new Vector2Int(2, 3), Direction4.down);
                var render = test.world.GetContext<RenderContext>();

                view.Tick(render);
                view.Render(render, 1f);

                var position = parent.GetComponentInChildren<SpriteRenderer>().transform.position;
                Assert.AreEqual(new Vector3(2.5f, 3f, 0f), position);
            }
            finally {
                view.Dispose();
                Object.DestroyImmediate(parent);
                test.Dispose();
            }
        }
    }
}
