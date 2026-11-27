using System;
using System.Collections.Generic;
using LPE;
using UnityEngine;

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

        // Seeded, so a run can be repeated. Startup reseeds it from the world settings.
        public System.Random random = new System.Random(0);

        // Input, written by the root outside the phases
        public readonly PlayerInput input = new PlayerInput();

        // Set by the root. Cutscenes will swap it.
        public ICameraController cameraController;
        // Startup sets it from the world settings.
        public Vector2Int viewTiles = new Vector2Int(15, 11);

        // Spawn zones, in bake order. Each owns the list of mobs it spawned.
        public readonly List<SpawnZone> spawnZones = new List<SpawnZone>();

        // Owned by the decision phase
        public readonly PlayerControlState playerControl = new PlayerControlState();

        // Cutscenes, owned by the cutscene phase, in start order
        public readonly List<RunningCutscene> runningCutscenes = new List<RunningCutscene>();

        // Command queues
        // Decision -> Movement. One per entity, and the latest wins.
        public readonly Dictionary<EntityID, MoveRequest> id2moveRequest = new Dictionary<EntityID, MoveRequest>();
        // Anyone -> Cutscene phase. Started in order, and a request whose entities are locked is dropped.
        public readonly List<ICutscene> cutsceneRequests = new List<ICutscene>();

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
