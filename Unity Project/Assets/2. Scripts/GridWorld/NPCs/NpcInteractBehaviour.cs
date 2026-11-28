using UnityEngine;

namespace GridWorld {
    /// <summary>Plays a cutscene when the player faces the NPC and presses interact, wherever the NPC has walked to.</summary>
    [DisallowMultipleComponent]
    public class NpcInteractBehaviour : NpcAddonBehaviour {
        [Tooltip("Plays when the player faces this NPC and presses interact.")]
        [SerializeReference, CutscenePicker] ICutscene cutscene;

        public override NpcAddonData Bake(WorldFeatureBaker baker, Vector2Int localTile) {
            if (cutscene == null) {
                baker.LogError(this, $"NPC '{name}' has an interact add-on with no cutscene.");
                return null;
            }
            return new NpcInteractData(CutsceneCopy.Of(cutscene));
        }
    }
}
