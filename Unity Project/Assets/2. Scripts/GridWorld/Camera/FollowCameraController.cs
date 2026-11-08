using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Keeps an entity exactly centred, using the same blended position as its sprite.
    /// </summary>
    public class FollowCameraController : ICameraController {
        static readonly Vector2 tileCentre = new Vector2(0.5f, 0.5f);

        public readonly EntityID target;

        public FollowCameraController(EntityID target) {
            this.target = target;
        }

        public Vector2 GetTarget(RenderContext ctx) {
            return (Vector2)ctx.GetTile(target) + tileCentre;
        }

        public Vector2 GetViewPosition(RenderContext ctx, float alpha) {
            return ctx.GetBlendedPosition(target, alpha) + tileCentre;
        }
    }
}
