using UnityEngine;

namespace GridWorld {
    public static class AreaFunctions {
        public static int GetAreaCount<T>(this T ctx)
                    where T : IWorldFunctionContext, IPerm_ReadAreas {
            return ctx.data.areas.count;
        }

        public static string GetAreaId<T>(this T ctx, int index)
                    where T : IWorldFunctionContext, IPerm_ReadAreas {
            return ctx.data.areas.GetId(index);
        }

        public static RectInt GetAreaBounds<T>(this T ctx, int index)
                    where T : IWorldFunctionContext, IPerm_ReadAreas {
            return ctx.data.areas.GetBounds(index);
        }

        public static bool IsAreaActive<T>(this T ctx, int index)
                    where T : IWorldFunctionContext, IPerm_ReadAreas {
            return ctx.data.areas.IsActive(index);
        }

        public static void SetAreaActive<T>(this T ctx, int index, bool active)
                    where T : IWorldFunctionContext, IPerm_UpdateAreas {
            ctx.data.areas.SetActive(index, active);
        }
    }
}
