using System;

namespace GridWorld {
    /// <summary>
    /// Turns one kind of baked feature record into world state at startup. Pass any dependencies to
    /// the constructor, and register an instance in the <see cref="WorldFeatureCatalogue"/>.
    /// </summary>
    public abstract class WorldFeatureType<TData> : IWorldFeatureRegistrar where TData : WorldFeatureData {
        Type IWorldFeatureRegistrar.dataType => typeof(TData);

        void IWorldFeatureRegistrar.RegisterRecord(WorldSetupContext ctx, AreaDataSO area, WorldFeatureData record) {
            Register(ctx, area, (TData)record);
        }

        /// <summary>Records hold area-local tiles. Convert them with <see cref="AreaDataSO.ToWorldTile"/>.</summary>
        protected abstract void Register(WorldSetupContext ctx, AreaDataSO area, TData data);
    }
}
