using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// An entity's idle and walk clips, one per facing direction. Set on an
    /// <see cref="EntityDefinitionSO"/>; an entity without one draws a placeholder.
    /// </summary>
    [CreateAssetMenu(fileName = "EntityAnimation", menuName = "Grid World/Entity Animation")]
    public class EntityAnimationSO : ScriptableObject {
        [SerializeField] DirectionalClips idleClips = new DirectionalClips();
        [SerializeField] DirectionalClips walkClips = new DirectionalClips();

        public DirectionalClips idle => idleClips;
        public DirectionalClips walk => walkClips;

        public SpriteClip Get(EntityAnimationState state, Direction4 facing) {
            return state == EntityAnimationState.Walk ? walkClips.Get(facing) : idleClips.Get(facing);
        }
    }
}
