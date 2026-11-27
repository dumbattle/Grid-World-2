using GridWorld;
using LPE;

namespace Game {
    /// <summary>
    /// The debug key's cutscene. The NPC turns to the player, they back away two tiles together,
    /// walk back, and the NPC turns to the player again from wherever it ended up.
    /// </summary>
    public class TestFaceAndBackAwayCutscene : ICutscene {
        readonly EntityID player;
        readonly EntityID npc;

        public TestFaceAndBackAwayCutscene(EntityID player, EntityID npc) {
            this.player = player;
            this.npc = npc;
        }

        public void Build(CutsceneBuildContext ctx, CutsceneBuilder b) {
            var toNpc = ctx.GetFacing(player);
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
            b.Extend((now, e) => {
                var toPlayer = Direction4.FromVector2Int(now.GetTile(player) - now.GetTile(npc));
                // Null when a blocked walk teleported one of them out of line.
                if (toPlayer != null) {
                    e.Face(npc, toPlayer);
                }
            });
        }
    }
}
