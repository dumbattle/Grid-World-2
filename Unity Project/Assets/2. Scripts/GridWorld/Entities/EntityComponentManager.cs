using LPE;
using UnityEngine;

namespace GridWorld {
    /// <summary>
    /// Owns the framework's component stores. <see cref="Register{T}"/> is the only way to add a
    /// component, including to a store owned by a game.
    /// </summary>
    public class EntityComponentManager {
        public readonly EntityComponent<Vector2Int> tile = new EntityComponent<Vector2Int>();
        public readonly EntityComponent<Direction4> facing = new EntityComponent<Direction4>();
        public readonly EntityComponent<EntityDefinitionSO> definition = new EntityComponent<EntityDefinitionSO>();

        public void Register<T>(EntityComponent<T> component, EntityID id, T value) {
            component.Add(id, value);
        }
    }
}
