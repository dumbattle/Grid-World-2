using System;
using LPE;
using UnityEngine;

namespace GridWorld.Tests {
    /// <summary>
    /// A world with one open area from (-8,-8) to (7,7) and a default definition: 16 ticks to walk, 8 to run.
    /// </summary>
    class TestWorld : IDisposable {
        public static readonly RectInt AreaBounds = new RectInt(-8, -8, 16, 16);

        public readonly WorldInstance world = new WorldInstance();
        public readonly EntityDefinitionSO definition = ScriptableObject.CreateInstance<EntityDefinitionSO>();
        readonly PhaseExecutionManager phases = new PhaseExecutionManager();

        public TestWorld() {
            var tiles = new ushort[AreaBounds.width * AreaBounds.height];
            for (int i = 0; i < tiles.Length; i++) {
                tiles[i] = TileBits.EnterAll;
            }
            world.GetContext<WorldSetupContext>().AddArea(new AreaTiles(AreaBounds, tiles));
        }

        public EntityID Spawn(Vector2Int tile, Direction4 facing) {
            return world.GetContext<WorldSetupContext>().SpawnEntity(definition, tile, facing);
        }

        public EntityID SpawnPlayer(Vector2Int tile, Direction4 facing) {
            var id = Spawn(tile, facing);
            world.GetContext<WorldSetupContext>().SetPlayer(id);
            return id;
        }

        public void Request(EntityID id, Direction4 direction, bool turnOnly = false, bool run = false) {
            world.GetContext<DecisionPhaseContext>().RequestMove(id, direction, turnOnly, run);
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
