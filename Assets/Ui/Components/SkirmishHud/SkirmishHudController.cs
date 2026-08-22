using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace ShadowInfection.UI.SkirmishHud
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class SkirmishHudController : MonoBehaviour
    {
        [SerializeField] private bool hideWhenClientInactive = true;

        private UIDocument uiDocument;
        private SkirmishHudView view;
        private SkirmishHudPresenter presenter;

        [Inject]
        internal void Construct(SkirmishHudPresenter injectedPresenter)
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
                UnityEngine.Debug.LogError("SkirmishHudController: UIDocument root is missing.");
                return;
            }

            if (presenter == null)
            {
                UnityEngine.Debug.LogError(
                    "SkirmishHudController: Presenter was not injected. Add GameLifetimeScope and SkirmishHudLifetimeScope.");
                return;
            }

            view = new SkirmishHudView(uiDocument.rootVisualElement);
            presenter.Bind(view, hideWhenClientInactive, destroyCancellationToken);
        }

        private void OnDestroy()
        {
            presenter?.Unbind();
        }
    }
}
