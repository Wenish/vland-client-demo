using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace ShadowInfection.UI.ZombieGameOver
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class ZombieGameOverController : MonoBehaviour
    {
        private UIDocument uiDocument;
        private ZombieGameOverView view;
        private ZombieGameOverPresenter presenter;

        [Inject]
        internal void Construct(ZombieGameOverPresenter injectedPresenter)
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
                UnityEngine.Debug.LogError("ZombieGameOverController: UIDocument root is missing.");
                return;
            }

            if (presenter == null)
            {
                UnityEngine.Debug.LogError(
                    "ZombieGameOverController: Presenter was not injected. Add GameLifetimeScope and ZombieGameOverLifetimeScope.");
                return;
            }

            view = new ZombieGameOverView(uiDocument.rootVisualElement);
            presenter.Bind(view, destroyCancellationToken);
        }

        private void OnDestroy()
        {
            presenter?.Unbind();
        }
    }
}
