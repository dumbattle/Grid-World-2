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
        /// <summary>Records hold area-local tiles. Convert them with <see cref="AreaDataSO.ToWorldTile"/>.</summary>
        public abstract void Register(WorldSetupContext ctx, AreaDataSO area, EntityID id);
    }
}
