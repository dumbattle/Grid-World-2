using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// The NPC walks a route of points. Each point shares a row or column with the next, and the NPC
    /// starts in line with the first.
    /// </summary>
    public class PatrolAIBehaviour : NpcAIBehaviour {
        [SerializeField] PatrolPointSettings[] points = new PatrolPointSettings[0];
        [Tooltip("Loop walks from the last point back to the first. Ping Pong walks the route in reverse.")]
        [SerializeField] PatrolMode mode = PatrolMode.Loop;

        public override NpcAddonData Bake(WorldFeatureBaker baker, Vector2Int localTile) {
            var tiles = new Vector2Int[points.Length];
            for (int i = 0; i < points.Length; i++) {
                tiles[i] = points[i].localTile;
            }

            var error = PatrolAI.FindRouteError(tiles, mode);
            if (error != null) {
                baker.LogError(this, $"NPC '{name}': {error}");
                return null;
            }
            if (localTile.x != tiles[0].x && localTile.y != tiles[0].y) {
                baker.LogError(this, $"NPC '{name}' at {localTile} doesn't share a row or column with its first patrol point {tiles[0]}.");
                return null;
            }
            return new PatrolAIData(pauses, points, mode);
        }

        void OnDrawGizmosSelected() {
            if (points == null || points.Length == 0) {
                return;
            }

            Gizmos.color = Color.cyan;
            var half = new Vector3(0.5f, 0.5f);
            for (int i = 0; i < points.Length; i++) {
                var position = (Vector3)(Vector2)points[i].localTile + half;
                Gizmos.DrawWireSphere(position, 0.2f);

                bool last = i == points.Length - 1;
                if (!last || mode == PatrolMode.Loop) {
                    var next = (Vector3)(Vector2)points[(i + 1) % points.Length].localTile + half;
                    Gizmos.DrawLine(position, next);
                }
            }
        }
    }
}
