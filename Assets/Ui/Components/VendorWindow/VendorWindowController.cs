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
        [SerializeField] private Texture2D tradeCursorTexture;

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
            UiCursorRefresh.SetTradeCursor(tradeCursorTexture);
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

        public void Open(IVendorSession session, PlayerController player)
        {
            presenter?.Open(session, player);
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

        public void CloseIfInteractable(IVendorInteractable interactable)
        {
            presenter?.CloseIfInteractable(interactable);
        }

        public void CloseIfZone(InteractionZone zone)
        {
            presenter?.CloseIfInteractable(zone);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            if (tradeCursorTexture == null)
            {
                tradeCursorTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Assets/Art/Cursors/CursorTrade_32.png");
            }
        }
#endif
    }
}
