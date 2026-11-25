using System;
using LPE.Animation;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// A looping list of sprite frames, each lasting a number of ticks. Played by LPE's
    /// <see cref="AnimationPlayer{TTarget}"/>.
    /// </summary>
    [Serializable]
    public class SpriteClip : IAnimationDefinition<SpriteRenderer> {
        [SerializeField] AnimationFrameDefinition[] frames = new AnimationFrameDefinition[0];

        public int numFrames => frames == null ? 0 : frames.Length;

        public SpriteClip() { }

        public SpriteClip(AnimationFrameDefinition[] frames) {
            this.frames = frames;
        }

        public int GetFrameDuration(int frame) {
            // A frame of 0 ticks would make LPE's player skip frames until its loop guard throws.
            return Mathf.Max(1, frames[frame].duration);
        }

        public AnimationFrameTags GetFrameTags(int frame) {
            return frames[frame].tags;
        }

        public void DrawFrame(int frame, SpriteRenderer target) {
            target.sprite = frames[frame].sprite;
        }

        public Sprite GetSprite(int frame) {
            return frames[frame].sprite;
        }
    }
}
