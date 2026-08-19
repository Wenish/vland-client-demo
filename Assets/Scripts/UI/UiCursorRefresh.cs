using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// Drives hardware cursors for gameplay (pointer), interactive UI (hover), and
// non-interactive UI (default). UnityEngine.Cursor.SetCursor is used because
// UI Toolkit USS cursors do not reliably win once a hardware cursor is set.
public static class UiCursorRefresh
{
    public static readonly Vector2 PointerHotspot = new Vector2(16f, 16f);
    public static readonly Vector2 HoverHotspot = Vector2.zero;
    public static readonly Vector2 DefaultHotspot = Vector2.zero;

    private static Texture2D _pointerCursor;
    private static Texture2D _hoverCursor;
    private static Texture2D _defaultCursor;

    private static bool _gameplayPointerEnabled;
    private static int _interactiveHoverDepth;

    private static readonly HashSet<IPanel> HookedPanels = new HashSet<IPanel>();
    private static readonly HashSet<VisualElement> AttachedRoots = new HashSet<VisualElement>();
    private static readonly Dictionary<IPanel, int> PanelSortingOrders = new Dictionary<IPanel, int>();

    public static void Configure(Texture2D pointerCursor, Texture2D hoverCursor, Texture2D defaultCursor)
    {
        _pointerCursor = pointerCursor;
        _hoverCursor = hoverCursor;
        _defaultCursor = defaultCursor;
    }

    /// <summary>
    /// When enabled, empty space and pass-through UI use the attack/pointer cursor.
    /// Enable while controlling a character; keep disabled in menus and lobbies.
    /// </summary>
    public static void SetGameplayPointerEnabled(bool enabled)
    {
        if (_gameplayPointerEnabled == enabled)
            return;

        _gameplayPointerEnabled = enabled;
        ApplyHardwareCursor();
    }

    public static void PushInteractiveHover()
    {
        _interactiveHoverDepth++;
        ApplyHardwareCursor();
    }

    public static void PopInteractiveHover()
    {
        if (_interactiveHoverDepth > 0)
            _interactiveHoverDepth--;

        ApplyHardwareCursor();
    }

    public static void ResetInteractiveHover()
    {
        _interactiveHoverDepth = 0;
        ApplyHardwareCursor();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void SubscribeSceneEvents()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetInteractiveHover();
    }

    public static void Refresh()
    {
        ApplyHardwareCursor();
    }

    public static void ScheduleForRoot(VisualElement root, int sortingOrder = 0)
    {
        if (root == null || !AttachedRoots.Add(root))
            return;

        root.RegisterCallback<DetachFromPanelEvent>(_ => AttachedRoots.Remove(root));

        void Attach()
        {
            var panel = root.panel;
            if (panel == null)
                return;

            if (!PanelSortingOrders.TryGetValue(panel, out var existing) || sortingOrder > existing)
                PanelSortingOrders[panel] = sortingOrder;

            ApplyHardwareCursor();
            HookPanel(panel);
            root.schedule.Execute(ApplyHardwareCursor).StartingIn(1);
        }

        if (root.panel != null)
        {
            Attach();
            return;
        }

        root.RegisterCallback<AttachToPanelEvent>(_ => Attach());
    }

    private static void HookPanel(IPanel panel)
    {
        if (panel?.visualTree == null || !HookedPanels.Add(panel))
            return;

        panel.visualTree.RegisterCallback<PointerMoveEvent>(OnPanelPointerMove);
        panel.visualTree.RegisterCallback<PointerEnterEvent>(OnPanelPointerMoveGeneric);
        panel.visualTree.RegisterCallback<PointerLeaveEvent>(OnPanelPointerLeave);
    }

    private static void OnPanelPointerMove(PointerMoveEvent evt)
    {
        ApplyHardwareCursor();
    }

    private static void OnPanelPointerMoveGeneric(PointerEnterEvent evt)
    {
        ApplyHardwareCursor();
    }

    private static void OnPanelPointerLeave(PointerLeaveEvent evt)
    {
        ApplyHardwareCursor();
    }

