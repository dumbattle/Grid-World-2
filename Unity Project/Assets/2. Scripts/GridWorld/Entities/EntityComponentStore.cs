using LPE;

namespace GridWorld {
    /// <summary>
    /// The untyped side of a component store, so <see cref="EntityComponentManager"/> can remove a
    /// despawned entity from every store, including those a game adds with
    /// <see cref="EntityComponentManager.AddStore"/>.
    /// </summary>
    public abstract class EntityComponentStore {
        internal abstract void Remove(EntityID id);
    }
}
