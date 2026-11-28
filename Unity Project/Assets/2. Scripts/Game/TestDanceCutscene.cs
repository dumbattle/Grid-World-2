using System;
using GridWorld;

namespace Game {
    /// <summary>A tile interaction with no NPC: the player spins twice on the spot.</summary>
    [Serializable]
    public class TestDanceCutscene : ICutscene {
        public void Build(CutsceneBuildContext ctx, CutsceneBuilder b) {
            var player = ctx.GetPlayer();
            TestSpin.Add(b, player, ctx.GetFacing(player), 8, 6);
        }
    }
}
