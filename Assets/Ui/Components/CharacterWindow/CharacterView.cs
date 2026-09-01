using System;
using System.Collections.Generic;
using ShadowInfection.Items;
using ShadowInfection.UI;
using UnityEngine;
using UnityEngine.UIElements;
using Vland.UI;

namespace ShadowInfection.UI.CharacterWindow
{
    internal sealed class CharacterSlotVm
    {
        public ItemSlot Slot;
        public Texture2D Icon;
        public string RarityClass;
        public string EmptyLabel;
        public bool Occupied;
    }

    internal sealed class CharacterView
    {
        private static readonly (string name, ItemSlot slot, string label)[] SlotMap =
        {
            ("slotHead", ItemSlot.Head, "Head"),
            ("slotShoulder", ItemSlot.Shoulder, "Shoulder"),
            ("slotCape", ItemSlot.Cape, "Cape"),
            ("slotGloves", ItemSlot.Gloves, "Gloves"),
            ("slotChest", ItemSlot.Chest, "Chest"),
            ("slotPants", ItemSlot.Pants, "Pants"),
            ("slotFeet", ItemSlot.Feet, "Feet"),
            ("slotMainHand", ItemSlot.MainHand, "Main"),
            ("slotOffHand", ItemSlot.OffHand, "Off")
        };

        private readonly VisualElement host;
        private readonly VisualElement panel;
        private readonly VisualElement headerRow;
        private readonly Label subheading;
        private readonly Dictionary<ItemSlot, VisualElement> slotElements = new();
        private readonly UiDraggablePanel draggable;

        private bool isOpen;
        private bool modalInputPushed;
        private ItemSlot? activeSlot;

        public event Action CloseClicked;
        public event Action<ItemSlot> SlotClicked;
        public event Action<ItemSlot> SlotRightClicked;
        public event Action PositionChanged;

        public bool IsOpen => isOpen;
        public UiDraggablePanel Draggable => draggable;

        public CharacterView(VisualElement root)
        {
            host = root.Q<VisualElement>("characterHost");
            panel = root.Q<VisualElement>("CharacterPanel");
            headerRow = root.Q<VisualElement>("headerRow");
            subheading = root.Q<Label>("subheading");

            if (host == null || panel == null)
            {
                UnityEngine.Debug.LogError("CharacterView: host or panel was not found.");
                return;
            }

            host.pickingMode = PickingMode.Ignore;
            panel.pickingMode = PickingMode.Position;
            UiGameplayInputGuard.Apply(panel);
            UiPointerState.RegisterBlockingElement(panel);
            panel.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());

            draggable = new UiDraggablePanel(host, panel, headerRow, ComputeDefaultPosition);
            draggable.PositionChanged += () => PositionChanged?.Invoke();

            var closeButton = root.Q<OrnateButton>("closeButton") ?? root.Q<Button>("closeButton");
            if (closeButton != null)
                closeButton.clicked += () => CloseClicked?.Invoke();

            BuildSlots(root);
            SetSubheading(null);
            SetOpen(false);
        }

        public void Dispose()
        {
            ReleaseModalInputBlock();
            UiPointerState.UnregisterBlockingElement(panel);
        }

        public void SetOpen(bool open)
        {
            isOpen = open;
            draggable?.SetOpen(open);
            if (host != null)
                host.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            if (!open)
                SetSubheading(null);
            RefreshModalInputBlock();
        }

        public void ApplyPosition(float left, float top) => draggable?.ApplyPosition(left, top);

        public void ApplyDefaultPosition() => draggable?.ApplyDefaultPosition();

        public Vector2 GetPosition() => draggable != null ? draggable.GetPosition() : Vector2.zero;

        public bool HasUsableLayout() => draggable != null && draggable.HasUsableLayout();

        public void ClampToViewport() => draggable?.ClampToViewport();

        public void SetActiveSlot(ItemSlot? slot)
        {
            activeSlot = slot;
            foreach (var pair in slotElements)
                pair.Value.EnableInClassList("character-slot--active", slot.HasValue && pair.Key == slot.Value);
        }

        public void SetSubheading(string text)
        {
            if (subheading == null)
                return;

            var hasText = !string.IsNullOrWhiteSpace(text);
            subheading.style.display = hasText ? DisplayStyle.Flex : DisplayStyle.None;
            subheading.text = text ?? string.Empty;
        }

        public void SetSlots(IReadOnlyList<CharacterSlotVm> slots)
        {
            if (slots == null)
                return;

            for (var i = 0; i < slots.Count; i++)
            {
                var vm = slots[i];
                if (!slotElements.TryGetValue(vm.Slot, out var element))
                    continue;

                element.Clear();
                element.tooltip = vm.Occupied ? null : vm.EmptyLabel;
                if (vm.Occupied && vm.Icon != null)
                {
                    var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                    icon.AddToClassList("character-slot__icon");
                    if (!string.IsNullOrEmpty(vm.RarityClass))
                        icon.AddToClassList(vm.RarityClass);
                    icon.style.backgroundImage = new StyleBackground(vm.Icon);
                    element.Add(icon);
                }
                else
                {
                    var label = new Label(vm.EmptyLabel ?? ItemPresentation.SlotLabel(vm.Slot))
                    {
                        pickingMode = PickingMode.Ignore
                    };
                    label.AddToClassList("character-slot__label");
                    element.Add(label);
                }
            }
        }

        private void BuildSlots(VisualElement root)
        {
            for (var i = 0; i < SlotMap.Length; i++)
            {
                var (name, slot, label) = SlotMap[i];
                var element = root.Q<VisualElement>(name);
                if (element == null)
                    continue;

                element.pickingMode = PickingMode.Position;
                element.userData = slot;
                slotElements[slot] = element;

                var capturedSlot = slot;
                element.RegisterCallback<ClickEvent>(evt =>
                {
                    if (evt.button == 0)
                        SlotClicked?.Invoke(capturedSlot);
                });
                element.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button == 1)
                    {
                        SlotRightClicked?.Invoke(capturedSlot);
                        evt.StopImmediatePropagation();
                    }
                });
                element.RegisterCallback<PointerEnterEvent>(_ => UiCursorRefresh.PushInteractiveHover(), TrickleDown.TrickleDown);
                element.RegisterCallback<PointerLeaveEvent>(_ => UiCursorRefresh.PopInteractiveHover(), TrickleDown.TrickleDown);
            }
        }

        private (float left, float top) ComputeDefaultPosition(bool _)
        {
            if (draggable == null || !draggable.TryGetLayoutSize(out var hostWidth, out var hostHeight, out var width, out var height))
                return (96f, 120f);

            var left = Mathf.Clamp(hostWidth * 0.18f, 72f, hostWidth * 0.35f);
            var top = (hostHeight - height) * 0.5f;
            return (left, top);
        }

        private void RefreshModalInputBlock()
        {
            var shouldBlock = isOpen;
            if (shouldBlock == modalInputPushed)
                return;

            if (shouldBlock)
            {
                PlayerInput.CancelLocalGameplayInput();
                UiModalInputBlock.Push();
            }
            else
            {
                UiModalInputBlock.Pop();
            }

            modalInputPushed = shouldBlock;
        }

        private void ReleaseModalInputBlock()
        {
            if (!modalInputPushed)
                return;

            UiModalInputBlock.Pop();
            modalInputPushed = false;
        }
    }
}
