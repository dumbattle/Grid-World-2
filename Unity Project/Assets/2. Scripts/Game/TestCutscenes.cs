using GridWorld;
using LPE;

namespace Game {
    /// <summary>The test world's cutscenes.</summary>
    public static class TestCutscenes {
        public const string FaceAndBackAway = "TEST_FaceAndBackAway";

        public static void Register(CutsceneCatalogue catalogue) {
            catalogue.Register(FaceAndBackAway, BuildFaceAndBackAway);
        }

        // The target turns to the player, they back away two tiles together, walk back, and the
        // target turns to the player again from wherever it ended up.
        static void BuildFaceAndBackAway(CutsceneBuilder b, CutsceneStart start) {
            var player = start.instigator;
            var npc = start.target;
            var toNpc = b.context.GetFacing(player);
            var away = toNpc.Reverse();

            b.Face(npc, away);
            b.Wait(30);
            b.Parallel(
                p => p.Walk(player, away, 2),
                n => n.Walk(npc, away, 2));
            b.Wait(20);
            b.Parallel(
                p => p.Walk(player, toNpc, 2),
                n => n.Walk(npc, toNpc, 2));
            b.Extend(e => {
                var ctx = e.context;
                var toPlayer = Direction4.FromVector2Int(ctx.GetTile(player) - ctx.GetTile(npc));
                // Null when a blocked walk teleported one of them out of line.
                if (toPlayer != null) {
                    e.Face(npc, toPlayer);
                }
            });
        }
    }
}
