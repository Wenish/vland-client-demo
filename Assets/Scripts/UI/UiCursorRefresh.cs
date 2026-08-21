using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Vland.UI;

// Drives hardware cursors for gameplay (pointer), interactive UI (hover),
// buyable vendor rows (trade), and non-interactive UI (default).
// UnityEngine.Cursor.SetCursor is used because UI Toolkit USS cursors do not
// reliably win once a hardware cursor is set.
public static class UiCursorRefresh
{
    public static readonly Vector2 PointerHotspot = new Vector2(16f, 16f);
    public static readonly Vector2 HoverHotspot = Vector2.zero;
    public static readonly Vector2 DefaultHotspot = Vector2.zero;
    public static readonly Vector2 TradeHotspot = Vector2.zero;

    private static Texture2D _pointerCursor;
    private static Texture2D _hoverCursor;
    private static Texture2D _defaultCursor;
    private static Texture2D _tradeCursor;

    private static bool _gameplayPointerEnabled;
    private static int _interactiveHoverDepth;
    private static int _tradeHoverDepth;

    private static readonly HashSet<IPanel> HookedPanels = new HashSet<IPanel>();
    private static readonly HashSet<VisualElement> AttachedRoots = new HashSet<VisualElement>();
    private static readonly Dictionary<IPanel, int> PanelSortingOrders = new Dictionary<IPanel, int>();

    public static void Configure(
        Texture2D pointerCursor,
        Texture2D hoverCursor,
        Texture2D defaultCursor,
        Texture2D tradeCursor = null)
    {
        _pointerCursor = pointerCursor;
        _hoverCursor = hoverCursor;
        _defaultCursor = defaultCursor;
        if (tradeCursor != null)
            _tradeCursor = tradeCursor;
    }

    public static void SetTradeCursor(Texture2D tradeCursor)
    {
        if (tradeCursor != null)
            _tradeCursor = tradeCursor;

        ApplyHardwareCursor();
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

        // DetachFromPanel can fire while the panel layout store is torn down.
        // Pick() would ValidateLayout and NRE; hover-only updates are safe.
        if (_interactiveHoverDepth > 0 || _tradeHoverDepth > 0)
        {
            ApplyHardwareCursor();
            return;
        }

        ScheduleApplyHardwareCursor();
    }

    public static void PushTradeHover()
    {
        _tradeHoverDepth++;
        ApplyHardwareCursor();
    }

    public static void PopTradeHover()
    {
        if (_tradeHoverDepth > 0)
            _tradeHoverDepth--;

        if (_tradeHoverDepth > 0 || _interactiveHoverDepth > 0)
        {
            ApplyHardwareCursor();
            return;
        }

        ScheduleApplyHardwareCursor();
    }

    public static void ResetInteractiveHover()
    {
        _interactiveHoverDepth = 0;
        _tradeHoverDepth = 0;
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
        if (_tradeHoverDepth > 0)
        {
            if (_tradeCursor != null)
                UnityEngine.Cursor.SetCursor(_tradeCursor, TradeHotspot, CursorMode.Auto);
            else if (_hoverCursor != null)
                UnityEngine.Cursor.SetCursor(_hoverCursor, HoverHotspot, CursorMode.Auto);
            return;
        }

        if (_interactiveHoverDepth > 0)
        {
            if (_hoverCursor != null)
                UnityEngine.Cursor.SetCursor(_hoverCursor, HoverHotspot, CursorMode.Auto);
            return;
        }

        ApplyHardwareCursorForPick(PickBestElementAtMouse());
    }

    private static void ScheduleApplyHardwareCursor()
    {
        foreach (var root in AttachedRoots)
        {
            if (root?.panel == null)
                continue;

            root.schedule.Execute(ApplyHardwareCursor);
            return;
        }

        ApplyHardwareCursorForPick(null);
    }

    private static VisualElement PickBestElementAtMouse()
    {
        Vector2 screenPos = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        HookedPanels.RemoveWhere(panel => panel?.visualTree == null);

        foreach (var panel in HookedPanels.OrderByDescending(GetPanelSortingOrder))
        {
            var picked = TryPick(panel, screenPos);
            if (picked != null)
                return picked;
        }

        return null;
    }

