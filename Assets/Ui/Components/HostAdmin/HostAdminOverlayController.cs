using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace ShadowInfection.UI.HostAdmin
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class HostAdminOverlayController : MonoBehaviour
    {
        [SerializeField] private bool hideWhenNotHost = true;

        private UIDocument uiDocument;
        private HostAdminOverlayView view;
        private HostAdminOverlayPresenter presenter;

        [Inject]
        internal void Construct(HostAdminOverlayPresenter injectedPresenter)
        {
            presenter = injectedPresenter;
        }

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void Start()
        {
            if (uiDocument == null || uiDocument.rootVisualElement == null)
            {
                UnityEngine.Debug.LogError("HostAdminOverlayController: UIDocument root is missing.");
                return;
            }

            if (presenter == null)
            {
                UnityEngine.Debug.LogError(
                    "HostAdminOverlayController: Presenter was not injected. Add GameLifetimeScope and HostAdminLifetimeScope.");
                return;
            }

            view = new HostAdminOverlayView(uiDocument.rootVisualElement);
            presenter.Bind(view, hideWhenNotHost, destroyCancellationToken);
        }

        private void OnDestroy()
        {
            presenter?.Unbind();
        }
    }
}
