using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace ShadowInfection.UI.PlayerHud
{
    [DisallowMultipleComponent]
    public sealed class PlayerHudController : MonoBehaviour
    {
        public Color castSuccessColor = Color.green;
        [SerializeField] private Texture2D aimCursorTexture;
        [SerializeField] private Texture2D hoverCursorTexture;
        [SerializeField] private Texture2D uiCursorTexture;

        private UIDocument uiDocument;
        private PlayerHudView view;
        private PlayerHudPresenter presenter;

        [Inject]
        internal void Construct(PlayerHudPresenter injectedPresenter)
        {
            presenter = injectedPresenter;
        }

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void Start()
        {
            if (uiDocument == null)
            {
                UnityEngine.Debug.LogError("PlayerHudController: UIDocument is missing.");
                return;
            }

            var root = uiDocument.rootVisualElement;
            if (root == null)
            {
                UnityEngine.Debug.LogError("PlayerHudController: UIDocument rootVisualElement is missing.");
                return;
            }

            if (presenter == null)
            {
                UnityEngine.Debug.LogError(
                    "PlayerHudController: Presenter was not injected. Add GameLifetimeScope and PlayerHudLifetimeScope to the HUD.");
                return;
            }

            RegisterCursors();
            UiCursorRefresh.ScheduleForRoot(root);
            view = new PlayerHudView(root);
            view.LoadoutButtonClicked += OnLoadoutButtonClicked;
            view.SetLoadoutButtonVisible(LoadoutWindowController.Instance != null);
            presenter.Bind(view, castSuccessColor, destroyCancellationToken);
            presenter.SetEnabled(isActiveAndEnabled);
        }

        private void OnEnable()
        {
            presenter?.SetEnabled(true);
        }

        private void OnDisable()
        {
            presenter?.SetEnabled(false);
        }

        private void OnDestroy()
        {
            if (view != null)
                view.LoadoutButtonClicked -= OnLoadoutButtonClicked;

            UiCursorRefresh.SetGameplayPointerEnabled(false);
            presenter?.SetEnabled(false);
            presenter?.Unbind();
        }

        private static void OnLoadoutButtonClicked()
        {
            var loadout = LoadoutWindowController.Instance;
            if (loadout == null)
                return;

            loadout.SetOpen(true);
        }

        private void RegisterCursors()
        {
            if (aimCursorTexture == null && hoverCursorTexture == null && uiCursorTexture == null)
                return;

            UiCursorRefresh.Configure(aimCursorTexture, hoverCursorTexture, uiCursorTexture);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            if (aimCursorTexture == null)
            {
                aimCursorTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Assets/Art/Cursors/CursorPointer_32.png");
            }

            if (hoverCursorTexture == null)
            {
                hoverCursorTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Assets/Art/Cursors/CursorHover_32.png");
            }

            if (uiCursorTexture == null)
            {
                uiCursorTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Assets/Art/Cursors/CursorDefaultAlternativ_32.png");
            }
        }
#endif
    }
}
