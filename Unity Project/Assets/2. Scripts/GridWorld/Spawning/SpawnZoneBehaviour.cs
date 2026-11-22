using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// A rectangle that keeps mobs alive while its area is active. Add a mob AI profile, such as
    /// <see cref="SpawnWanderAIBehaviour"/>, on the same object. A game can subclass this, override
    /// <see cref="Validate"/> and <see cref="CreateRecord"/>, and bake its own
    /// <see cref="SpawnZoneData"/> subclass.
    /// </summary>
    public class SpawnZoneBehaviour : WorldFeatureBehaviour {
        [Tooltip("The tiles mobs spawn on and stay within, in area-local tiles. It must be inside the area.")]
        [SerializeField] RectInt region;
        [Tooltip("What each mob is.")]
        [SerializeField] EntityDefinitionSO definition;
        [Tooltip("The most mobs alive at once.")]
        [SerializeField, Min(1)] int maxCount = 3;
        [Tooltip("Ticks between spawns, and after a mob is removed. The game runs 60 ticks a second.")]
        [SerializeField, Min(0)] int respawnDelayTicks = 300;

        protected RectInt localRegion => region;
        protected EntityDefinitionSO mobDefinition => definition;
        protected int maxMobs => maxCount;
        protected int respawnDelay => respawnDelayTicks;

        public override void Bake(WorldFeatureBaker baker) {
            if (!Validate(baker)) {
                return;
            }

            var aiBehaviours = GetComponents<SpawnAIBehaviour>();
            if (aiBehaviours.Length > 1) {
                baker.LogError(this, $"Spawn zone '{name}' has {aiBehaviours.Length} mob AI profiles. A zone can have only one.");
                return;
            }

            SpawnAIData ai = null;
            if (aiBehaviours.Length == 1) {
                ai = aiBehaviours[0].Bake(baker);
                if (ai == null) {
                    return;
                }
            }

            baker.Add(CreateRecord(ai));
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

        /// <param name="ai">Null when the zone has no mob AI profile.</param>
        protected virtual SpawnZoneData CreateRecord(SpawnAIData ai) {
            return new SpawnZoneData(region, definition, maxCount, respawnDelayTicks, ai);
        }

        // Starts the region as 5x5 tiles around the object.
        void Reset() {
            var tile = new Vector2Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y));
            region = new RectInt(tile - new Vector2Int(2, 2), new Vector2Int(5, 5));
        }

        void OnDrawGizmos() {
            Gizmos.color = new Color(1f, 0.55f, 0.2f);
            Gizmos.DrawWireCube((Vector2)region.position + (Vector2)region.size / 2f, (Vector2)region.size);
        }
    }
}
