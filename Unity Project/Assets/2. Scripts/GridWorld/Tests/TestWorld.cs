using System;
using LPE;
using UnityEngine;

namespace GridWorld.Tests {
    /// <summary>
    /// A world with one open area from (-8,-8) to (7,7) and a default definition: 16 ticks to walk, 8 to run.
    /// </summary>
    class TestWorld : IDisposable {
        // No framework context can despawn, so tests use their own.
        class DespawnContext : WorldFunctionContext
                             , IPerm_DespawnEntities { }

        class StartCutsceneContext : WorldFunctionContext
                                   , IPerm_StartCutscenes { }

        public static readonly RectInt AreaBounds = new RectInt(-8, -8, 16, 16);

        public readonly WorldInstance world = new WorldInstance();
        public readonly EntityDefinitionSO definition = ScriptableObject.CreateInstance<EntityDefinitionSO>();
        readonly PhaseExecutionManager phases = new PhaseExecutionManager();

        public TestWorld() {
            var tiles = new ushort[AreaBounds.width * AreaBounds.height];
            for (int i = 0; i < tiles.Length; i++) {
                tiles[i] = TileBits.EnterAll;
            }
            world.GetContext<WorldSetupContext>().AddArea("test", new AreaTiles(AreaBounds, tiles));
        }

        public EntityID Spawn(Vector2Int tile, Direction4 facing) {
            return world.GetContext<WorldSetupContext>().SpawnEntity(definition, tile, facing);
        }

        public EntityID SpawnPlayer(Vector2Int tile, Direction4 facing) {
            var id = Spawn(tile, facing);
            world.GetContext<WorldSetupContext>().SetPlayer(id);
            return id;
        }

        public void AddAI(EntityID id, EntityAI ai) {
            world.GetContext<WorldSetupContext>().AddAI(id, ai);
        }

        public SpawnZone AddSpawnZone(SpawnZone zone) {
            world.GetContext<WorldSetupContext>().AddSpawnZone(zone);
            return zone;
        }

        public void Despawn(EntityID id) {
            world.GetContext<DespawnContext>().Despawn(id);
        }

        public bool Exists(EntityID id) {
            return world.entityIds.Exists(id);
        }

        /// <summary>The camera follows <paramref name="id"/> with a view of <paramref name="viewTiles"/>.</summary>
        public void FollowWithCamera(EntityID id, Vector2Int viewTiles) {
            world.GetContext<WorldSetupContext>().SetViewSize(viewTiles);
            world.cameraController = new FollowCameraController(id);
        }

        public void SetAreaActive(bool active) {
            world.GetContext<AreaActivationContext>().SetAreaActive(0, active);
        }

        public void Request(EntityID id, Direction4 direction, bool turnOnly = false, bool run = false) {
            world.GetContext<DecisionPhaseContext>().RequestMove(id, direction, turnOnly, run);
        }

        /// <summary>Queues a cutscene, which starts on the next tick.</summary>
        public void StartCutscene(string id, CutsceneStart start = default) {
            world.GetContext<StartCutsceneContext>().StartCutscene(id, start);
        }

        public bool IsLocked(EntityID id) {
            return world.components.scripted.Has(id);
        }

        public void Tick(int count = 1) {
            for (int i = 0; i < count; i++) {
                phases.Tick(world);
            }
        }

        public Vector2Int TileOf(EntityID id) {
            return world.components.tile.Get(id);
        }

        public Direction4 FacingOf(EntityID id) {
            return world.components.facing.Get(id);
        }

        public Step StepOf(EntityID id) {
            return world.components.step.Get(id);
        }

        public void Dispose() {
            UnityEngine.Object.DestroyImmediate(definition);
        }
    }
}
