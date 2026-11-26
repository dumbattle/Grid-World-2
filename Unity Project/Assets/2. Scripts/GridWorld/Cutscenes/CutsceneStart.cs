using LPE;

namespace GridWorld {
    /// <summary>
    /// Who a cutscene is for, passed to its build function. For an interaction, the instigator is
    /// the player and the target is what they interacted with. Either can be invalid.
    /// </summary>
    public readonly struct CutsceneStart {
        public readonly EntityID instigator;
        public readonly EntityID target;

        public CutsceneStart(EntityID instigator, EntityID target) {
            this.instigator = instigator;
            this.target = target;
        }
    }
}
