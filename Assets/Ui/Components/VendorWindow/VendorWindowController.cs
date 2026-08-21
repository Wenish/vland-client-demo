using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using Vland.UI;

namespace ShadowInfection.UI.VendorWindow
{
    [DisallowMultipleComponent]
    public sealed class VendorWindowController : MonoBehaviour
    {
        private const int VendorSortingOrder = 70;

        public static VendorWindowController Instance { get; private set; }

        public UIDocument uiDocument;
        public StyleSheet vendorWindowUss;

        private VendorView view;
        private VendorWindowPresenter presenter;

        public bool IsOpen => presenter != null && presenter.IsOpen;

        [Inject]
        internal void Construct(VendorWindowPresenter injectedPresenter)
        {
            presenter = injectedPresenter;
        }

        private void Awake()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                UnityEngine.Debug.LogError("VendorWindowController: UIDocument missing.");
                return;
            }

            uiDocument.sortingOrder = VendorSortingOrder;
            Instance = this;
        }

        private void Start()
        {
            if (uiDocument == null || uiDocument.rootVisualElement == null)
            {
                UnityEngine.Debug.LogError("VendorWindowController: UIDocument root is missing.");
                return;
            }

            if (presenter == null)
            {
                UnityEngine.Debug.LogError(
                    "VendorWindowController: Presenter was not injected. Add GameLifetimeScope and VendorWindowLifetimeScope.");
                return;
            }

            var root = uiDocument.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            if (vendorWindowUss != null)
                root.styleSheets.Add(vendorWindowUss);

            var vendorRoot = root.Q<VisualElement>("vendorRoot") ?? root;
            UiCursorRefresh.ScheduleForRoot(vendorRoot, VendorSortingOrder);

            view = new VendorView(vendorRoot);
            presenter.Bind(view, destroyCancellationToken);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            presenter?.Unbind();
            view?.Dispose();
        }

        public void Open(VendorDefinition catalog)
        {
            presenter?.Open(catalog);
        }

        public void Open(InteractionZone zone, PlayerController player)
        {
            presenter?.Open(zone, player);
        }

        public void Close()
        {
            presenter?.Close();
        }

        public void CloseIfZone(InteractionZone zone)
        {
            presenter?.CloseIfZone(zone);
        }
    }
}
