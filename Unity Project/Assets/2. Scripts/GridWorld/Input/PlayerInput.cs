using System.Collections.Generic;
using LPE;

namespace GridWorld {
    /// <summary>
    /// Unity input as the root last saw it. Written outside the phases, and read by the player input
    /// system. When several directions are held, the most recently pressed one wins. A button press
    /// stays buffered for <see cref="ButtonBufferTicks"/> ticks, or until a tick uses it.
    /// </summary>
    public class PlayerInput {
        public const int ButtonBufferTicks = 6;

        // In press order, so the last one is the most recent.
        List<Direction4> heldDirections = new List<Direction4>(4);
        int interactTicksLeft;

        public bool runHeld;

        public Direction4 direction => heldDirections.Count > 0 ? heldDirections[heldDirections.Count - 1] : null;
        public bool interactBuffered => interactTicksLeft > 0;

        public void SetDirectionHeld(Direction4 direction, bool held) {
            bool wasHeld = heldDirections.Contains(direction);

            if (held && !wasHeld) {
                heldDirections.Add(direction);
            }
            else if (!held && wasHeld) {
                heldDirections.Remove(direction);
            }
        }

        public void PressInteract() {
            interactTicksLeft = ButtonBufferTicks;
        }

        /// <summary>Uses up a buffered press, or drops it.</summary>
        public void ClearInteract() {
            interactTicksLeft = 0;
        }

        /// <summary>Once per tick: a press that hasn't been used gets one tick older.</summary>
        public void AgeButtons() {
            if (interactTicksLeft > 0) {
                interactTicksLeft--;
            }
        }
    }
}
