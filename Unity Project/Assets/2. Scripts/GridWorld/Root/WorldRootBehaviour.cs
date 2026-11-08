using LPE;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GridWorld {
    /// <summary>
    /// The trusted root, and the only holder of <see cref="WorldInstance"/>. Builds the world on
    /// start, then each frame stores input, runs whole ticks, and draws. A game that needs more
    /// writes its own root.
    /// </summary>
    public class WorldRootBehaviour : MonoBehaviour {
        // A stick has to pass this to count as a held direction.
        const float DirectionThreshold = 0.5f;

        [SerializeField] WorldDataSO worldData;
        [SerializeField] Camera viewCamera;
        [SerializeField] InputActionReference moveAction;
        [SerializeField] InputActionReference runAction;

        WorldInstance world;
        PhaseExecutionManager phases = new PhaseExecutionManager();
        TickClock clock = new TickClock();
        RenderContext render;
        AreaActivationContext activation;
        AreaActivationSystem areaActivation;
        AreaSceneLoader sceneLoader;
        WorldView view;
        CameraView cameraView;

        void OnEnable() {
            EnableAction(moveAction);
            EnableAction(runAction);
        }

        void Start() {
            var instance = new WorldInstance();
            var catalogue = new WorldFeatureCatalogue();
            catalogue.Register(new PlayerSpawnFeatureType());
            WorldStartup.Load(instance.GetContext<WorldSetupContext>(), worldData, catalogue);

            if (instance.player.IsValid()) {
                instance.cameraController = new FollowCameraController(instance.player);
            }

            world = instance;
            render = world.GetContext<RenderContext>();
            activation = world.GetContext<AreaActivationContext>();
            areaActivation = new AreaActivationSystem(worldData.settings);
            sceneLoader = new AreaSceneLoader(render, worldData.areas);
            view = new WorldView(transform);
            cameraView = new CameraView(viewCamera, worldData.settings);

            // Activate the starting areas now, so their scenes start loading on the first frame.
            UpdateActiveAreas();
            view.Tick(render);
        }

        void Update() {
            if (world == null) {
                return;
            }

            StoreInput();

            int ticks = clock.Advance(Time.deltaTime);
            for (int i = 0; i < ticks; i++) {
                phases.Tick(world);
                UpdateActiveAreas();
                view.Tick(render);
            }

            sceneLoader.Update(render);
            view.Render(render, clock.alpha);
            cameraView.Render(render, clock.alpha);
        }

        void OnDestroy() {
            view?.Dispose();
        }

        void StoreInput() {
            var input = world.input;
            var move = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;

            input.SetDirectionHeld(Direction4.up, move.y > DirectionThreshold);
            input.SetDirectionHeld(Direction4.down, move.y < -DirectionThreshold);
            input.SetDirectionHeld(Direction4.left, move.x < -DirectionThreshold);
            input.SetDirectionHeld(Direction4.right, move.x > DirectionThreshold);
            input.runHeld = runAction != null && runAction.action.IsPressed();
        }

        void UpdateActiveAreas() {
            var controller = world.cameraController;
            if (controller != null) {
                areaActivation.Run(activation, controller.GetTarget(render));
            }
        }

        static void EnableAction(InputActionReference reference) {
            if (reference != null) {
                reference.action.Enable();
            }
        }
    }
}
