using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GridWorld {
    /// <summary>
    /// Every feature ID the Editor can see: the features in the open scenes, and the baked records of
    /// areas whose scenes aren't open. Rebuilt lazily after any change in the Editor.
    /// </summary>
    public sealed class FeatureIdIndex {
        public enum Status { Ok, Missing, BadFormat, Duplicate }

        public struct Entry {
            public string id;
            public string areaId;
            /// <summary>Null for a baked record from a scene that isn't open.</summary>
            public IdentifiedFeatureBehaviour feature;
            public string kind;
            public string name;
            public Vector2Int tile;
            public string scenePath;
            public Status status;

            public bool isOpen => feature != null;
            /// <summary>The feature's name in messages.</summary>
            public string label => isOpen ? $"'{name}'" : $"a baked {kind}";
        }

        static FeatureIdIndex current;

        /// <summary>Raised when the index goes stale, so windows can repaint.</summary>
        public static event Action changed;

        public static FeatureIdIndex Current {
            get {
                if (current == null) {
                    current = Build();
                }
                return current;
            }
        }

        readonly List<Entry> entryList = new List<Entry>();
        readonly Dictionary<string, int> id2count = new Dictionary<string, int>(FeatureIds.comparer);

        /// <summary>Open scenes first, in scene and hierarchy order, then baked records.</summary>
        public IReadOnlyList<Entry> entries => entryList;

        [InitializeOnLoadMethod]
        static void Subscribe() {
            ObjectChangeEvents.changesPublished += (ref ObjectChangeEventStream stream) => Invalidate();
            EditorSceneManager.sceneOpened += (scene, mode) => Invalidate();
            EditorSceneManager.sceneClosed += scene => Invalidate();
            Undo.undoRedoPerformed += Invalidate;
        }

        public static void Invalidate() {
            current = null;
            changed?.Invoke();
        }

        /// <summary>"NPC", "Spawn Zone", or "Player Spawn", from a behaviour or record type.</summary>
        public static string KindName(Type type) {
            var name = type.Name;
            foreach (var suffix in new[] { "Behaviour", "Data" }) {
                if (name.EndsWith(suffix) && name.Length > suffix.Length) {
                    name = name.Substring(0, name.Length - suffix.Length);
                }
            }
            return ObjectNames.NicifyVariableName(name).Replace("Npc", "NPC");
        }

        static FeatureIdIndex Build() {
            var index = new FeatureIdIndex();
            var openScenePaths = new HashSet<string>();
            var worlds = new HashSet<WorldDataSO>();

            for (int i = 0; i < SceneManager.sceneCount; i++) {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) {
                    continue;
                }
                openScenePaths.Add(scene.path);

                var area = FindAreaQuietly(scene);
                if (area != null && area.world != null) {
                    worlds.Add(area.world);
                }
                var areaId = area != null ? area.areaId : "";
                foreach (var feature in FeatureIdChecks.InScene(scene)) {
                    index.entryList.Add(new Entry {
                        id = feature.featureId,
                        areaId = areaId,
                        feature = feature,
                        kind = KindName(feature.GetType()),
                        name = feature.name,
                        tile = TileSpaces.TileOf(feature.transform),
                        scenePath = scene.path,
                    });
                }
            }

            foreach (var world in worlds) {
                foreach (var data in world.areas) {
                    if (data == null || openScenePaths.Contains(data.scenePath)) {
                        continue;
                    }
                    foreach (var record in data.featureRecords) {
                        if (record is IdentifiedFeatureData identified) {
                            index.entryList.Add(new Entry {
                                id = identified.id,
                                areaId = data.areaId,
                                kind = KindName(record.GetType()),
                                name = "",
                                tile = data.ToWorldTile(TileOf(record)),
                                scenePath = data.scenePath,
                            });
                        }
                    }
                }
            }

            foreach (var entry in index.entryList) {
                if (entry.id.Length > 0) {
                    index.id2count.TryGetValue(entry.id, out int count);
                    index.id2count[entry.id] = count + 1;
                }
            }
            for (int i = 0; i < index.entryList.Count; i++) {
                var entry = index.entryList[i];
                entry.status = index.StatusOf(entry.id);
                index.entryList[i] = entry;
            }
            return index;
        }

        // The record's area-local tile. A zone uses its region's corner.
        static Vector2Int TileOf(WorldFeatureData record) {
            switch (record) {
                case NpcData npc:
                    return npc.tile;
                case PlayerSpawnData spawn:
                    return spawn.tile;
                case SpawnZoneData zone:
                    return zone.region.position;
                default:
                    return Vector2Int.zero;
            }
        }

        // Unlike AreaBaker.FindArea, this doesn't log when a scene has two areas.
        static AreaBehaviour FindAreaQuietly(Scene scene) {
            foreach (var root in scene.GetRootGameObjects()) {
                var area = root.GetComponentInChildren<AreaBehaviour>(true);
                if (area != null) {
                    return area;
                }
            }
            return null;
        }

        Status StatusOf(string id) {
            if (id.Length == 0) {
                return Status.Missing;
            }
            if (FeatureIds.FindFormatError(id) != null) {
                return Status.BadFormat;
            }
            return IsDuplicated(id) ? Status.Duplicate : Status.Ok;
        }

        public bool IsDuplicated(string id) {
            return id.Length > 0 && id2count.TryGetValue(id, out int count) && count > 1;
        }

        /// <summary>Every entry with this ID, ignoring case, in index order.</summary>
        public List<Entry> WithId(string id) {
            var found = new List<Entry>();
            foreach (var entry in entryList) {
                if (FeatureIds.Same(entry.id, id)) {
                    found.Add(entry);
                }
            }
            return found;
        }

        /// <summary>
        /// What's wrong with giving <paramref name="feature"/> this ID, or null if it's valid and no
        /// other feature uses it.
        /// </summary>
        public string FindProblem(string id, IdentifiedFeatureBehaviour feature) {
            var formatError = FeatureIds.FindFormatError(id);
            if (formatError != null) {
                return formatError;
            }
            foreach (var entry in entryList) {
                if (entry.feature != feature && FeatureIds.Same(entry.id, id)) {
                    var where = entry.areaId.Length > 0 ? $" in area '{entry.areaId}'" : " outside any area";
                    return $"'{id}' is also used by {entry.label}{where}.";
                }
            }
            return null;
        }

        /// <summary>Whether this feature's own ID is missing, badly formed, or used by another feature.</summary>
        public bool HasProblem(IdentifiedFeatureBehaviour feature) {
            return FindProblem(feature.featureId, feature) != null;
        }
    }
}
