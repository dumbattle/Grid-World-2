using UnityEngine;

namespace GridWorld {
    [CreateAssetMenu(fileName = "EntityDefinition", menuName = "Grid World/Entity Definition")]
    public class EntityDefinitionSO : ScriptableObject {
        [SerializeField, Min(1)] int walkTicksPerTile = 16;
        [SerializeField, Min(1)] int runTicksPerTile = 8;

        public int walkTicks => walkTicksPerTile;
        public int runTicks => runTicksPerTile;
    }
}
