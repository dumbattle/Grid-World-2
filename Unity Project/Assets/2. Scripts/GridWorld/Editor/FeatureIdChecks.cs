using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace GridWorld {
    /// <summary>
    /// Checks the IDs of one area's features before the bake. A missing ID is a warning. A bad
    /// format or a duplicate is an error, and the area doesn't bake.
    /// </summary>
    public static class FeatureIdChecks {
        public struct Problem {
            public IdentifiedFeatureBehaviour feature;
            public string message;
            public bool isError;
        }

        /// <summary>Every identified feature in the scene, including inactive ones, in hierarchy order.</summary>
        public static List<IdentifiedFeatureBehaviour> InScene(Scene scene) {
            var features = new List<IdentifiedFeatureBehaviour>();
            foreach (var root in scene.GetRootGameObjects()) {
                features.AddRange(root.GetComponentsInChildren<IdentifiedFeatureBehaviour>(true));
            }
            return features;
        }

        public static List<Problem> Find(IReadOnlyList<IdentifiedFeatureBehaviour> features) {
            var problems = new List<Problem>();
            var id2feature = new Dictionary<string, IdentifiedFeatureBehaviour>(FeatureIds.comparer);

            foreach (var feature in features) {
                var id = feature.featureId;
                if (id.Length == 0) {
                    problems.Add(new Problem {
                        feature = feature,
                        message = $"'{feature.name}' has no ID. Give it one with Grid World → Fix Feature IDs.",
                    });
                    continue;
                }

                var formatError = FeatureIds.FindFormatError(id);
                if (formatError != null) {
                    problems.Add(new Problem { feature = feature, message = $"'{feature.name}' has the ID '{id}'. {formatError}", isError = true });
                    continue;
                }

                if (id2feature.TryGetValue(id, out var first)) {
                    problems.Add(new Problem {
                        feature = feature,
                        message = $"'{first.name}' and '{feature.name}' share the ID '{id}'. Give one a new ID with Grid World → Fix Feature IDs.",
                        isError = true,
                    });
                    continue;
                }
                id2feature.Add(id, feature);
            }
            return problems;
        }
    }
}
