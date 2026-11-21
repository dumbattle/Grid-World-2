using UnityEngine;

namespace GridWorld {
    /// <summary>An NPC's AI profile. An NPC has at most one.</summary>
    public abstract class NpcAIBehaviour : NpcAddonBehaviour {
        [Tooltip("Stop deciding while the NPC's area is inactive.")]
        [SerializeField] bool pauseWhenAreaInactive = true;

        protected bool pauses => pauseWhenAreaInactive;
    }
}
