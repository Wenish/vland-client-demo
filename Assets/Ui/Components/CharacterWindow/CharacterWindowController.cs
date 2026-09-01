using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace ShadowInfection.UI.CharacterWindow
{
    [DisallowMultipleComponent]
    public sealed class CharacterWindowController : MonoBehaviour
    {
        private const int CharacterSortingOrder = 64;

        public UIDocument uiDocument;
        public VisualTreeAsset characterPanelUxml;
        public StyleSheet characterWindowUss;

        private CharacterView view;
        private CharacterWindowPresenter presenter;

        [Inject]
        internal void Construct(CharacterWindowPresenter injectedPresenter)
        {
            presenter = injectedPresenter;
        }

        private void Awake()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                UnityEngine.Debug.LogError("CharacterWindowController: UIDocument missing.");
                return;
            }

            uiDocument.sortingOrder = CharacterSortingOrder;
        }

        private void Start()
        {
            if (uiDocument == null || uiDocument.rootVisualElement == null)
            {
                UnityEngine.Debug.LogError("CharacterWindowController: UIDocument root is missing.");
                return;
            }

            if (presenter == null)
            {
                UnityEngine.Debug.LogError(
                    "CharacterWindowController: Presenter was not injected. Add GameLifetimeScope and CharacterWindowLifetimeScope.");
                return;
            }

            var root = uiDocument.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            if (characterWindowUss != null)
                root.styleSheets.Add(characterWindowUss);
            if (characterPanelUxml != null)
                characterPanelUxml.CloneTree(root);

            var characterRoot = root.Q<VisualElement>("characterRoot") ?? root;
            UiCursorRefresh.ScheduleForRoot(characterRoot, CharacterSortingOrder);

            view = new CharacterView(characterRoot);
            presenter.Bind(view, destroyCancellationToken);
        }

        private void OnDestroy()
        {
            presenter?.Unbind();
            view?.Dispose();
        }
    }
}
