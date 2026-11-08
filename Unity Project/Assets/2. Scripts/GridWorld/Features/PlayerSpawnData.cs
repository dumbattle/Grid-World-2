using System;
using LPE;
using UnityEngine;

namespace GridWorld {
    [Serializable]
    public sealed class PlayerSpawnData : WorldFeatureData {
        [SerializeField] EntityDefinitionSO entityDefinition;
        [SerializeField] Vector2Int localTile;
        [SerializeField] Direction4.Enum facingDirection;

        public EntityDefinitionSO definition => entityDefinition;
        public Vector2Int tile => localTile;
        public Direction4.Enum facing => facingDirection;

        public PlayerSpawnData(EntityDefinitionSO definition, Vector2Int tile, Direction4.Enum facing) {
            entityDefinition = definition;
            localTile = tile;
            facingDirection = facing;
        }
    }
}
