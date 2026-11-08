using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Decides where the camera looks. Positions are in world units, where a tile spans from its
    /// coordinate to its coordinate plus one.
    /// </summary>
    public interface ICameraController {
        /// <summary>Once per tick. Active areas are measured from it.</summary>
        Vector2 GetTarget(RenderContext ctx);

        /// <summary>Once per frame. The point the camera centres on.</summary>
        Vector2 GetViewPosition(RenderContext ctx, float alpha);
    }
}
