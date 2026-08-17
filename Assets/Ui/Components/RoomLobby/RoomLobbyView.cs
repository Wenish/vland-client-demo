using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShadowInfection.UI.RoomLobby
{
    internal sealed class RoomLobbyView
    {
        private const float MaxScrollHeight = 300f;
        private const float ScrollHeightFudge = 2f;

        private readonly VisualElement root;
        private readonly Label subtitle;
        private readonly Label localStatus;
        private readonly Button readyButton;
        private readonly ScrollView playerListScroll;
        private readonly VisualElement playerListContent;

        public event Action ReadyButtonClicked;

        public RoomLobbyView(VisualElement root)
        {
            this.root = root;
            subtitle = root.Q<Label>("subtitle");
            localStatus = root.Q<Label>("localStatus");
            readyButton = root.Q<Button>("readyButton");
            playerListScroll = root.Q<ScrollView>("playerListScroll");
            playerListContent = root.Q<VisualElement>("playerListContent");

            UiGameplayInputGuard.Apply(root);

            if (readyButton != null)
                readyButton.clicked += () => ReadyButtonClicked?.Invoke();
        }

        public void SetVisible(bool visible)
        {
            if (root != null)
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

            if (readyButton == null)
                return;

            readyButton.text = isReady ? "Not Ready" : "Ready";
            readyButton.EnableInClassList("room-lobby__button--ready", !isReady);
        }

        public void SetReadyButtonEnabled(bool enabled)
        {
            readyButton?.SetEnabled(enabled);
        }

        public void SetPlayers(IReadOnlyList<PlayerRowVm> players)
        {
            if (playerListContent == null)
                return;

            playerListContent.Clear();

            if (players == null || players.Count == 0)
            {
                var empty = new Label("No players in room yet.");
                empty.AddToClassList("room-lobby__empty");
                playerListContent.Add(empty);
                ScheduleScrollHeightUpdate();
                return;
            }

            for (var i = 0; i < players.Count; i++)
            {
                var row = CreatePlayerRow(players[i]);
                if (i == players.Count - 1)
                    row.AddToClassList("room-lobby__row--last");
                playerListContent.Add(row);
            }

            ScheduleScrollHeightUpdate();
        }

        private void ScheduleScrollHeightUpdate()
        {
            if (playerListContent == null || playerListScroll == null)
                return;

            playerListContent.RegisterCallbackOnce<GeometryChangedEvent>(_ =>
            {
                UpdateScrollHeightFromContent();
                playerListScroll.RegisterCallbackOnce<GeometryChangedEvent>(_ => FinalizeScrollHeight());
            });
            playerListContent.MarkDirtyRepaint();
        }

        private void UpdateScrollHeightFromContent()
        {
            if (playerListScroll == null || playerListContent == null)
                return;

            var contentHeight = MeasureContentHeight();
            var chromeHeight = MeasureScrollChromeHeight();
            var totalHeight = contentHeight + chromeHeight;
            var needsScroll = totalHeight > MaxScrollHeight;
            var viewportHeight = needsScroll ? MaxScrollHeight : totalHeight + ScrollHeightFudge;

            playerListScroll.style.height = viewportHeight;
            playerListScroll.style.maxHeight = needsScroll ? MaxScrollHeight : StyleKeyword.Null;
            playerListScroll.verticalScrollerVisibility =
                needsScroll ? ScrollerVisibility.Auto : ScrollerVisibility.Hidden;

            if (!needsScroll)
                playerListScroll.scrollOffset = Vector2.zero;
        }

        private void FinalizeScrollHeight()
        {
            if (playerListScroll == null)
                return;

            var contentContainer = playerListScroll.contentContainer;
            var contentViewport = playerListScroll.contentViewport;
            if (contentContainer == null || contentViewport == null)
                return;

            var overflow = contentContainer.layout.height - contentViewport.layout.height;
            if (overflow <= 1f)
            {
                playerListScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
                playerListScroll.scrollOffset = Vector2.zero;

                if (overflow > 0f)
                    playerListScroll.style.height = playerListScroll.layout.height + overflow + 1f;

                return;
            }

            if (MeasureContentHeight() + MeasureScrollChromeHeight() <= MaxScrollHeight)
            {
                playerListScroll.style.height = playerListScroll.layout.height + overflow + 1f;
                playerListScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
                playerListScroll.scrollOffset = Vector2.zero;
            }
        }

        private float MeasureContentHeight()
        {
            var contentContainer = playerListScroll.contentContainer;
            var measured = contentContainer != null ? contentContainer.layout.height : playerListContent.layout.height;
            if (measured <= 0f)
                measured = playerListContent.layout.height;
            if (measured <= 0f)
                measured = playerListContent.resolvedStyle.height;

            return Mathf.Ceil(measured);
        }

        private float MeasureScrollChromeHeight()
        {
            var style = playerListScroll.resolvedStyle;
            return style.paddingTop
                + style.paddingBottom
                + style.borderTopWidth
                + style.borderBottomWidth;
        }

        private static VisualElement CreatePlayerRow(PlayerRowVm vm)
        {
            var row = new VisualElement();
            row.AddToClassList("room-lobby__row");
            if (vm.isLocal)
                row.AddToClassList("room-lobby__row--local");

            var left = new VisualElement();
            left.AddToClassList("room-lobby__row-left");

            var dot = new VisualElement();
            dot.AddToClassList("room-lobby__dot");
            if (vm.ready)
                dot.AddToClassList("room-lobby__dot--ready");

            var name = new Label(vm.displayName);
            name.AddToClassList("room-lobby__name");

            left.Add(dot);
            left.Add(name);

            if (vm.isLocal)
            {
                var tag = new Label("(You)");
                tag.AddToClassList("room-lobby__tag");
                left.Add(tag);
            }

            var state = new Label(vm.ready ? "Ready" : "Not Ready");
            state.AddToClassList("room-lobby__state");
            if (vm.ready)
                state.AddToClassList("room-lobby__state--ready");

            row.Add(left);
            row.Add(state);
            return row;
        }
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
