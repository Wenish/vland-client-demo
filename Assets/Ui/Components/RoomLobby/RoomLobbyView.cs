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
        private const float ChangeCharacterButtonGap = 12f;

        private readonly VisualElement root;
        private readonly VisualElement roomLobbyPanel;
        private readonly Label localStatus;
        private readonly Button readyButton;
        private readonly Button changeCharacterButton;
        private readonly ScrollView playerListScroll;
        private readonly VisualElement playerListContent;
        private readonly VisualElement characterSelectOverlay;
        private readonly VisualElement characterCreateOverlay;
        private readonly VisualElement characterDeleteConfirmOverlay;
        private readonly FloatingWindow characterSelectPanel;
        private readonly FloatingWindow characterCreatePanel;
        private readonly FloatingWindow characterDeleteConfirmPanel;
        private readonly Label characterDeleteConfirmMessage;
        private readonly ScrollView characterListScroll;
        private readonly VisualElement characterListContent;
        private readonly TextField characterNameField;
        private readonly Button genderMaleButton;
        private readonly Button genderFemaleButton;
        private readonly Button openCreateCharacterButton;
        private readonly Button createCharacterButton;
        private readonly Button closeCharacterOverlayButton;
        private readonly Button backFromCreateButton;
        private readonly Button cancelDeleteCharacterButton;
        private readonly Button confirmDeleteCharacterButton;

        private CharacterGender selectedGender = CharacterGender.Male;
        private bool characterControlsEnabled = true;
        private bool selectOverlayVisible;
        private bool createOverlayVisible;
        private bool deleteConfirmVisible;
        private bool modalInputPushed;

        public event Action ReadyButtonClicked;
        public event Action ChangeCharacterClicked;
        public event Action CloseCharacterOverlayClicked;
        public event Action OpenCreateCharacterClicked;
        public event Action BackFromCreateClicked;
        public event Action<string> CharacterSelected;
        public event Action CreateCharacterClicked;
        public event Action<string> CharacterDeleteRequested;
        public event Action ConfirmDeleteCharacterClicked;
        public event Action CancelDeleteCharacterClicked;

        public RoomLobbyView(VisualElement root)
        {
            this.root = root;
            roomLobbyPanel = root.Q<VisualElement>("roomLobbyPanel");
            localStatus = root.Q<Label>("localStatus");
            readyButton = root.Q<OrnateButton>("readyButton") ?? root.Q<Button>("readyButton");
            changeCharacterButton = root.Q<OrnateButton>("changeCharacterButton") ?? root.Q<Button>("changeCharacterButton");
            playerListScroll = root.Q<ScrollView>("playerListScroll");
            playerListContent = root.Q<VisualElement>("playerListContent");
            characterSelectOverlay = root.Q<VisualElement>("characterSelectOverlay");
            characterCreateOverlay = root.Q<VisualElement>("characterCreateOverlay");
            characterDeleteConfirmOverlay = root.Q<VisualElement>("characterDeleteConfirmOverlay");
            characterSelectPanel = root.Q<FloatingWindow>("characterSelectPanel");
            characterCreatePanel = root.Q<FloatingWindow>("characterCreatePanel");
            characterDeleteConfirmPanel = root.Q<FloatingWindow>("characterDeleteConfirmPanel");
            characterDeleteConfirmMessage = root.Q<Label>("characterDeleteConfirmMessage");
            characterListScroll = root.Q<ScrollView>("characterListScroll");
            characterListContent = root.Q<VisualElement>("characterListContent");
            if (characterListScroll?.contentContainer != null)
                characterListScroll.contentContainer.pickingMode = PickingMode.Position;
            characterNameField = root.Q<TextField>("characterNameField");
            genderMaleButton = root.Q<Button>("genderMaleButton");
            genderFemaleButton = root.Q<Button>("genderFemaleButton");
            openCreateCharacterButton = root.Q<OrnateButton>("openCreateCharacterButton")
                ?? root.Q<Button>("openCreateCharacterButton");
            createCharacterButton = root.Q<OrnateButton>("createCharacterButton") ?? root.Q<Button>("createCharacterButton");
            closeCharacterOverlayButton = root.Q<OrnateButton>("closeCharacterOverlayButton")
                ?? root.Q<Button>("closeCharacterOverlayButton");
            backFromCreateButton = root.Q<OrnateButton>("backFromCreateButton")
                ?? root.Q<Button>("backFromCreateButton");
            cancelDeleteCharacterButton = root.Q<OrnateButton>("cancelDeleteCharacterButton")
                ?? root.Q<Button>("cancelDeleteCharacterButton");
            confirmDeleteCharacterButton = root.Q<OrnateButton>("confirmDeleteCharacterButton")
                ?? root.Q<Button>("confirmDeleteCharacterButton");

            roomLobbyPanel?.RegisterCallback<GeometryChangedEvent>(_ => PositionChangeCharacterButton());
            PositionChangeCharacterButton();
            root.schedule.Execute(PositionChangeCharacterButton);
            UiGameplayInputGuard.Apply(root);
            UiPointerState.RegisterBlockingElement(characterSelectOverlay);
            UiPointerState.RegisterBlockingElement(characterCreateOverlay);
            UiPointerState.RegisterBlockingElement(characterDeleteConfirmOverlay);

            if (characterSelectOverlay != null)
                characterSelectOverlay.pickingMode = PickingMode.Position;
            if (characterCreateOverlay != null)
                characterCreateOverlay.pickingMode = PickingMode.Position;
            if (characterDeleteConfirmOverlay != null)
                characterDeleteConfirmOverlay.pickingMode = PickingMode.Position;

            WireFloatingWindow(characterSelectPanel, () => CloseCharacterOverlayClicked?.Invoke());
            WireFloatingWindow(characterCreatePanel, () => BackFromCreateClicked?.Invoke());
            WireFloatingWindow(characterDeleteConfirmPanel, () => CancelDeleteCharacterClicked?.Invoke());

            if (readyButton != null)
                readyButton.clicked += () => ReadyButtonClicked?.Invoke();
            else
                UnityEngine.Debug.LogError("RoomLobbyView: readyButton was not found in the lobby UI.");

            if (changeCharacterButton != null)
                changeCharacterButton.clicked += () => ChangeCharacterClicked?.Invoke();

            if (closeCharacterOverlayButton != null)
                closeCharacterOverlayButton.clicked += () => CloseCharacterOverlayClicked?.Invoke();

            if (openCreateCharacterButton != null)
                openCreateCharacterButton.clicked += () => OpenCreateCharacterClicked?.Invoke();

            if (backFromCreateButton != null)
                backFromCreateButton.clicked += () => BackFromCreateClicked?.Invoke();

            if (createCharacterButton != null)
                createCharacterButton.clicked += () => CreateCharacterClicked?.Invoke();

            if (cancelDeleteCharacterButton != null)
                cancelDeleteCharacterButton.clicked += () => CancelDeleteCharacterClicked?.Invoke();

            if (confirmDeleteCharacterButton != null)
                confirmDeleteCharacterButton.clicked += () => ConfirmDeleteCharacterClicked?.Invoke();

            if (genderMaleButton != null)
                genderMaleButton.clicked += () => SetSelectedGender(CharacterGender.Male);

            if (genderFemaleButton != null)
                genderFemaleButton.clicked += () => SetSelectedGender(CharacterGender.Female);

            // TextField must remain focusable and receive pointer hits.
            if (characterNameField != null)
            {
                characterNameField.focusable = true;
                characterNameField.pickingMode = PickingMode.Position;
                var textInput = characterNameField.Q("unity-text-input");
                if (textInput != null)
                    textInput.pickingMode = PickingMode.Position;
            }

            SetSelectedGender(CharacterGender.Male);
            SetCharacterSelectOverlayVisible(false);
            SetCharacterCreateOverlayVisible(false);
            SetCharacterDeleteConfirmVisible(false, null);
        }

        public string CharacterNameInput => characterNameField?.value?.Trim() ?? string.Empty;

        public CharacterGender SelectedGender => selectedGender;

        public bool IsCharacterSelectOverlayVisible => selectOverlayVisible;

        public bool IsCharacterCreateOverlayVisible => createOverlayVisible;

        public bool IsAnyCharacterOverlayVisible => selectOverlayVisible || createOverlayVisible || deleteConfirmVisible;

        private bool isVisible;

        public void SetVisible(bool visible)
        {
            if (root == null || isVisible == visible)
                return;

            isVisible = visible;
            root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible)
            {
                SetCharacterSelectOverlayVisible(false);
                SetCharacterCreateOverlayVisible(false);
                SetCharacterDeleteConfirmVisible(false, null);
            }
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

        public void SetChangeCharacterButtonEnabled(bool enabled)
        {
            changeCharacterButton?.SetEnabled(enabled);
        }

        public void SetCharacterSelectOverlayVisible(bool visible)
        {
            selectOverlayVisible = visible;
            if (characterSelectOverlay != null)
                characterSelectOverlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            characterSelectPanel?.SetOpen(visible);

            RefreshModalInputBlock();
        }

        public void SetCharacterCreateOverlayVisible(bool visible)
        {
            var wasVisible = createOverlayVisible;
            createOverlayVisible = visible;
            if (characterCreateOverlay != null)
                characterCreateOverlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            characterCreatePanel?.SetOpen(visible);

            if (visible && !wasVisible && characterNameField != null)
            {
                characterNameField.schedule.Execute(() =>
                {
                    characterNameField.Focus();
                    // Place caret at end so the blink indicator is visible immediately.
                    var text = characterNameField.value ?? string.Empty;
                    characterNameField.SelectRange(text.Length, text.Length);
                });
            }

            RefreshModalInputBlock();
        }

        public void SetCharacterOverlayCanClose(bool canClose)
        {
            characterSelectPanel?.SetCloseButtonVisible(canClose);

            if (closeCharacterOverlayButton == null)
                return;

            closeCharacterOverlayButton.style.display = canClose ? DisplayStyle.Flex : DisplayStyle.None;
            closeCharacterOverlayButton.SetEnabled(canClose);
        }

        public void SetCharacterDeleteConfirmVisible(bool visible, string characterDisplayName)
        {
            deleteConfirmVisible = visible;
            if (characterDeleteConfirmOverlay != null)
                characterDeleteConfirmOverlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            characterDeleteConfirmPanel?.SetOpen(visible);

            if (characterDeleteConfirmMessage != null)
            {
                characterDeleteConfirmMessage.text = string.IsNullOrWhiteSpace(characterDisplayName)
                    ? "Delete this character permanently?"
                    : $"Delete \"{characterDisplayName}\" permanently?";
            }

            RefreshModalInputBlock();
        }

        public void SetCharacterControlsEnabled(bool enabled)
        {
            characterControlsEnabled = enabled;
            openCreateCharacterButton?.SetEnabled(enabled);
            createCharacterButton?.SetEnabled(enabled);
            genderMaleButton?.SetEnabled(enabled);
            genderFemaleButton?.SetEnabled(enabled);
            characterNameField?.SetEnabled(enabled);

            if (characterListContent == null)
                return;

            foreach (var child in characterListContent.Children())
                child.SetEnabled(enabled);
        }

        public void SetCreateCharacterButtonVisible(bool visible)
        {
            if (openCreateCharacterButton != null)
                openCreateCharacterButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void ClearCharacterNameInput()
        {
            if (characterNameField != null)
                characterNameField.value = string.Empty;
        }

        public void SetCharacters(IReadOnlyList<CharacterRowVm> characters)
        {
            if (characterListContent == null)
                return;

            characterListContent.Clear();

            if (characters == null || characters.Count == 0)
            {
                var empty = new Label("No characters yet. Create one to get started.");
                empty.AddToClassList("room-lobby__empty");
                characterListContent.Add(empty);
                if (characterListScroll != null)
                    characterListScroll.scrollOffset = Vector2.zero;
                return;
            }

            for (var i = 0; i < characters.Count; i++)
            {
                var row = CreateCharacterCard(characters[i], i == characters.Count - 1);
                characterListContent.Add(row);
            }

            if (characterListScroll != null)
                characterListScroll.scrollOffset = Vector2.zero;
            SetCharacterControlsEnabled(characterControlsEnabled);
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

        private static void WireFloatingWindow(FloatingWindow window, Action onClose)
        {
            if (window == null)
                return;

            window.pickingMode = PickingMode.Position;
            window.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
            window.CloseClicked += onClose;
        }

        private void RefreshModalInputBlock()
        {
            var shouldBlock = selectOverlayVisible || createOverlayVisible || deleteConfirmVisible;
            if (shouldBlock == modalInputPushed)
                return;

            if (shouldBlock)
                PlayerInput.CancelLocalGameplayInput();

            UiModalInputBlock.SetActive(shouldBlock);
            modalInputPushed = shouldBlock;
        }

        private void SetSelectedGender(CharacterGender gender)
        {
            selectedGender = gender;
            genderMaleButton?.EnableInClassList("room-lobby__gender-button--active", gender == CharacterGender.Male);
            genderFemaleButton?.EnableInClassList("room-lobby__gender-button--active", gender == CharacterGender.Female);
        }

        private void ScheduleScrollHeightUpdate()
        {
            if (playerListContent == null || playerListScroll == null)
                return;

            playerListContent.RegisterCallbackOnce<GeometryChangedEvent>(_ =>
            {
                UpdateScrollHeightFromContent(playerListScroll, playerListContent, MaxScrollHeight);
                PositionChangeCharacterButton();
            });
            playerListContent.MarkDirtyRepaint();
        }

        private void PositionChangeCharacterButton()
        {
            if (changeCharacterButton == null || roomLobbyPanel == null)
                return;

            var panelLayout = roomLobbyPanel.layout;
            if (panelLayout.height <= 0f)
                return;

            changeCharacterButton.style.left = panelLayout.x;
            changeCharacterButton.style.top = panelLayout.y + panelLayout.height + ChangeCharacterButtonGap;
        }

        private static void UpdateScrollHeightFromContent(ScrollView scroll, VisualElement content, float maxHeight)
        {
            if (scroll == null || content == null)
                return;

            var contentHeight = MeasureContentHeight(scroll, content);
            if (content.childCount > 0 && contentHeight <= 1f)
                return;

            var chromeHeight = MeasureScrollChromeHeight(scroll);
            var totalHeight = contentHeight + chromeHeight;
            var needsScroll = totalHeight > maxHeight;
            var viewportHeight = needsScroll ? maxHeight : totalHeight + ScrollHeightFudge;

            scroll.style.height = viewportHeight;
            scroll.style.maxHeight = needsScroll ? maxHeight : StyleKeyword.Null;
            scroll.verticalScrollerVisibility =
                needsScroll ? ScrollerVisibility.Auto : ScrollerVisibility.Hidden;

            if (!needsScroll)
                scroll.scrollOffset = Vector2.zero;
        }

        private static float MeasureContentHeight(ScrollView scroll, VisualElement content)
        {
            var contentContainer = scroll.contentContainer;
            var measured = contentContainer != null ? contentContainer.layout.height : 0f;
            if (measured <= 0f)
                measured = content.layout.height;
            if (measured <= 0f)
                measured = content.resolvedStyle.height;

            if (measured <= 0f && content.childCount > 0)
            {
                float sum = 0f;
                foreach (var child in content.Children())
                {
                    if (child == null || child.resolvedStyle.display == DisplayStyle.None)
                        continue;

                    float childHeight = child.layout.height;
                    if (childHeight <= 0f)
                        childHeight = child.resolvedStyle.height;
                    if (childHeight <= 0f)
                        continue;

                    sum += childHeight
                        + child.resolvedStyle.marginTop
                        + child.resolvedStyle.marginBottom;
                }

                measured = sum;
            }

            return Mathf.Ceil(measured);
        }

        private static float MeasureScrollChromeHeight(ScrollView scroll)
        {
            var style = scroll.resolvedStyle;
            return style.paddingTop
                + style.paddingBottom
                + style.borderTopWidth
                + style.borderBottomWidth;
        }

        private VisualElement CreateCharacterCard(CharacterRowVm vm, bool isLast)
        {
            var card = new VisualElement();
            card.AddToClassList("room-lobby__character-card");
            if (vm.isSelected)
                card.AddToClassList("room-lobby__character-card--selected");
            if (isLast)
                card.AddToClassList("room-lobby__character-card--last");

            var main = new VisualElement();
            main.AddToClassList("room-lobby__character-card-main");

            var name = new Label(vm.displayName);
            name.AddToClassList("room-lobby__character-card-name");
            main.Add(name);

            var meta = new VisualElement();
            meta.AddToClassList("room-lobby__character-card-meta");

            var gender = new Label(vm.genderLabel);
            gender.AddToClassList("room-lobby__character-badge");
            if (vm.genderLabel == "Female")
                gender.AddToClassList("room-lobby__character-badge--female");
            meta.Add(gender);

            if (vm.isSelected)
            {
                var active = new Label("Active");
                active.AddToClassList("room-lobby__character-badge");
                active.AddToClassList("room-lobby__character-badge--active");
                meta.Add(active);
            }

            main.Add(meta);

            var actions = new VisualElement();
            actions.AddToClassList("room-lobby__character-actions");

            var delete = new Button(() => CharacterDeleteRequested?.Invoke(vm.id))
            {
                text = "Delete"
            };
            delete.AddToClassList("si-button");
            delete.AddToClassList("si-button--compact");
            delete.AddToClassList("room-lobby__character-delete");
            delete.focusable = false;
            delete.SetEnabled(characterControlsEnabled);

            var select = new Button(() => CharacterSelected?.Invoke(vm.id))
            {
                text = vm.isSelected ? "Continue" : "Select"
            };
            select.AddToClassList("si-button");
            select.AddToClassList("si-button--compact");
            select.AddToClassList("room-lobby__character-select");
            select.focusable = false;
            select.SetEnabled(characterControlsEnabled);

            actions.Add(delete);
            actions.Add(select);

            card.Add(main);
            card.Add(actions);
            return card;
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

    internal readonly struct CharacterRowVm
    {
        public readonly string id;
        public readonly string displayName;
        public readonly string genderLabel;
        public readonly bool isSelected;

        public CharacterRowVm(string id, string displayName, string genderLabel, bool isSelected)
        {
            this.id = id;
            this.displayName = displayName;
            this.genderLabel = genderLabel;
            this.isSelected = isSelected;
        }
    }
}
