using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Where the player starts. A world needs exactly one.
    /// </summary>
    public class PlayerSpawnBehaviour : WorldFeatureBehaviour {
        [SerializeField] EntityDefinitionSO definition;
        [SerializeField] Direction4.Enum facing = Direction4.Enum.down;

        public override void Bake(WorldFeatureBaker baker) {
            if (definition == null) {
                baker.LogError(this, $"Player spawn '{name}' has no entity definition.");
                return;
            }
            if (facing == Direction4.Enum.none) {
                baker.LogError(this, $"Player spawn '{name}' has no facing direction.");
                return;
            }
            baker.Add(new PlayerSpawnData(definition, baker.ToLocalTile(transform.position), facing));
        }

        void OnDrawGizmos() {
            var center = new Vector3(Mathf.Floor(transform.position.x) + 0.5f, Mathf.Floor(transform.position.y) + 0.5f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(center, new Vector3(0.9f, 0.9f));
            Gizmos.DrawLine(center, center + (Vector3)(Vector2)((Direction4)facing ?? Direction4.down).ToVector2Int() * 0.45f);
        }
    }
}
