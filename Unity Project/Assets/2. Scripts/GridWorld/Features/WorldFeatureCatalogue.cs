using System;
using System.Collections.Generic;

namespace GridWorld {
    /// <summary>
    /// The world feature types the root registers before the world loads, keyed by their data type.
    /// </summary>
    public class WorldFeatureCatalogue {
        Dictionary<Type, IWorldFeatureRegistrar> type2registrar = new Dictionary<Type, IWorldFeatureRegistrar>();

        public void Register<TData>(WorldFeatureType<TData> featureType) where TData : WorldFeatureData {
            if (featureType == null) {
                throw new ArgumentNullException(nameof(featureType));
            }

            var registrar = (IWorldFeatureRegistrar)featureType;
            if (type2registrar.TryGetValue(registrar.dataType, out var existing)) {
                throw new InvalidOperationException(
                    $"{registrar.dataType.Name} already has a world feature type: {existing.GetType().Name}. Can't also register {featureType.GetType().Name}.");
            }
            type2registrar[registrar.dataType] = registrar;
        }

        public bool Contains(Type dataType) {
            return type2registrar.ContainsKey(dataType);
        }

        internal IWorldFeatureRegistrar Get(Type dataType, string areaId) {
            if (!type2registrar.TryGetValue(dataType, out var registrar)) {
                throw new InvalidOperationException(
                    $"Area '{areaId}' has a {dataType.Name} record, but no world feature type is registered for it.");
            }
            return registrar;
        }
    }
}
