using System;
using System.Collections.Generic;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Every area's ID, world bounds, and active flag, by index in the order startup added them.
    /// Logic reads the active flag, never whether the area's scene is actually loaded.
    /// </summary>
    public class WorldAreas {
        List<string> ids = new List<string>();
        List<RectInt> bounds = new List<RectInt>();
        List<bool> active = new List<bool>();

        public int count => ids.Count;

        public void Add(string id, RectInt worldBounds) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentException("An area needs an ID.", nameof(id));
            }
            if (ids.Contains(id)) {
                throw new InvalidOperationException($"Area '{id}' was added twice.");
            }

            ids.Add(id);
            bounds.Add(worldBounds);
            active.Add(false);
        }

        public string GetId(int index) {
            return ids[index];
        }

        public RectInt GetBounds(int index) {
            return bounds[index];
        }

        public bool IsActive(int index) {
            return active[index];
        }

        public void SetActive(int index, bool value) {
            active[index] = value;
        }

        /// <summary>The index of the area holding <paramref name="tile"/>, or -1 if none does.</summary>
        public int IndexOf(Vector2Int tile) {
            for (int i = 0; i < bounds.Count; i++) {
                if (bounds[i].Contains(tile)) {
                    return i;
                }
            }
            return -1;
        }
    }
}
