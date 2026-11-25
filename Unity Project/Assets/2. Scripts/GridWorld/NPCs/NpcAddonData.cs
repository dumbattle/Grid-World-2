using System;
using LPE;

namespace GridWorld {
    /// <summary>
    /// One add-on's part of a baked NPC record, such as its AI. At startup, after the NPC's entity
    /// exists, <see cref="Register"/> adds this add-on's components to it. Renaming or moving a
    /// subclass breaks the records already baked, so rebake after.
    /// </summary>
    [Serializable]
    public abstract class NpcAddonData {
        /// <summary>
        /// Runs before the NPC spawns, so a failed check leaves nothing half registered. Throw for
        /// mistakes that would break the NPC, and log a warning for ones that are only suspicious.
        /// <paramref name="npc"/> describes the NPC, for starting messages.
        /// </summary>
        public virtual void Validate(WorldSetupContext ctx, AreaDataSO area, string npc) {
        }

        /// <summary>Records hold area-local tiles. Convert them with <see cref="AreaDataSO.ToWorldTile"/>.</summary>
        public abstract void Register(WorldSetupContext ctx, AreaDataSO area, EntityID id);
    }
}