    private static VisualElement TryPick(IPanel panel, Vector2 screenPos)
    {
        if (panel?.visualTree == null)
            return null;

        try
        {
            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(panel, screenPos);
            return panel.Pick(panelPos);
        }
        catch (System.NullReferenceException)
        {
            // Unity UITK can NRE in LayoutDataAccess while a document hierarchy
            // is rebuilt (DetachFromPanel / ValidateLayout).
            return null;
        }
    }

    private static int GetPanelSortingOrder(IPanel panel)
    {
        return PanelSortingOrders.TryGetValue(panel, out var sortingOrder) ? sortingOrder : 0;
    }

    private static void ApplyHardwareCursorForPick(VisualElement picked)
    {
        switch (ResolveCursorKind(picked))
        {
            case CursorKind.Trade:
                if (_tradeCursor != null)
                    UnityEngine.Cursor.SetCursor(_tradeCursor, TradeHotspot, CursorMode.Auto);
                else if (_hoverCursor != null)
                    UnityEngine.Cursor.SetCursor(_hoverCursor, HoverHotspot, CursorMode.Auto);
                break;
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
            if (IsTradeCursorElement(element))
                return CursorKind.Trade;
            if (IsHoverCursorElement(element))
                return CursorKind.Hover;
            if (IsDefaultCursorElement(element))
                hasDefault = true;
        }

        if (hasDefault)
            return CursorKind.Default;

        return _gameplayPointerEnabled ? CursorKind.Pointer : CursorKind.Default;
    }

    private static bool IsTradeCursorElement(VisualElement element)
    {
        if (element is VendorRow row && row.Model != null && row.Model.CanTransact && !row.Model.Locked)
            return true;

        return element.ClassListContains("vendor-row--buyable");
    }

    private static bool IsHoverCursorElement(VisualElement element)
    {
        if (element is Button or TextField or Toggle or Slider or DropdownField or IntegerField or FloatField)
            return true;

        if (element is OrnateButton or AbilityCooldownElement or LoadoutRow)
            return true;

        if (element is VendorRow row)
            return row.Model != null && !row.Model.Locked && !row.Model.CanTransact;

        return HasAnyClass(
            element,
            "room-lobby__ready-button",
            "si-button",
            "si-button__icon",
            "unity-button",
            "unity-button__image",
            "ability-container",
            "loadout-row",
            "loadout-slot",
            "loadout-filter",
            "loadout-open-button",
            "loadout-close-button",
            "vendor-tab",
            "vendor-title",
            "vendor-close-button",
            "vendor-page__btn",
            "unity-base-field__input",
            "unity-text-element__selectable",
            "unity-base-slider__dragger",
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
            "loadout-header-row",
            "loadout-subheading",
            "loadout-overlay",
            "loadout-body",
            "loadout-scroll",
            "loadout-list",
            "loadout-detail",
            "loadout-detail__icon",
            "loadout-detail__name",
            "loadout-detail__meta",
            "loadout-detail__description",
            "loadout-detail__empty",
            "loadout-detail__tags",
            "loadout-slots",
            "loadout-filters",
            "loadout-empty",
            "vendor-panel",
            "vendor-title",
            "vendor-hint",
            "vendor-list",
            "vendor-footer",
            "vendor-tooltip",
            "vendor-empty",
            "loadout-row__icon",
            "loadout-row__body",
            "loadout-row__meta-row",
            "loadout-row__name",
            "loadout-row__meta",
            "loadout-row__summary",
            "loadout-slot__icon",
            "loadout-slot__name",
            "loadout-slot__role",
            "loadout-chip",
            "loadout-open-host",
            "unity-scroll-view__content-viewport",
            "unity-scroll-view__content-container",
            "unity-scroller--vertical",
            "unity-base-slider--vertical",
            "unity-base-slider__tracker",
            "unity-base-slider__fill",
            "unity-base-slider__drag-container",
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
        Trade,
    }
}
