using System.Collections.Generic;
using UnityEngine;

namespace GridWorld {
    [CreateAssetMenu(fileName = "WorldData", menuName = "Grid World/World Data")]
    public class WorldDataSO : ScriptableObject {
        [SerializeField] WorldSettingsSO worldSettings;
        [SerializeField] TilePropertyTableSO tilePropertyTable;
        // The bake adds each area it bakes.
        [SerializeField] List<AreaDataSO> areaList = new List<AreaDataSO>();

        public WorldSettingsSO settings => worldSettings;
        public TilePropertyTableSO tileProperties => tilePropertyTable;
        public IReadOnlyList<AreaDataSO> areas => areaList;

        /// <summary>For the bake. Returns false if the area was already listed.</summary>
        public bool AddArea(AreaDataSO area) {
            if (area == null || areaList.Contains(area)) {
                return false;
            }
            areaList.Add(area);
            return true;
        }
    }
}
