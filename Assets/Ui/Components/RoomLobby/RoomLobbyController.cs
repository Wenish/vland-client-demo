using UnityEngine;
using UnityEngine.UIElements;

namespace ShadowInfection.UI.RoomLobby
{
    [DisallowMultipleComponent]
    public sealed class RoomLobbyController : MonoBehaviour
    {
        [Header("Assets")]
        [Tooltip("UXML template for the room lobby UI.")]
        [SerializeField] private VisualTreeAsset roomLobbyUxml;

        [Tooltip("Optional: PanelSettings to use for this UIDocument. If empty, the controller tries to reuse one from other UIDocuments in the scene.")]
        [SerializeField] private PanelSettings panelSettings;

        [Tooltip("UI refresh rate in seconds. Lower = more responsive, higher = less GC/CPU.")]
        [SerializeField] private float refreshIntervalSeconds = 0.2f;

        private UIDocument uiDocument;
        private RoomLobbyView view;
        private MirrorRoomLobbyPresenter presenter;

        private VisualElement lobbyRoot;
        private bool styleSheetAddedToDocument;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>() ?? gameObject.AddComponent<UIDocument>();

            EnsurePanelSettings();

            if (roomLobbyUxml == null)
            {
                UnityEngine.Debug.LogError("RoomLobbyController: Missing UXML reference (roomLobbyUxml). Assign the RoomLobby.uxml asset in the inspector.");
                enabled = false;
                return;
            }

            var root = uiDocument.rootVisualElement;
            lobbyRoot = root.Q<VisualElement>("roomLobbyRoot");
            if (lobbyRoot != null)
                lobbyRoot.RemoveFromHierarchy();

            var tree = roomLobbyUxml.CloneTree();
            lobbyRoot = tree.Q<VisualElement>("roomLobbyRoot") ?? tree;

            root.Add(tree);

            view = new RoomLobbyView(tree);
            presenter = new MirrorRoomLobbyPresenter(view, refreshIntervalSeconds);
        }

        private void OnDestroy()
        {
            presenter?.SetEnabled(false);

            if (lobbyRoot != null)
                lobbyRoot.RemoveFromHierarchy();
        }

        private void OnEnable()
        {
            presenter?.SetEnabled(true);
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

            // Reuse an existing PanelSettings if another UIDocument already exists in the scene.
            UIDocument[] docs;
#if UNITY_2023_1_OR_NEWER
            docs = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
#else
            docs = FindObjectsOfType<UIDocument>();
#endif
            for (int i = 0; i < docs.Length; i++)
            {
                var doc = docs[i];
                if (doc != null && doc != uiDocument && doc.panelSettings != null)
                {
                    uiDocument.panelSettings = doc.panelSettings;
                    return;
                }
            }

            // Fallback is convenient for quick testing, but a real PanelSettings asset is recommended.
            uiDocument.panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
        }
    }
}
