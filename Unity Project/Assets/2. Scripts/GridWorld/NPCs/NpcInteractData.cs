using System;
using LPE;
using UnityEngine;

namespace GridWorld {
    [Serializable]
    public sealed class NpcInteractData : NpcAddonData {
        [SerializeReference] ICutscene interaction;

        public ICutscene cutscene => interaction;

        public NpcInteractData(ICutscene cutscene) {
            interaction = cutscene;
        }

        public override void Validate(WorldSetupContext ctx, AreaDataSO area, string npc) {
            if (interaction == null) {
                // Also what a renamed, moved, or deleted cutscene class leaves behind.
                throw new InvalidOperationException($"{npc} has an interact add-on with no cutscene, or its cutscene's class is missing. Rebake the area.");
            }
        }

        public override void Register(WorldSetupContext ctx, AreaDataSO area, EntityID id) {
            if (!ctx.TryAddEntityInteraction(id, interaction)) {
                throw new InvalidOperationException($"Area '{area.areaId}' gives {id} a second interaction. An entity can have only one.");
            }
        }
    }
}
