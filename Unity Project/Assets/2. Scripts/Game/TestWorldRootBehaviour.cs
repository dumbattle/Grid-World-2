using GridWorld;
using LPE;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game {
    /// <summary>
    /// The test world's root. Adds a debug key that despawns the entity the player faces, standing
    /// in for defeating a mob.
    /// </summary>
    public class TestWorldRootBehaviour : WorldRootBehaviour {
        [Tooltip("Despawns the entity the player faces.")]
        [SerializeField] InputActionReference despawnAction;

        // Read each frame, used on the next tick, so a press isn't lost on a frame with no tick.
        bool despawnPressed;

        protected override void OnEnable() {
            base.OnEnable();
            if (despawnAction != null) {
                despawnAction.action.Enable();
            }
        }

        protected override void StoreInput() {
            base.StoreInput();
            if (despawnAction != null && despawnAction.action.WasPressedThisFrame()) {
                despawnPressed = true;
            }
        }

        protected override void TickWorld() {
            if (despawnPressed) {
                despawnPressed = false;
                DespawnFacedEntity(world.GetContext<TestDebugContext>());
            }
            base.TickWorld();
        }

        static void DespawnFacedEntity(TestDebugContext ctx) {
            var player = ctx.GetPlayer();
            if (!player.IsValid()) {
                return;
            }

            var target = ctx.GetTile(player) + ctx.GetFacing(player).ToVector2Int();
            var id = ctx.GetEntityAt(target);
            if (id.IsValid()) {
                ctx.Despawn(id);
                Debug.Log($"Grid World test: despawned {id} at {target}.");
            }
        }
    }
}
