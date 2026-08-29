using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using VContainer;

namespace ShadowInfection.UI.Nameplates
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(10000)]
    public sealed class NameplateLayerController : MonoBehaviour
    {
        [SerializeField] private StyleSheet nameplateStyles;

        private UIDocument uiDocument;
        private NameplateLayerView view;
        private NameplateLayerPresenter presenter;

        [Inject]
        internal void Construct(NameplateLayerPresenter injectedPresenter)
        {
            presenter = injectedPresenter;
        }

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument != null)
                uiDocument.sortingOrder = -1;
        }

        private void Start()
        {
            if (uiDocument == null)
            {
                UnityEngine.Debug.LogError("NameplateLayerController: UIDocument is missing.");
                return;
            }

            var root = uiDocument.rootVisualElement;
            if (root == null)
            {
                UnityEngine.Debug.LogError("NameplateLayerController: rootVisualElement is missing.");
                return;
            }

            if (presenter == null)
            {
                UnityEngine.Debug.LogError(
                    "NameplateLayerController: Presenter was not injected. Add NameplateLayerLifetimeScope.");
                return;
            }

            StretchToPanel(root);
            if (nameplateStyles != null && !root.styleSheets.Contains(nameplateStyles))
                root.styleSheets.Add(nameplateStyles);

            view = new NameplateLayerView(root);
            presenter.Bind(view, destroyCancellationToken);
            presenter.SetEnabled(isActiveAndEnabled);
        }

        private void Update()
        {
            presenter?.Tick(Time.deltaTime);
        }

        private void LateUpdate()
        {
            SyncWithWorldCamera(Camera.main);
        }

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += HandleBeginCameraRendering;
            presenter?.SetEnabled(true);
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= HandleBeginCameraRendering;
            presenter?.SetEnabled(false);
        }

        private void HandleBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera == null || camera.cameraType != CameraType.Game)
                return;

            if (camera != Camera.main)
                return;

            SyncWithWorldCamera(camera);
        }

        private void SyncWithWorldCamera(Camera camera)
        {
            if (camera != null)
                presenter?.SyncPositions(camera);
        }

        private void OnDestroy()
        {
            presenter?.Unbind();
        }

        private static void StretchToPanel(VisualElement root)
        {
            root.style.position = Position.Absolute;
            root.style.left = 0;
            root.style.top = 0;
            root.style.right = 0;
            root.style.bottom = 0;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);
        }
    }
}
