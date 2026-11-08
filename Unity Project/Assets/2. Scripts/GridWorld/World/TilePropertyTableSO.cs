using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GridWorld {
    /// <summary>
    /// Maps each tile asset to the tile data the bake writes for it. Works with any tile class,
    /// including rule tiles, so tiles don't need subclassing.
    /// </summary>
    [CreateAssetMenu(fileName = "TilePropertyTable", menuName = "Grid World/Tile Property Table")]
    public class TilePropertyTableSO : ScriptableObject {
        [Serializable]
        struct Entry {
            public TileBase tile;
            public bool enterUp;
            public bool enterDown;
            public bool enterLeft;
            public bool enterRight;
            [Range(0, 255)] public int gameBits;

            public ushort value {
                get {
                    int bits = gameBits << 8;
                    if (enterUp) {
                        bits |= TileBits.EnterUp;
                    }
                    if (enterDown) {
                        bits |= TileBits.EnterDown;
                    }
                    if (enterLeft) {
                        bits |= TileBits.EnterLeft;
                    }
                    if (enterRight) {
                        bits |= TileBits.EnterRight;
                    }
                    return (ushort)bits;
                }
            }
        }

        [SerializeField] List<Entry> entries = new List<Entry>();

        Dictionary<TileBase, ushort> tile2value;

        public bool TryGetValue(TileBase tile, out ushort value) {
            if (tile2value == null) {
                tile2value = new Dictionary<TileBase, ushort>();
                foreach (var entry in entries) {
                    if (entry.tile != null) {
                        tile2value[entry.tile] = entry.value;
                    }
                }
            }
            return tile2value.TryGetValue(tile, out value);
        }

        /// <summary>Adds or replaces a tile's entry. For editor tools that build tables.</summary>
        public void SetEntry(TileBase tile, ushort value) {
            var entry = new Entry {
                tile = tile,
                enterUp = (value & TileBits.EnterUp) != 0,
                enterDown = (value & TileBits.EnterDown) != 0,
                enterLeft = (value & TileBits.EnterLeft) != 0,
                enterRight = (value & TileBits.EnterRight) != 0,
                gameBits = value >> 8,
            };

            int index = entries.FindIndex(e => e.tile == tile);
            if (index >= 0) {
                entries[index] = entry;
            }
            else {
                entries.Add(entry);
            }
            tile2value = null;
        }

        void OnValidate() {
            tile2value = null;
        }
    }
}
