using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace ShadowInfection.UI.InGameMenu
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class InGameMenuController : MonoBehaviour
    {
        private const int MenuSortingOrder = 100;

        private UIDocument uiDocument;
        private InGameMenuView view;
        private InGameMenuPresenter presenter;

        [Inject]
        internal void Construct(InGameMenuPresenter injectedPresenter)
        {
            presenter = injectedPresenter;
        }

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument != null)
                uiDocument.sortingOrder = MenuSortingOrder;
        }

        private void Start()
        {
            if (uiDocument == null || uiDocument.rootVisualElement == null)
            {
                UnityEngine.Debug.LogError("InGameMenuController: UIDocument root is missing.");
                return;
            }

            if (presenter == null)
            {
                UnityEngine.Debug.LogError(
                    "InGameMenuController: Presenter was not injected. Add GameLifetimeScope and InGameMenuLifetimeScope.");
                return;
            }

            view = new InGameMenuView(uiDocument.rootVisualElement);
            if (view.Root != null)
            {
                UiPointerState.RegisterBlockingElement(view.Root);
                UiCursorRefresh.ScheduleForRoot(view.Root, MenuSortingOrder);
            }

            presenter.Bind(view, destroyCancellationToken);
        }

        private void OnDestroy()
        {
            if (view != null && view.Root != null)
                UiPointerState.UnregisterBlockingElement(view.Root);

            presenter?.Unbind();
        }
    }
}
