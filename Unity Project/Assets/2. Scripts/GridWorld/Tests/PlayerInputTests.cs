using LPE;
using NUnit.Framework;

namespace GridWorld.Tests {
    public class PlayerInputTests {
        [Test]
        public void Direction_NothingHeld_ReturnsNull() {
            Assert.IsNull(new PlayerInput().direction);
        }

        [Test]
        public void Direction_TwoHeld_ReturnsMostRecentlyPressed() {
            var input = new PlayerInput();

            input.SetDirectionHeld(Direction4.up, true);
            input.SetDirectionHeld(Direction4.right, true);

            Assert.AreSame(Direction4.right, input.direction);
        }

        [Test]
        public void Direction_MostRecentReleased_ReturnsEarlierHeld() {
            var input = new PlayerInput();
            input.SetDirectionHeld(Direction4.up, true);
            input.SetDirectionHeld(Direction4.right, true);

            input.SetDirectionHeld(Direction4.right, false);

            Assert.AreSame(Direction4.up, input.direction);
        }

        [Test]
        public void SetDirectionHeld_AlreadyHeld_KeepsPressOrder() {
            var input = new PlayerInput();
            input.SetDirectionHeld(Direction4.up, true);
            input.SetDirectionHeld(Direction4.right, true);

            // The root reports every held key each frame, which mustn't reorder them.
            input.SetDirectionHeld(Direction4.up, true);

            Assert.AreSame(Direction4.right, input.direction);
        }
    }
}
