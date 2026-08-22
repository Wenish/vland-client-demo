using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace ShadowInfection.UI.LoadoutWindow
{
    [DisallowMultipleComponent]
    public sealed class LoadoutWindowController : MonoBehaviour
    {
        private const int LoadoutSortingOrder = 60;

        public UIDocument uiDocument;
        public VisualTreeAsset loadoutPanelUxml;
        public StyleSheet loadoutWindowUss;
        public StyleSheet loadoutPanelUss;

        private LoadoutView view;
        private LoadoutWindowPresenter presenter;

        [Inject]
        internal void Construct(LoadoutWindowPresenter injectedPresenter)
        {
            presenter = injectedPresenter;
        }

        private void Awake()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                UnityEngine.Debug.LogError("LoadoutWindowController: UIDocument missing.");
                return;
            }

            uiDocument.sortingOrder = LoadoutSortingOrder;
        }

        private void Start()
        {
            if (uiDocument == null || uiDocument.rootVisualElement == null)
            {
                UnityEngine.Debug.LogError("LoadoutWindowController: UIDocument root is missing.");
                return;
            }

            if (presenter == null)
            {
                UnityEngine.Debug.LogError(
                    "LoadoutWindowController: Presenter was not injected. Add GameLifetimeScope and LoadoutWindowLifetimeScope.");
                return;
            }

            var root = uiDocument.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            if (loadoutWindowUss != null)
                root.styleSheets.Add(loadoutWindowUss);
            if (loadoutPanelUss != null)
                root.styleSheets.Add(loadoutPanelUss);
            if (loadoutPanelUxml != null)
                loadoutPanelUxml.CloneTree(root);

            var loadoutRoot = root.Q<VisualElement>("loadoutRoot") ?? root;
            UiCursorRefresh.ScheduleForRoot(loadoutRoot, LoadoutSortingOrder);

            view = new LoadoutView(loadoutRoot);
            presenter.Bind(view, destroyCancellationToken);
        }

        private void OnDestroy()
        {
            presenter?.Unbind();
            view?.Dispose();
        }
    }
}
