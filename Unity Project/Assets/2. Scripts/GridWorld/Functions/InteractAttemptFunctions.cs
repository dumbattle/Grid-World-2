using System.Collections.Generic;

namespace GridWorld {
    public static class InteractAttemptFunctions {
        /// <summary>Last tick's interact presses, in the order they were made.</summary>
        public static IReadOnlyList<InteractAttempt> GetInteractAttempts<T>(this T ctx)
                    where T : IWorldFunctionContext, IPerm_ReadInteractAttempts {
            return ctx.data.interactAttempts;
        }

        public static void RecordInteractAttempt<T>(this T ctx, InteractAttempt attempt)
                    where T : IWorldFunctionContext, IPerm_WriteInteractAttempts {
            ctx.data.interactAttempts.Add(attempt);
        }

        /// <summary>Clears last tick's attempts, once every reader has seen them.</summary>
        public static void ClearInteractAttempts<T>(this T ctx)
                    where T : IWorldFunctionContext, IPerm_ClearInteractAttempts {
            ctx.data.interactAttempts.Clear();
        }
    }
}
