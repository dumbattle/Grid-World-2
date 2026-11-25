using LPE;

namespace GridWorld {
    /// <summary>
    /// Picks an entity's animation from its step and facing. View only: nothing here feeds back
    /// into logic.
    /// </summary>
    public static class EntityAnimationRules {
        /// <summary>
        /// Walk while a step is moving or has just finished, so the cycle carries on into a chained
        /// step. Idle otherwise.
        /// </summary>
        public static EntityAnimationState ChooseState(Step step) {
            return step.state == StepState.Idle ? EntityAnimationState.Idle : EntityAnimationState.Walk;
        }

        /// <summary>
        /// How many animation ticks to advance per world tick: 2 for a step faster than the
        /// definition's walk, so running plays the walk at double speed, and 1 otherwise.
        /// </summary>
        public static int ChooseSpeed(Step step, EntityDefinitionSO definition) {
            if (step.state != StepState.Idle && step.duration < definition.walkTicks) {
                return 2;
            }
            return 1;
        }

        /// <summary>
        /// The clip to play, falling back from an empty walk clip to the idle clip for the same
        /// direction. Null when there's nothing to play, so the view draws a placeholder.
        /// </summary>
        public static SpriteClip ChooseClip(EntityAnimationSO animation, EntityAnimationState state, Direction4 facing) {
            if (animation == null) {
                return null;
            }

            var clip = animation.Get(state, facing);
            if (clip.numFrames == 0 && state == EntityAnimationState.Walk) {
                clip = animation.Get(EntityAnimationState.Idle, facing);
            }
            return clip.numFrames == 0 ? null : clip;
        }
    }
}
