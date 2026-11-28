using System;
using GridWorld;
using LPE;
using UnityEngine;

namespace Game {
    /// <summary>
    /// The greeter's interaction. Holds the player while the NPC turns to them, spins once on the
    /// spot, and ends facing them.
    /// </summary>
    [Serializable]
    public class TestGreetCutscene : ICutscene {
        [Tooltip("The feature ID of the NPC that greets.")]
        [SerializeField] string npcId = "";

        public TestGreetCutscene() { }

        public TestGreetCutscene(string npcId) {
            this.npcId = npcId;
        }

        public void Build(CutsceneBuildContext ctx, CutsceneBuilder b) {
            if (!ctx.TryGetEntity(npcId, out var npc)) {
                throw new InvalidOperationException($"No entity has the feature ID '{npcId}'.");
            }
            var player = ctx.GetPlayer();
            // Null if they aren't side by side, such as when the NPC was reached across a counter.
            var toPlayer = Direction4.FromVector2Int(ctx.GetTile(player) - ctx.GetTile(npc)) ?? ctx.GetFacing(player).Reverse();

            b.Lock(player);
            b.Face(npc, toPlayer);
            b.Wait(20);
            TestSpin.Add(b, npc, toPlayer, 4, 8);
        }
    }
}
