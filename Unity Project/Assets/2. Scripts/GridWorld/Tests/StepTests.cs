using NUnit.Framework;
using UnityEngine;

namespace GridWorld.Tests {
    public class StepTests {
        static readonly Vector2Int from = new Vector2Int(2, 3);
        static readonly Vector2Int to = new Vector2Int(3, 3);

        static void AssertPosition(Vector2 expected, Vector2 actual) {
            Assert.AreEqual(expected.x, actual.x, 1e-4f);
            Assert.AreEqual(expected.y, actual.y, 1e-4f);
        }

        [Test]
        public void BlendedPosition_Idle_IsTheTile() {
            AssertPosition(from, Step.Idle(from).BlendedPosition(0.7f));
        }

        [Test]
        public void BlendedPosition_FirstTick_BlendsFromTheStartTile() {
            var step = new Step(from, to, 4, 1);

            AssertPosition(from, step.BlendedPosition(0f));
            AssertPosition(new Vector2(2.25f, 3), step.BlendedPosition(1f));
        }

        [Test]
        public void BlendedPosition_Finished_ReachesTheTargetAtFullBlend() {
            var step = new Step(from, to, 4, 4);

            AssertPosition(new Vector2(2.75f, 3), step.BlendedPosition(0f));
            AssertPosition(to, step.BlendedPosition(1f));
        }

        [Test]
        public void BlendedPosition_NextStepAfterFinish_ContinuesAtTheSameRate() {
            var finished = new Step(from, to, 4, 4);
            var next = new Step(to, to + Vector2Int.right, 4, 1);

            // The finished step's end is the next step's start, so there's no pause at the border.
            AssertPosition(finished.BlendedPosition(1f), next.BlendedPosition(0f));
            AssertPosition(new Vector2(3.25f, 3), next.BlendedPosition(1f));
        }
    }
}
