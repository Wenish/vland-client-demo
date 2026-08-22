using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace ShadowInfection.UI.CastleSiegeHud
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class CastleSiegeHudController : MonoBehaviour
    {
        [SerializeField] private bool hideWhenClientInactive = true;

        private UIDocument uiDocument;
        private CastleSiegeHudView view;
        private CastleSiegeHudPresenter presenter;

        [Inject]
        internal void Construct(CastleSiegeHudPresenter injectedPresenter)
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
                UnityEngine.Debug.LogError("CastleSiegeHudController: UIDocument root is missing.");
                return;
            }

            if (presenter == null)
            {
                UnityEngine.Debug.LogError(
                    "CastleSiegeHudController: Presenter was not injected. Add GameLifetimeScope and CastleSiegeHudLifetimeScope.");
                return;
            }

            view = new CastleSiegeHudView(uiDocument.rootVisualElement);
            presenter.Bind(view, hideWhenClientInactive, destroyCancellationToken);
        }

        private void OnDestroy()
        {
            presenter?.Unbind();
        }
    }
}
