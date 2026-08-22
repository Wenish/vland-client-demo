using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace ShadowInfection.UI.ZombieLeaderboard
{
    [DisallowMultipleComponent]
    public sealed class ZombieLeaderboardController : MonoBehaviour
    {
        private UIDocument uiDocument;
        private ZombieLeaderboardView view;
        private ZombieLeaderboardPresenter presenter;

        [Inject]
        internal void Construct(ZombieLeaderboardPresenter injectedPresenter)
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
                UnityEngine.Debug.LogError("ZombieLeaderboardController: UIDocument root is missing.");
                return;
            }

            if (presenter == null)
            {
                UnityEngine.Debug.LogError(
                    "ZombieLeaderboardController: Presenter was not injected. Add GameLifetimeScope and ZombieLeaderboardLifetimeScope.");
                return;
            }

            view = new ZombieLeaderboardView(uiDocument.rootVisualElement);
            presenter.Bind(view, destroyCancellationToken);
        }

        private void OnDestroy()
        {
            presenter?.Unbind();
        }
    }
}
