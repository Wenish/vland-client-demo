using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace ShadowInfection.UI.InventoryWindow
{
    [DisallowMultipleComponent]
    public sealed class InventoryWindowController : MonoBehaviour
    {
        private const int InventorySortingOrder = 65;

        public UIDocument uiDocument;
        public VisualTreeAsset inventoryPanelUxml;
        public StyleSheet inventoryWindowUss;

        private InventoryView view;
        private InventoryWindowPresenter presenter;

        [Inject]
        internal void Construct(InventoryWindowPresenter injectedPresenter)
        {
            presenter = injectedPresenter;
        }

        private void Awake()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                UnityEngine.Debug.LogError("InventoryWindowController: UIDocument missing.");
                return;
            }

            uiDocument.sortingOrder = InventorySortingOrder;
        }

        private void Start()
        {
            if (uiDocument == null || uiDocument.rootVisualElement == null)
            {
                UnityEngine.Debug.LogError("InventoryWindowController: UIDocument root is missing.");
                return;
            }

            if (presenter == null)
            {
                UnityEngine.Debug.LogError(
                    "InventoryWindowController: Presenter was not injected. Add GameLifetimeScope and InventoryWindowLifetimeScope.");
                return;
            }

            var root = uiDocument.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            if (inventoryWindowUss != null)
                root.styleSheets.Add(inventoryWindowUss);
            if (inventoryPanelUxml != null)
                inventoryPanelUxml.CloneTree(root);

            var inventoryRoot = root.Q<VisualElement>("inventoryRoot") ?? root;
            UiCursorRefresh.ScheduleForRoot(inventoryRoot, InventorySortingOrder);

            view = new InventoryView(inventoryRoot);
            presenter.Bind(view, destroyCancellationToken);
        }

        private void OnDestroy()
        {
            presenter?.Unbind();
            view?.Dispose();
        }
    }
}
