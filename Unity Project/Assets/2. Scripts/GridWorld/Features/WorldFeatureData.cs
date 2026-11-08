using System;

namespace GridWorld {
    /// <summary>
    /// A baked world feature record, stored by reference on its area's data asset so it can hold
    /// asset references. Each subclass needs a <see cref="WorldFeatureType{TData}"/> registered in the
    /// catalogue. Renaming or moving a subclass breaks the records already baked, so rebake after.
    /// </summary>
    [Serializable]
    public abstract class WorldFeatureData { }
}
