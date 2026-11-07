using System;
using LPE;

namespace GridWorld {
    /// <summary>
    /// The framework's bits in a tile's <c>ushort</c>. The low 8 bits belong to the framework and the
    /// high 8 to the game. An entry bit says whether a step moving in that direction may enter the tile.
    /// </summary>
    public static class TileBits {
        public const ushort EnterUp = 1 << 0;
        public const ushort EnterDown = 1 << 1;
        public const ushort EnterLeft = 1 << 2;
        public const ushort EnterRight = 1 << 3;

        public const ushort EnterAll = EnterUp | EnterDown | EnterLeft | EnterRight;
        public const ushort FrameworkMask = 0x00FF;

        public static ushort EnterBit(Direction4 direction) {
            if (direction == Direction4.up) {
                return EnterUp;
            }
            if (direction == Direction4.down) {
                return EnterDown;
            }
            if (direction == Direction4.left) {
                return EnterLeft;
            }
            if (direction == Direction4.right) {
                return EnterRight;
            }
            throw new ArgumentNullException(nameof(direction));
        }
    }
}
