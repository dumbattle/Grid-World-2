namespace GridWorld {
    /// <summary>What an authored tile or region is measured from.</summary>
    public enum TileSpace {
        /// <summary>Area-local tiles. First, so components saved before the setting existed load in this space.</summary>
        Area,
        /// <summary>An offset from the authoring object's own tile, so the shape moves with the object.</summary>
        Relative,
    }
}
