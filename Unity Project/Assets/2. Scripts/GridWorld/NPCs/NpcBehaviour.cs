using System.Collections.Generic;
using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// An NPC placed in an area scene. Add an AI profile, such as <see cref="WanderAIBehaviour"/>, on
    /// the same object to make it move. With none, it stands still.
    /// </summary>
    public class NpcBehaviour : WorldFeatureBehaviour {
        [SerializeField] EntityDefinitionSO definition;
        [SerializeField] Direction4.Enum facing = Direction4.Enum.down;

        public override void Bake(WorldFeatureBaker baker) {
            if (definition == null) {
                baker.LogError(this, $"NPC '{name}' has no entity definition.");
                return;
            }
            if (facing == Direction4.Enum.none) {
                baker.LogError(this, $"NPC '{name}' has no facing direction.");
                return;
            }

            var addonBehaviours = GetComponents<NpcAddonBehaviour>();
            int aiCount = 0;
            foreach (var addon in addonBehaviours) {
                if (addon is NpcAIBehaviour) {
                    aiCount++;
                }
            }
            if (aiCount > 1) {
                baker.LogError(this, $"NPC '{name}' has {aiCount} AI profiles. An NPC can have only one.");
                return;
            }

            var tile = baker.ToLocalTile(transform.position);
            var addons = new List<NpcAddonData>();
            foreach (var addon in addonBehaviours) {
                var record = addon.Bake(baker, tile);
                if (record == null) {
                    return;
                }
                addons.Add(record);
            }

            baker.Add(new NpcData(definition, tile, facing, addons));
        }

        void OnDrawGizmos() {
            var center = new Vector3(Mathf.Floor(transform.position.x) + 0.5f, Mathf.Floor(transform.position.y) + 0.5f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(center, new Vector3(0.9f, 0.9f));
            Gizmos.DrawLine(center, center + (Vector3)(Vector2)((Direction4)facing ?? Direction4.down).ToVector2Int() * 0.45f);
        }
    }
}
