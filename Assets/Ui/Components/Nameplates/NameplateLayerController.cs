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

        private void OnEnable()
        {
            RenderPipelineManager.beginContextRendering += HandleBeginContextRendering;
            presenter?.SetEnabled(true);
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginContextRendering -= HandleBeginContextRendering;
            presenter?.SetEnabled(false);
        }

        private void HandleBeginContextRendering(ScriptableRenderContext context, System.Collections.Generic.List<Camera> cameras)
        {
            Camera worldCamera = null;
            for (var i = 0; i < cameras.Count; i++)
            {
                var camera = cameras[i];
                if (camera != null && camera.cameraType == CameraType.Game)
                {
                    worldCamera = camera;
                    break;
                }
            }

            if (worldCamera == null)
                worldCamera = Camera.main;

            if (worldCamera != null)
                presenter?.SyncPositions(worldCamera);
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
