using GridWorld;
using LPE;

namespace Game {
    // Turns an entity clockwise on the spot, which can't be blocked, unlike a walk.
    static class TestSpin {
        static readonly Direction4[] clockwise = { Direction4.up, Direction4.right, Direction4.down, Direction4.left };

        public static void Add(CutsceneBuilder b, EntityID id, Direction4 from, int turns, int ticksPerTurn) {
            int first = System.Array.IndexOf(clockwise, from);
            for (int i = 1; i <= turns; i++) {
                b.Face(id, clockwise[(first + i) % clockwise.Length]);
                b.Wait(ticksPerTurn);
            }
        }
    }
}
