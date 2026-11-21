using System;
using System.Collections.Generic;
using LPE;
using UnityEngine;

namespace GridWorld {
    [Serializable]
    public sealed class NpcData : WorldFeatureData {
        [SerializeField] EntityDefinitionSO entityDefinition;
        [SerializeField] Vector2Int localTile;
        [SerializeField] Direction4.Enum facingDirection;
        [SerializeReference] List<NpcAddonData> addonRecords = new List<NpcAddonData>();

        public EntityDefinitionSO definition => entityDefinition;
        public Vector2Int tile => localTile;
        public Direction4.Enum facing => facingDirection;
        public IReadOnlyList<NpcAddonData> addons => addonRecords;

        public NpcData(EntityDefinitionSO definition, Vector2Int tile, Direction4.Enum facing, List<NpcAddonData> addons) {
            entityDefinition = definition;
            localTile = tile;
            facingDirection = facing;
            addonRecords = addons ?? new List<NpcAddonData>();
        }
    }
}
