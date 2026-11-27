using System;

namespace GridWorld.Tests {
    /// <summary>Lets a test write its cutscene inline.</summary>
    class LambdaCutscene : ICutscene {
        readonly Action<CutsceneBuildContext, CutsceneBuilder> build;

        public LambdaCutscene(Action<CutsceneBuildContext, CutsceneBuilder> build) {
            this.build = build;
        }

        public void Build(CutsceneBuildContext ctx, CutsceneBuilder b) {
            build(ctx, b);
        }
    }
}
