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

        public static bool IsInteractBuffered<T>(this T ctx)
                    where T : IWorldFunctionContext, IPerm_PlayerControl {
            return ctx.data.input.interactBuffered;
        }

        /// <summary>Uses up a buffered interact press, or drops it.</summary>
        public static void ClearInteractPress<T>(this T ctx)
                    where T : IWorldFunctionContext, IPerm_PlayerControl {
            ctx.data.input.ClearInteract();
        }

        /// <summary>Once per tick: unused button presses get one tick older.</summary>
        public static void AgeButtonPresses<T>(this T ctx)
                    where T : IWorldFunctionContext, IPerm_PlayerControl {
            ctx.data.input.AgeButtons();
        }

        public static PlayerControlState GetPlayerControl<T>(this T ctx)
                    where T : IWorldFunctionContext, IPerm_PlayerControl {
            return ctx.data.playerControl;
        }
    }
}
