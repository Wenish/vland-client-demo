using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace ShadowInfection.UI.RoomLobby
{
    internal sealed class RoomLobbyView
    {
        private readonly VisualElement root;
        private readonly Label subtitle;
        private readonly Label localStatus;
        private readonly Button readyButton;
        private readonly ListView playerList;

        public event Action ReadyButtonClicked;

        public RoomLobbyView(VisualElement root)
        {
            this.root = root;
            subtitle = root.Q<Label>("subtitle");
            localStatus = root.Q<Label>("localStatus");
            readyButton = root.Q<Button>("readyButton");
            playerList = root.Q<ListView>("playerList");

            readyButton.clicked += () => ReadyButtonClicked?.Invoke();

            playerList.selectionType = SelectionType.None;
        }

        public void SetVisible(bool visible)
        {
            if (root == null)
                return;

            root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetSubtitle(string text)
        {
            if (subtitle != null)
                subtitle.text = text ?? string.Empty;
        }

        public void SetLocalReadyState(bool isReady)
        {
            if (localStatus != null)
                localStatus.text = isReady ? "Ready" : "Not Ready";

            if (readyButton != null)
            {
                readyButton.text = isReady ? "Not Ready" : "Ready";

                readyButton.RemoveFromClassList("room-lobby__button--ready");
                if (!isReady)
                    readyButton.AddToClassList("room-lobby__button--ready");
            }
        }

        public void SetReadyButtonEnabled(bool enabled)
        {
            if (readyButton != null)
                readyButton.SetEnabled(enabled);
        }

        public void BindPlayerList(
            Func<VisualElement> makeItem,
            Action<VisualElement, int> bindItem,
            IList itemsSource)
        {
            playerList.makeItem = makeItem;
            playerList.bindItem = bindItem;
            playerList.itemsSource = itemsSource;
        }

        public void RefreshPlayers()
        {
            playerList?.RefreshItems();
        }

        internal readonly struct PlayerRowVm
        {
            public readonly uint netId;
            public readonly int index;
            public readonly string displayName;
            public readonly bool ready;
            public readonly bool isLocal;

            public PlayerRowVm(uint netId, int index, string displayName, bool ready, bool isLocal)
            {
                this.netId = netId;
                this.index = index;
                this.displayName = displayName;
                this.ready = ready;
                this.isLocal = isLocal;
            }
        }
    }
}
