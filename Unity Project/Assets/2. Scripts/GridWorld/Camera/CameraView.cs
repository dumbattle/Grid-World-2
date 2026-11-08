using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Moves the Unity camera to the current controller's view position. Call <see cref="Render"/>
    /// once per frame, after the sprites.
    /// </summary>
    public class CameraView {
        Camera camera;

        public CameraView(Camera camera, WorldSettingsSO settings) {
            this.camera = camera;
            camera.orthographic = true;
            camera.orthographicSize = settings.viewTiles.y / 2f;
        }

        public void Render(RenderContext ctx, float alpha) {
            var controller = ctx.GetCameraController();
            if (controller == null) {
                return;
            }

            var position = controller.GetViewPosition(ctx, alpha);
            var transform = camera.transform;
            transform.position = new Vector3(position.x, position.y, transform.position.z);
        }
    }
}
