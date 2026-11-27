using System;

namespace GridWorld {
    /// <summary>
    /// Builds more steps when it's reached, from up-to-date state, then runs them. Its builder can
    /// act only on entities the cutscene already locked.
    /// </summary>
    public class ExtendSequence : ISequence {
        readonly CutsceneContext ctx;
        readonly CutsceneBuildContext buildCtx;
        readonly RunningCutscene cutscene;
        readonly Action<CutsceneBuildContext, CutsceneBuilder> extend;
        ISequence inner;

        internal ExtendSequence(CutsceneContext ctx, CutsceneBuildContext buildCtx, RunningCutscene cutscene,
                                Action<CutsceneBuildContext, CutsceneBuilder> extend) {
            this.ctx = ctx;
            this.buildCtx = buildCtx;
            this.cutscene = cutscene;
            this.extend = extend;
        }

        public void Next() {
            if (inner == null) {
                var builder = new CutsceneBuilder(ctx, buildCtx, cutscene);
                extend(buildCtx, builder);
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
