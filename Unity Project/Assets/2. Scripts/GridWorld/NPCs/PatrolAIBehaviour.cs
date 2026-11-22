using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// The NPC walks a route of points. Each point's tile is an offset from the NPC's tile, so the
    /// route moves with the NPC. Each point shares a row or column with the next, and the first
    /// shares one with the NPC.
    /// </summary>
    public class PatrolAIBehaviour : NpcAIBehaviour {
        [Tooltip("Tiles are offsets from the NPC's tile.")]
        [SerializeField] PatrolPointSettings[] points = new PatrolPointSettings[0];
        [Tooltip("Loop walks from the last point back to the first. Ping Pong walks the route in reverse.")]
        [SerializeField] PatrolMode mode = PatrolMode.Loop;

        public override NpcAddonData Bake(WorldFeatureBaker baker, Vector2Int localTile) {
            var areaPoints = new PatrolPointSettings[points.Length];
            var tiles = new Vector2Int[points.Length];
            for (int i = 0; i < points.Length; i++) {
                areaPoints[i] = points[i].Offset(localTile);
                tiles[i] = areaPoints[i].localTile;
            }

            var error = PatrolAI.FindRouteError(tiles, mode);
            if (error != null) {
                baker.LogError(this, $"NPC '{name}' at {localTile}: {error} Patrol points are offsets from the NPC's tile.");
                return null;
            }
            if (points[0].localTile.x != 0 && points[0].localTile.y != 0) {
                baker.LogError(this, $"NPC '{name}' doesn't share a row or column with its first patrol point, at offset {points[0].localTile}. One of the offset's values must be 0.");
                return null;
            }
            return new PatrolAIData(pauses, areaPoints, mode);
        }

        void OnDrawGizmosSelected() {
            if (points == null || points.Length == 0) {
                return;
            }

            Gizmos.color = Color.cyan;
            var origin = (Vector3)(Vector2)TileSpaces.TileOf(transform) + new Vector3(0.5f, 0.5f);
            for (int i = 0; i < points.Length; i++) {
                var position = origin + (Vector3)(Vector2)points[i].localTile;
                Gizmos.DrawWireSphere(position, 0.2f);

                bool last = i == points.Length - 1;
                if (!last || mode == PatrolMode.Loop) {
                    var next = origin + (Vector3)(Vector2)points[(i + 1) % points.Length].localTile;
                    Gizmos.DrawLine(position, next);
                }
            }
        }
    }
}
