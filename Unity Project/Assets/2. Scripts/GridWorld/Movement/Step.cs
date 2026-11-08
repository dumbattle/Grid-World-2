using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// An entity's current step. The entity's tile is already <see cref="to"/> while the step runs.
    /// An idle step has a duration of 0.
    /// </summary>
    public readonly struct Step {
        public readonly Vector2Int from;
        public readonly Vector2Int to;
        public readonly int duration;
        public readonly int elapsed;

        public StepState state {
            get {
                if (duration == 0) {
                    return StepState.Idle;
                }
                return elapsed >= duration ? StepState.Finished : StepState.Moving;
            }
        }

        public Step(Vector2Int from, Vector2Int to, int duration, int elapsed) {
            this.from = from;
            this.to = to;
            this.duration = duration;
            this.elapsed = elapsed;
        }

        public static Step Idle(Vector2Int tile) {
            return new Step(tile, tile, 0, 0);
        }

        public Step Advanced() {
            return new Step(from, to, duration, elapsed + 1);
        }

        /// <summary>
        /// The position in tiles, blended from the previous tick (at <paramref name="alpha"/> 0) to
        /// this one (at 1). A step starts at elapsed 1, so the previous tick is elapsed - 1 even on
        /// the step's first tick. For the view only.
        /// </summary>
        public Vector2 BlendedPosition(float alpha) {
            if (duration == 0) {
                return to;
            }

            float progress = Mathf.Clamp01((elapsed - 1 + alpha) / duration);
            return Vector2.Lerp(from, to, progress);
        }
    }
}
