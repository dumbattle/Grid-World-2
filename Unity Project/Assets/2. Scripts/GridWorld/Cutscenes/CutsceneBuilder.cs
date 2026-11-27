using System;
using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Builds a cutscene as a list of steps that run one after another, each starting on the tick
    /// the one before it finishes. Every step records the entity it acts on, and the cutscene locks
    /// them all when it starts. A game adds its own steps as extension methods that call
    /// <see cref="Lock"/> and <see cref="Add"/>.
    /// </summary>
    public class CutsceneBuilder {
        readonly CutsceneContext ctx;
        readonly CutsceneBuildContext buildCtx;
        readonly RunningCutscene cutscene;
        SequenceQueue queue = SequenceQueue.GetNew();

        internal CutsceneBuilder(CutsceneContext ctx, CutsceneBuildContext buildCtx, RunningCutscene cutscene) {
            this.ctx = ctx;
            this.buildCtx = buildCtx;
            this.cutscene = cutscene;
        }

        // For a game's own steps' messages.
        public string cutsceneName => cutscene.name;

        /// <summary>
        /// Adds an entity to the cutscene's lock. Steps call this for the entity they act on. Once the
        /// cutscene has started, as in an <see cref="Extend"/>, only entities already locked are allowed.
        /// </summary>
        public void Lock(EntityID id) {
            if (!id.IsValid()) {
                throw new ArgumentException($"Cutscene '{cutscene.name}' can't act on an invalid entity.", nameof(id));
            }
            if (cutscene.entities.Contains(id)) {
                return;
            }
            if (cutscene.started) {
                throw new InvalidOperationException($"Cutscene '{cutscene.name}' acts on {id}, which it didn't lock. Lock it when the cutscene is built, with Lock.");
            }
            cutscene.entities.Add(id);
        }

        /// <summary>Adds any sequence as the next step. Call <see cref="Lock"/> for the entities it acts on.</summary>
        public void Add(ISequence sequence) {
            if (sequence == null) {
                throw new ArgumentNullException(nameof(sequence));
            }
            if (queue == null) {
                throw new InvalidOperationException($"Cutscene '{cutscene.name}' has already been built.");
            }
            queue.AddSequence(sequence, playNextImmediate: true);
        }

        /// <summary>
        /// Walks <paramref name="count"/> tiles in a straight line. A walk blocked for
        /// <see cref="WalkStepsSequence.TimeoutTicks"/> ticks teleports to its destination.
        /// </summary>
        public void Walk(EntityID id, Direction4 direction, int count, bool run = false) {
            if (direction == null) {
                throw new ArgumentNullException(nameof(direction));
            }
            if (count < 1) {
                throw new ArgumentOutOfRangeException(nameof(count), $"Cutscene '{cutscene.name}' has a walk of {count} tiles. A walk needs at least 1.");
            }
            Lock(id);
            Add(new WalkStepsSequence(ctx, cutscene.name, id, direction, count, run));
        }

        /// <summary>Turns in place. The new facing shows after this tick's movement phase.</summary>
        public void Face(EntityID id, Direction4 direction) {
            if (direction == null) {
                throw new ArgumentNullException(nameof(direction));
            }
            Lock(id);
            Add(new FaceSequence(ctx, cutscene.name, id, direction));
        }

        public void Teleport(EntityID id, Vector2Int tile) {
            Lock(id);
            Add(new TeleportSequence(ctx, cutscene.name, id, tile));
        }

        /// <summary>The next step starts exactly <paramref name="ticks"/> ticks after this one does.</summary>
        public void Wait(int ticks) {
            if (ticks < 0) {
                throw new ArgumentOutOfRangeException(nameof(ticks));
            }
            if (ticks == 0) {
                return;
            }
            // The pause's first tick is the one the step before it finished on, so it needs one more.
            Add(PauseFramesSequence.Get(ticks + 1));
        }

        /// <summary>Runs an action once, with no gap before the next step.</summary>
        public void Do(Action action) {
            if (action == null) {
                throw new ArgumentNullException(nameof(action));
            }
            Add(new LambdaSequence(action));
        }

        /// <summary>
        /// Runs each branch at the same time, as its own list of steps. The next step starts once
        /// every branch has finished.
        /// </summary>
        public void Parallel(params Action<CutsceneBuilder>[] branches) {
            if (branches == null) {
                throw new ArgumentNullException(nameof(branches));
            }

            ISequence combined = null;
            for (int i = 0; i < branches.Length; i++) {
                if (branches[i] == null) {
                    throw new ArgumentNullException(nameof(branches), $"Cutscene '{cutscene.name}' has a null parallel branch.");
                }
                var branch = new CutsceneBuilder(ctx, buildCtx, cutscene);
                branches[i](branch);
                var built = branch.Build();
                // LPE's parallel takes two or three sequences, so more branches nest in pairs.
                combined = combined == null ? built : ParallelSequence.Get(combined, built);
            }

            if (combined != null) {
                Add(combined);
            }
        }

        /// <summary>
        /// Runs <paramref name="extend"/> when the steps before it finish, then the steps it adds.
        /// It reads up-to-date state, and can act only on entities the cutscene already locked.
        /// </summary>
        public void Extend(Action<CutsceneBuildContext, CutsceneBuilder> extend) {
            if (extend == null) {
                throw new ArgumentNullException(nameof(extend));
            }
            Add(new ExtendSequence(ctx, buildCtx, cutscene, extend));
        }

        internal ISequence Build() {
            if (queue == null) {
                throw new InvalidOperationException($"Cutscene '{cutscene.name}' has already been built.");
            }
            var built = queue;
            queue = null;
            return built;
        }
    }
}
