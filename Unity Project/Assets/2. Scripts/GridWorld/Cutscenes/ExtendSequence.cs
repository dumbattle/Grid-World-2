using System;

namespace GridWorld {
    /// <summary>
    /// Builds more steps when it's reached, from up-to-date state, then runs them. Its builder can
    /// act only on entities the cutscene already locked.
    /// </summary>
    public class ExtendSequence : ISequence {
        readonly CutsceneContext ctx;
        readonly RunningCutscene cutscene;
        readonly Action<CutsceneBuilder> extend;
        ISequence inner;

        internal ExtendSequence(CutsceneContext ctx, RunningCutscene cutscene, Action<CutsceneBuilder> extend) {
            this.ctx = ctx;
            this.cutscene = cutscene;
            this.extend = extend;
        }

        public void Next() {
            if (inner == null) {
                var builder = new CutsceneBuilder(ctx, cutscene);
                extend(builder);
                inner = builder.Build();
            }
            inner.Next();
        }

        public bool Done() {
            return inner != null && inner.Done();
        }

        public void OnComplete() {
            inner?.OnComplete();
        }
    }
}
