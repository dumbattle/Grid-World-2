using GridWorld;
using LPE;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game {
    /// <summary>
    /// The test world's root. Adds debug keys for the entity the player faces: one despawns it,
    /// standing in for defeating a mob, and one plays a test cutscene with it.
    /// </summary>
    public class TestWorldRootBehaviour : WorldRootBehaviour {
        [Tooltip("Despawns the entity the player faces.")]
        [SerializeField] InputActionReference despawnAction;
        [Tooltip("Plays the test cutscene with the entity the player faces.")]
        [SerializeField] InputActionReference cutsceneAction;

        // Read each frame, used on the next tick, so a press isn't lost on a frame with no tick.
        bool despawnPressed;
        bool cutscenePressed;

        protected override void OnEnable() {
            base.OnEnable();
            if (despawnAction != null) {
                despawnAction.action.Enable();
            }
            if (cutsceneAction != null) {
                cutsceneAction.action.Enable();
            }
        }

        protected override void RegisterCutscenes(CutsceneCatalogue catalogue) {
            TestCutscenes.Register(catalogue);
        }

        protected override void StoreInput() {
            base.StoreInput();
            if (despawnAction != null && despawnAction.action.WasPressedThisFrame()) {
                despawnPressed = true;
            }
            if (cutsceneAction != null && cutsceneAction.action.WasPressedThisFrame()) {
                cutscenePressed = true;
            }
        }

        protected override void TickWorld() {
            var ctx = world.GetContext<TestDebugContext>();
            if (despawnPressed) {
                despawnPressed = false;
                DespawnFacedEntity(ctx);
            }
            if (cutscenePressed) {
                cutscenePressed = false;
                StartFacedCutscene(ctx);
            }
            base.TickWorld();
        }

        static void DespawnFacedEntity(TestDebugContext ctx) {
            var id = GetFacedEntity(ctx, out var target);
            if (id.IsValid()) {
                ctx.Despawn(id);
                Debug.Log($"Grid World test: despawned {id} at {target}.");
            }
        }

        static void StartFacedCutscene(TestDebugContext ctx) {
            var id = GetFacedEntity(ctx, out _);
            if (id.IsValid()) {
                ctx.StartCutscene(TestCutscenes.FaceAndBackAway, new CutsceneStart(ctx.GetPlayer(), id));
            }
        }

        static EntityID GetFacedEntity(TestDebugContext ctx, out Vector2Int target) {
            var player = ctx.GetPlayer();
            if (!player.IsValid()) {
                target = default;
                return EntityID.INVALID;
            }
            target = ctx.GetTile(player) + ctx.GetFacing(player).ToVector2Int();
            return ctx.GetEntityAt(target);
        }
    }
}
