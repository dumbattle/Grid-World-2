using System;
using System.Collections.Generic;
using LPE;

namespace GridWorld {
    /// <summary>
    /// Owns all overworld state. Holding it means full authority, so only the trusted root keeps a
    /// reference. Systems receive contexts from <see cref="GetContext{T}"/> instead.
    /// </summary>
    public class WorldInstance {
        // Entities
        public readonly EntityIDManager entityIds = new EntityIDManager();
        public readonly EntityComponentManager components = new EntityComponentManager();
        public EntityID player = EntityID.INVALID;

        // Tiles
        public readonly WorldTiles tiles = new WorldTiles();
        public readonly TileOccupancy occupancy = new TileOccupancy();

        // Areas, with active flags updated once per tick after the phases
        public readonly WorldAreas areas = new WorldAreas();

        // Input, written by the root outside the phases
        public readonly PlayerInput input = new PlayerInput();

        // Set by the root. Cutscenes will swap it.
        public ICameraController cameraController;

        // Owned by the decision phase
        public readonly PlayerControlState playerControl = new PlayerControlState();

        // Command queues
        // Decision -> Movement. One per entity, and the latest wins.
        public readonly Dictionary<EntityID, MoveRequest> id2moveRequest = new Dictionary<EntityID, MoveRequest>();

        Dictionary<Type, IWorldFunctionContext> type2context = new Dictionary<Type, IWorldFunctionContext>();

        public T GetContext<T>() where T : IWorldFunctionContext, new() {
            if (type2context.TryGetValue(typeof(T), out var existing)) {
                return (T)existing;
            }

            var context = new T();
            context.Init(this);
            type2context[typeof(T)] = context;
            return context;
        }
    }
}
