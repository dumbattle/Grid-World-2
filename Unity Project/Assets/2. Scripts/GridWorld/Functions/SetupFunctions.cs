using System;
using LPE;
using UnityEngine;

namespace GridWorld {
    public static class SetupFunctions {
        public static void AddArea<T>(this T ctx, string areaId, AreaTiles area)
                    where T : IWorldFunctionContext, IPerm_SetupWorld {
            if (area == null) {
                throw new ArgumentNullException(nameof(area));
            }
            ctx.data.areas.Add(areaId, area.bounds);
            ctx.data.tiles.AddArea(area);
        }

        public static void SetPlayer<T>(this T ctx, EntityID id)
                    where T : IWorldFunctionContext, IPerm_SetupWorld {
            ctx.data.player = id;
        }

        public static void AddSpawnZone<T>(this T ctx, SpawnZone zone)
                    where T : IWorldFunctionContext, IPerm_SetupWorld {
            if (zone == null) {
                throw new ArgumentNullException(nameof(zone));
            }
            ctx.data.spawnZones.Add(zone);
        }

        public static void SeedRandom<T>(this T ctx, int seed)
                    where T : IWorldFunctionContext, IPerm_SetupWorld {
            ctx.data.random = new System.Random(seed);
        }

        public static void SetViewSize<T>(this T ctx, Vector2Int viewTiles)
                    where T : IWorldFunctionContext, IPerm_SetupWorld {
            if (viewTiles.x <= 0 || viewTiles.y <= 0) {
                throw new ArgumentOutOfRangeException(nameof(viewTiles), $"The view needs a positive size, not {viewTiles}.");
            }
            ctx.data.viewTiles = viewTiles;
        }
    }
}