    private static void ApplyHardwareCursor()
    {
        if (_interactiveHoverDepth > 0)
        {
            if (_hoverCursor != null)
                UnityEngine.Cursor.SetCursor(_hoverCursor, HoverHotspot, CursorMode.Auto);
            return;
        }

        ApplyHardwareCursorForPick(PickBestElementAtMouse());
    }

    private static VisualElement PickBestElementAtMouse()
    {
        Vector2 screenPos = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        foreach (var panel in HookedPanels
                     .Where(panel => panel?.visualTree != null)
                     .OrderByDescending(GetPanelSortingOrder))
        {
            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(panel, screenPos);
            var picked = panel.Pick(panelPos);
            if (picked != null)
                return picked;
        }

        return null;
    }

    private static int GetPanelSortingOrder(IPanel panel)
    {
        return PanelSortingOrders.TryGetValue(panel, out var sortingOrder) ? sortingOrder : 0;
    }

    private static void ApplyHardwareCursorForPick(VisualElement picked)
    {
        switch (ResolveCursorKind(picked))
        {
            case CursorKind.Hover when _hoverCursor != null:
                UnityEngine.Cursor.SetCursor(_hoverCursor, HoverHotspot, CursorMode.Auto);
                break;
            case CursorKind.Default when _defaultCursor != null:
                UnityEngine.Cursor.SetCursor(_defaultCursor, DefaultHotspot, CursorMode.Auto);
                break;
            case CursorKind.Pointer when _pointerCursor != null:
                UnityEngine.Cursor.SetCursor(_pointerCursor, PointerHotspot, CursorMode.Auto);
                break;
        }
    }

    private static CursorKind ResolveCursorKind(VisualElement picked)
    {
        if (picked == null)
            return _gameplayPointerEnabled ? CursorKind.Pointer : CursorKind.Default;

        var hasDefault = false;
        for (var element = picked; element != null; element = element.parent)
        {
            if (IsHoverCursorElement(element))
                return CursorKind.Hover;
            if (IsDefaultCursorElement(element))
                hasDefault = true;
        }

        if (hasDefault)
            return CursorKind.Default;

        return _gameplayPointerEnabled ? CursorKind.Pointer : CursorKind.Default;
    }

    private static bool IsHoverCursorElement(VisualElement element)
    {
        if (element is Button or TextField or Toggle or Slider or DropdownField or IntegerField or FloatField)
            return true;

        if (element is OrnateButton or AbilityCooldownElement or LoadoutTile)
            return true;

        return HasAnyClass(
            element,
            "room-lobby__ready-button",
            "si-button",
            "si-button__icon",
            "unity-button",
            "unity-button__image",
            "ability-container",
            "loadout-tile",
            "loadout-tile__icon",
            "slot",
            "slot__icon",
            "unity-base-field__input",
            "unity-text-element__selectable",
            "unity-base-slider__drag-container",
            "unity-base-slider__fill",
            "unity-base-slider__dragger",
            "unity-base-slider__tracker",
            "unity-base-popup-field__arrow",
            "unity-base-popup-field__text");
    }

    private static bool IsDefaultCursorElement(VisualElement element)
    {
        if (element is Label)
            return true;

        return HasAnyClass(
            element,
            "room-lobby__panel",
            "room-lobby__row",
            "room-lobby__scroll",
            "room-lobby__rows",
            "room-lobby__title",
            "room-lobby__subtitle",
            "room-lobby__section-title",
            "room-lobby__name",
            "room-lobby__status",
            "room-lobby__empty",
            "si-body",
            "si-title",
            "si-panel",
            "si-panel__title",
            "si-panel__label",
            "si-hud-panel",
            "loadout-panel",
            "loadout-header",
            "loadout-subheading",
            "loadout-window",
            "slots-bar",
            "si-gold",
            "si-round-stats",
            "si-round-box",
            "si-round-stat",
            "cast-bar",
            "si-vitals",
            "si-vital-row",
            "si-player-stats",
            "si-player-stats__label",
            "si-round-infos",
            "si-round-title",
            "si-round-completion-label");
    }

    private static bool HasAnyClass(VisualElement element, params string[] classNames)
    {
        foreach (var className in classNames)
        {
            if (element.ClassListContains(className))
                return true;
        }

        return false;
    }

    private enum CursorKind
    {
        Unknown,
        Pointer,
        Default,
        Hover,
    }
}
