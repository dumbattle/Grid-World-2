using LPE;

namespace GridWorld {
    public static class PlayerControlFunctions {
        /// <summary>The most recently pressed direction that's still held, or <c>null</c>.</summary>
        public static Direction4 GetHeldDirection<T>(this T ctx)
                    where T : IWorldFunctionContext, IPerm_PlayerControl {
            return ctx.data.input.direction;
        }

        public static bool IsRunHeld<T>(this T ctx)
                    where T : IWorldFunctionContext, IPerm_PlayerControl {
            return ctx.data.input.runHeld;
        }

        public static PlayerControlState GetPlayerControl<T>(this T ctx)
                    where T : IWorldFunctionContext, IPerm_PlayerControl {
            return ctx.data.playerControl;
        }
    }
}
