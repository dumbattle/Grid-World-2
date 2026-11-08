using System;

namespace GridWorld {
    // Lets the catalogue hold feature types of any data type without exposing an untyped entry point.
    interface IWorldFeatureRegistrar {
        Type dataType { get; }
        void RegisterRecord(WorldSetupContext ctx, AreaDataSO area, WorldFeatureData record);
    }
}
