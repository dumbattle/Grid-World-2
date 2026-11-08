namespace GridWorld {
    public static class CameraFunctions {
        /// <summary>The current camera controller, or null if there isn't one.</summary>
        public static ICameraController GetCameraController<T>(this T ctx)
                    where T : IWorldFunctionContext, IPerm_ReadCamera {
            return ctx.data.cameraController;
        }
    }
}
