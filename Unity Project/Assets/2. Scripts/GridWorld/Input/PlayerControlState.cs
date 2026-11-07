using LPE;

namespace GridWorld {
    /// <summary>
    /// How long the player has held their current direction. Owned by the player input system in the
    /// decision phase.
    /// </summary>
    public class PlayerControlState {
        public Direction4 direction;
        public int heldTicks;
        // Set when a press turned the player, so they walk only once the hold delay passes.
        public bool waitingToWalk;

        public void Reset() {
            direction = null;
            heldTicks = 0;
            waitingToWalk = false;
        }
    }
}
