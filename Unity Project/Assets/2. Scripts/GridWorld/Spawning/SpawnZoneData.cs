using System;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// A baked spawn zone. A game can subclass it to add settings, override <see cref="CreateZone"/>
    /// to return its own <see cref="SpawnZone"/>, and register a
    /// <see cref="SpawnZoneFeatureType{TData}"/> for the subclass.
    /// </summary>
    [Serializable]
    public class SpawnZoneData : IdentifiedFeatureData {
        [SerializeField] RectInt localRegion;
        [SerializeField] EntityDefinitionSO entityDefinition;
        [SerializeField] int maxMobs;
        [SerializeField] int respawnDelay;
        // Tells a missing AI class apart from a zone with no AI, since both load as null.
        [SerializeField] bool hasAI;
        [SerializeReference] SpawnAIData aiRecord;

        /// <summary>In area-local tiles.</summary>
        public RectInt region => localRegion;
        public EntityDefinitionSO definition => entityDefinition;
        public int maxCount => maxMobs;
        public int respawnDelayTicks => respawnDelay;
        public SpawnAIData ai => aiRecord;
        public bool isAIMissing => hasAI && aiRecord == null;

        public SpawnZoneData(RectInt localRegion, EntityDefinitionSO definition, int maxCount, int respawnDelayTicks, SpawnAIData ai) {
            this.localRegion = localRegion;
            entityDefinition = definition;
            maxMobs = maxCount;
            respawnDelay = respawnDelayTicks;
            hasAI = ai != null;
            aiRecord = ai;
        }

        public virtual SpawnZone CreateZone(AreaDataSO area) {
            return new SpawnZone(ToWorldRegion(area), definition, maxCount, respawnDelayTicks, ai);
        }

        protected RectInt ToWorldRegion(AreaDataSO area) {
            return new RectInt(area.ToWorldTile(localRegion.position), localRegion.size);
        }
    }
}
