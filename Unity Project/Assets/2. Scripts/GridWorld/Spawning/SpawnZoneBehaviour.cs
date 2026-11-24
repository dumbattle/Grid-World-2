using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// A rectangle that keeps mobs alive while its area is active. Add a mob AI profile, such as
    /// <see cref="SpawnWanderAIBehaviour"/>, on the same object. A game can subclass this, override
    /// <see cref="Validate"/> and <see cref="CreateRecord"/>, and bake its own
    /// <see cref="SpawnZoneData"/> subclass.
    /// </summary>
    public class SpawnZoneBehaviour : IdentifiedFeatureBehaviour {
        [Tooltip("Relative to zone moves the region with this object. Area tiles keeps it in place.")]
        [SerializeField] TileSpace regionSpace = TileSpace.Area;
        [Tooltip("The tiles mobs spawn on and stay within. It must be inside the area.")]
        [SerializeField] RectInt region;
        [Tooltip("What each mob is.")]
        [SerializeField] EntityDefinitionSO definition;
        [Tooltip("The most mobs alive at once.")]
        [SerializeField, Min(1)] int maxCount = 3;
        [Tooltip("Ticks between spawns, and after a mob is removed. The game runs 60 ticks a second.")]
        [SerializeField, Min(0)] int respawnDelayTicks = 300;

        /// <summary>The region as set in the Inspector, in <see cref="regionTileSpace"/>.</summary>
        protected RectInt authoredRegion => region;
        protected TileSpace regionTileSpace => regionSpace;
        protected EntityDefinitionSO mobDefinition => definition;
        protected int maxMobs => maxCount;
        protected int respawnDelay => respawnDelayTicks;

        protected override IdentifiedFeatureData BakeRecord(WorldFeatureBaker baker) {
            if (!Validate(baker)) {
                return null;
            }

            var aiBehaviours = GetComponents<SpawnAIBehaviour>();
            if (aiBehaviours.Length > 1) {
                baker.LogError(this, $"Spawn zone '{name}' has {aiBehaviours.Length} mob AI profiles. A zone can have only one.");
                return null;
            }

            SpawnAIData ai = null;
            if (aiBehaviours.Length == 1) {
                ai = aiBehaviours[0].Bake(baker);
                if (ai == null) {
                    return null;
                }
            }

            var localRegion = TileSpaces.ToArea(region, regionSpace, baker.ToLocalTile(transform.position));
            return CreateRecord(localRegion, ai);
        }

        /// <summary>Logs an error with the baker and returns false if the settings are invalid.</summary>
        protected virtual bool Validate(WorldFeatureBaker baker) {
            if (region.width <= 0 || region.height <= 0) {
                baker.LogError(this, $"Spawn zone '{name}' has an empty region.");
                return false;
            }
            if (definition == null) {
                baker.LogError(this, $"Spawn zone '{name}' has no entity definition.");
                return false;
            }
            if (maxCount < 1) {
                baker.LogError(this, $"Spawn zone '{name}' needs a max count of at least 1.");
                return false;
            }
            if (respawnDelayTicks < 0) {
                baker.LogError(this, $"Spawn zone '{name}' has a negative respawn delay.");
                return false;
            }
            return true;
        }

        /// <param name="localRegion">The region in area-local tiles.</param>
        /// <param name="ai">Null when the zone has no mob AI profile.</param>
        protected virtual SpawnZoneData CreateRecord(RectInt localRegion, SpawnAIData ai) {
            return new SpawnZoneData(localRegion, definition, maxCount, respawnDelayTicks, ai);
        }

        // New zones start with a relative 5x5 region around the object.
        void Reset() {
            regionSpace = TileSpace.Relative;
            region = new RectInt(-2, -2, 5, 5);
        }

        void OnDrawGizmos() {
            Gizmos.color = new Color(1f, 0.55f, 0.2f);
            var shown = TileSpaces.ToArea(region, regionSpace, TileSpaces.TileOf(transform));
            Gizmos.DrawWireCube((Vector2)shown.position + (Vector2)shown.size / 2f, (Vector2)shown.size);
        }
    }
}
