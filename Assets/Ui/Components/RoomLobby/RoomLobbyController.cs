using UnityEngine;
using UnityEngine.UIElements;

namespace ShadowInfection.UI.RoomLobby
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public sealed class RoomLobbyController : MonoBehaviour
    {
        [Header("Assets")]
        [SerializeField] private VisualTreeAsset roomLobbyUxml;
        [SerializeField] private StyleSheet gameBaseStyle;
        [SerializeField] private StyleSheet roomLobbyStyle;
        [SerializeField] private Texture2D aimCursorTexture;
        [SerializeField] private Texture2D hoverCursorTexture;
        [SerializeField] private Texture2D uiCursorTexture;

        [SerializeField] private PanelSettings panelSettings;

        [SerializeField] private float refreshIntervalSeconds = 0.2f;

        private UIDocument uiDocument;
        private RoomLobbyView view;
        private MirrorRoomLobbyPresenter presenter;
        private VisualElement lobbyShell;
        private VisualElement lobbyRoot;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>() ?? gameObject.AddComponent<UIDocument>();
            EnsurePanelSettings();

            if (roomLobbyUxml == null)
            {
                UnityEngine.Debug.LogError("RoomLobbyController: Assign RoomLobby.uxml in the inspector.");
                enabled = false;
                return;
            }

            MountLobbyUi();
            RegisterCursors();
            UiCursorRefresh.SetGameplayPointerEnabled(false);
            UiCursorRefresh.ScheduleForRoot(lobbyRoot);
            UiCursorRefresh.ScheduleForRoot(uiDocument.rootVisualElement);
            view = new RoomLobbyView(lobbyRoot);
            presenter = new MirrorRoomLobbyPresenter(view, refreshIntervalSeconds);
        }

        private void OnEnable()
        {
            presenter?.SetEnabled(true);
        }

        private void MountLobbyUi()
        {
            var documentRoot = uiDocument.rootVisualElement;
            documentRoot.pickingMode = PickingMode.Ignore;

            lobbyShell?.RemoveFromHierarchy();

            lobbyShell = roomLobbyUxml.Instantiate();
            lobbyShell.pickingMode = PickingMode.Ignore;
            lobbyShell.style.flexGrow = 1;
            lobbyShell.style.width = Length.Percent(100);
            lobbyShell.style.height = Length.Percent(100);

            ApplyStyleSheets(documentRoot, lobbyShell);

            documentRoot.Add(lobbyShell);

            lobbyRoot = lobbyShell.Q<VisualElement>("roomLobbyRoot") ?? lobbyShell;
            lobbyRoot.pickingMode = PickingMode.Ignore;

            var lobbyPanel = lobbyRoot.Q<VisualElement>("roomLobbyPanel");
            if (lobbyPanel != null)
            {
                lobbyPanel.pickingMode = PickingMode.Position;
            }
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
            lobbyShell?.RemoveFromHierarchy();
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

            uiDocument.panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
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
