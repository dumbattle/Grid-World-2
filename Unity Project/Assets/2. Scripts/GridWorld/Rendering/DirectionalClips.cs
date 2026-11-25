using System;
using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>One <see cref="SpriteClip"/> for each facing direction.</summary>
    [Serializable]
    public class DirectionalClips {
        [SerializeField] SpriteClip up = new SpriteClip();
        [SerializeField] SpriteClip down = new SpriteClip();
        [SerializeField] SpriteClip left = new SpriteClip();
        [SerializeField] SpriteClip right = new SpriteClip();

        /// <summary>The clip for a direction, or the down clip for anything that isn't one.</summary>
        public SpriteClip Get(Direction4 facing) {
            if (facing == Direction4.up) {
                return up;
            }
            if (facing == Direction4.left) {
                return left;
            }
            if (facing == Direction4.right) {
                return right;
            }
            return down;
        }
    }
}
