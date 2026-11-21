using UnityEngine;

namespace GridWorld {
    [CreateAssetMenu(fileName = "EntityDefinition", menuName = "Grid World/Entity Definition")]
    public class EntityDefinitionSO : ScriptableObject {
        [SerializeField, Min(1)] int walkTicksPerTile = 16;
        [SerializeField, Min(1)] int runTicksPerTile = 8;
        [Tooltip("Tints the generated placeholder sprite, until the entity has real art.")]
        [SerializeField] Color placeholderColor = new Color32(232, 112, 72, 255);

        public int walkTicks => walkTicksPerTile;
        public int runTicks => runTicksPerTile;
        public Color placeholderTint => placeholderColor;
    }
}
