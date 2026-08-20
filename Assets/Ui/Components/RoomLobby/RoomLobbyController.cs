using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace ShadowInfection.UI.RoomLobby
{
    [DisallowMultipleComponent]
    public sealed class RoomLobbyController : MonoBehaviour
    {
        private const int LobbySortingOrder = 50;
        private const int RootWaitFrames = 16;

        [Header("Assets")]
        [SerializeField] private VisualTreeAsset roomLobbyUxml;
        [SerializeField] private StyleSheet gameBaseStyle;
        [SerializeField] private StyleSheet roomLobbyStyle;
        [SerializeField] private Texture2D aimCursorTexture;
        [SerializeField] private Texture2D hoverCursorTexture;
        [SerializeField] private Texture2D uiCursorTexture;

        [SerializeField] private PanelSettings panelSettings;

        private UIDocument uiDocument;
        private RoomLobbyView view;
        private MirrorRoomLobbyPresenter presenter;
        private VisualElement lobbyRoot;
        private bool mounted;

        [Inject]
        internal void Construct(MirrorRoomLobbyPresenter injectedPresenter)
        {
            presenter = injectedPresenter;
        }

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>() ?? gameObject.AddComponent<UIDocument>();
            uiDocument.sortingOrder = LobbySortingOrder;
        }

        private IEnumerator Start()
        {
            if (roomLobbyUxml == null)
            {
                UnityEngine.Debug.LogError("RoomLobbyController: Assign RoomLobby.uxml in the inspector.");
                yield break;
            }

            yield return WaitForDocumentRoot();

            if (!MountLobbyUi())
            {
                UnityEngine.Debug.LogError(
                    "RoomLobbyController: UIDocument root is missing after waiting. " +
                    $"panelSettings={(uiDocument != null && uiDocument.panelSettings != null)} " +
                    $"visualTreeAsset={(uiDocument != null && uiDocument.visualTreeAsset != null)} " +
                    $"uiDocumentEnabled={(uiDocument != null && uiDocument.enabled)}");
                yield break;
            }

            if (presenter == null)
            {
                UnityEngine.Debug.LogError(
                    "RoomLobbyController: Presenter was not injected. Add GameLifetimeScope and RoomLobbyLifetimeScope to the scene.");
                yield break;
            }

            RegisterCursors();
            UiCursorRefresh.SetGameplayPointerEnabled(false);
            UiCursorRefresh.ScheduleForRoot(lobbyRoot, LobbySortingOrder);
            UiCursorRefresh.ScheduleForRoot(uiDocument.rootVisualElement, LobbySortingOrder);
            view = new RoomLobbyView(lobbyRoot);
            presenter.Bind(view);
            presenter.SetEnabled(isActiveAndEnabled);
        }

        private IEnumerator WaitForDocumentRoot()
        {
            EnsurePanelSettings();

            if (uiDocument.visualTreeAsset == null)
                uiDocument.visualTreeAsset = roomLobbyUxml;

            if (!uiDocument.enabled)
                uiDocument.enabled = true;

            if (uiDocument.rootVisualElement == null)
            {
                uiDocument.enabled = false;
                yield return null;
                uiDocument.enabled = true;
            }

            for (var i = 0; i < RootWaitFrames && uiDocument.rootVisualElement == null; i++)
                yield return null;
        }

        private void OnEnable()
        {
            presenter?.SetEnabled(true);
        }

        private bool MountLobbyUi()
        {
            if (mounted)
                return true;

            var documentRoot = uiDocument.rootVisualElement;
            if (documentRoot == null)
                return false;

            documentRoot.pickingMode = PickingMode.Ignore;
            StretchToFill(documentRoot);

            lobbyRoot = FindByName(documentRoot, "roomLobbyRoot") ?? documentRoot;
            lobbyRoot.pickingMode = PickingMode.Ignore;
            StretchToFill(lobbyRoot);
            ApplyStyleSheets(documentRoot, lobbyRoot);

            var lobbyPanel = FindByName(lobbyRoot, "roomLobbyPanel");
            if (lobbyPanel != null)
            {
                lobbyPanel.pickingMode = PickingMode.Position;
                PinPanelTopLeft(lobbyPanel);
            }

            mounted = true;
            return true;
        }

        private static VisualElement FindByName(VisualElement root, string name)
        {
            if (root == null)
                return null;

            if (root.name == name)
                return root;

            return root.Q<VisualElement>(name);
        }

        private static void StretchToFill(VisualElement element)
        {
            if (element == null)
                return;

            element.style.flexGrow = 1;
            element.style.width = Length.Percent(100);
            element.style.height = Length.Percent(100);
        }

        private static void PinPanelTopLeft(VisualElement panel)
        {
            panel.style.position = Position.Absolute;
            panel.style.left = 24;
            panel.style.top = 24;
            panel.style.right = StyleKeyword.Auto;
            panel.style.bottom = StyleKeyword.Auto;
            panel.style.flexShrink = 0;
        }

        private void ApplyStyleSheets(params VisualElement[] targets)
        {
            foreach (var target in targets)
            {
                if (target == null)
                    continue;

                AddStyleSheet(target, gameBaseStyle);
                AddStyleSheet(target, roomLobbyStyle);
            }
        }

        private static void AddStyleSheet(VisualElement target, StyleSheet sheet)
        {
            if (sheet == null || target.styleSheets.Contains(sheet))
                return;

            target.styleSheets.Add(sheet);
        }

        private void RegisterCursors()
        {
            if (aimCursorTexture == null && hoverCursorTexture == null && uiCursorTexture == null)
                return;

            UiCursorRefresh.Configure(aimCursorTexture, hoverCursorTexture, uiCursorTexture);
        }

        private void OnDestroy()
        {
            presenter?.SetEnabled(false);
            presenter?.Unbind();
        }

        private void OnDisable()
        {
            presenter?.SetEnabled(false);
        }

        private void Update()
        {
            presenter?.Tick(Time.unscaledTime);
        }

        private void EnsurePanelSettings()
        {
            if (uiDocument == null)
                return;

            if (uiDocument.panelSettings != null)
                return;

            if (panelSettings != null)
            {
                uiDocument.panelSettings = panelSettings;
                return;
            }

#if UNITY_2023_1_OR_NEWER
            var docs = FindObjectsByType<UIDocument>();
#else
            var docs = FindObjectsOfType<UIDocument>();
#endif
            foreach (var doc in docs)
            {
                if (doc != null && doc != uiDocument && doc.panelSettings != null)
                {
                    uiDocument.panelSettings = doc.panelSettings;
                    return;
                }
            }

            uiDocument.panelSettings = panelSettings;
        }

#if UNITY_EDITOR
        private void Reset()
        {
            if (roomLobbyUxml == null)
            {
                roomLobbyUxml = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/Ui/Components/RoomLobby/RoomLobby.uxml");
            }

            if (gameBaseStyle == null)
            {
                gameBaseStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Ui/GameBase.uss");
            }

            if (roomLobbyStyle == null)
            {
                roomLobbyStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Ui/Components/RoomLobby/RoomLobby.uss");
            }

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
