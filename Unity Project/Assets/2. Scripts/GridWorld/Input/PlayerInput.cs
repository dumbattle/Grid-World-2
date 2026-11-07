using System.Collections.Generic;
using LPE;

namespace GridWorld {
    /// <summary>
    /// Unity input as the root last saw it. Written outside the phases, and read by the player input
    /// system. When several directions are held, the most recently pressed one wins.
    /// </summary>
    public class PlayerInput {
        // In press order, so the last one is the most recent.
        List<Direction4> heldDirections = new List<Direction4>(4);

        public bool runHeld;

        public Direction4 direction => heldDirections.Count > 0 ? heldDirections[heldDirections.Count - 1] : null;

        public void SetDirectionHeld(Direction4 direction, bool held) {
            bool wasHeld = heldDirections.Contains(direction);

            if (held && !wasHeld) {
                heldDirections.Add(direction);
            }
            else if (!held && wasHeld) {
                heldDirections.Remove(direction);
            }
        }
    }
}
