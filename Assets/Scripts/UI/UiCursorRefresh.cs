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
    private static CursorKind _appliedKind;

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

        _appliedKind = CursorKind.Unknown;
        ApplyHardwareCursor();
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
        if (_interactiveHoverDepth > 0)
        {
            ApplyHardwareCursor();
            return;
        }

        ScheduleApplyHardwareCursor();
    }

    public static void ResetInteractiveHover()
    {
        _interactiveHoverDepth = 0;
        _appliedKind = CursorKind.Unknown;
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
        ApplyKind(ResolveCursorKind(PickBestElementAtMouse()));
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

        ApplyKind(ResolveCursorKind(null));
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
            var picked = panel.Pick(panelPos);
            if (picked != null)
                return picked;

            // Full-screen Ignore hosts (vendor/loadout overlays) make Pick()
            // return null beside chrome. Keep the higher panel if a blocking
            // window still contains the pointer so HUD/world cannot steal it.
            return UiPointerState.PickBlockingElement(panel, panelPos);
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

    private static void ApplyKind(CursorKind kind)
    {
        Texture2D texture = null;
        var hotspot = DefaultHotspot;
        switch (kind)
        {
            case CursorKind.Trade:
                texture = _tradeCursor != null ? _tradeCursor : _hoverCursor;
                hotspot = _tradeCursor != null ? TradeHotspot : HoverHotspot;
                if (_tradeCursor == null)
                    kind = CursorKind.Hover;
                break;
            case CursorKind.Hover:
                texture = _hoverCursor;
                hotspot = HoverHotspot;
                break;
            case CursorKind.Default:
                texture = _defaultCursor;
                hotspot = DefaultHotspot;
                break;
            case CursorKind.Pointer:
                texture = _pointerCursor;
                hotspot = PointerHotspot;
                break;
            default:
                return;
        }

        if (texture == null || kind == _appliedKind)
            return;

        UnityEngine.Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
        _appliedKind = kind;
    }

    private static CursorKind ResolveCursorKind(VisualElement picked)
    {
        if (picked == null)
        {
            if (_interactiveHoverDepth > 0)
                return CursorKind.Hover;
            return _gameplayPointerEnabled ? CursorKind.Pointer : CursorKind.Default;
        }

        var insideUiChrome = false;
        for (var element = picked; element != null; element = element.parent)
        {
            if (IsTradeCursorElement(element))
                return CursorKind.Trade;
            if (IsHoverCursorElement(element))
                return CursorKind.Hover;
            if (IsUiChromeElement(element))
                insideUiChrome = true;
        }

        if (insideUiChrome)
            return CursorKind.Default;

        if (_interactiveHoverDepth > 0)
            return CursorKind.Hover;

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
        if (!element.enabledSelf)
            return false;

        if (element is Button or TextField or Toggle or Slider or DropdownField or IntegerField or FloatField)
            return true;

        if (element is OrnateButton or AbilityCooldownElement or LoadoutRow)
            return true;

        if (element is VendorRow row)
            return row.Model != null && !row.Model.Locked && !row.Model.CanTransact;

        if (element.ClassListContains("floating-window__header")
            && element.parent != null
            && element.parent.ClassListContains("floating-window--draggable"))
            return true;

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
            "vendor-tab",
            "vendor-page__btn",
            "floating-window__close",
            "unity-base-field__input",
            "unity-text-element__selectable",
            "unity-base-slider__dragger",
            "unity-base-popup-field__arrow",
            "unity-base-popup-field__text");
    }

    private static bool IsUiChromeElement(VisualElement element)
    {
        if (element is Label || element is VendorRow)
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
            "floating-window",
            "floating-window__header",
            "floating-window__title",
            "floating-window__content",
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
            "vendor",
            "vendor-host",
            "vendor-panel",
            "vendor-tabs",
            "vendor-portrait",
            "vendor-identity",
            "vendor-hint",
            "vendor-list",
            "vendor-row",
            "vendor-footer",
            "vendor-gold",
            "vendor-page",
            "vendor-buyback",
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
