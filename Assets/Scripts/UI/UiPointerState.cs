using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Global helper to track whether pointer is over any registered UI element
public static class UiPointerState
{
    // Track registered elements and whether each is currently hovered
    private static readonly Dictionary<VisualElement, bool> _registered = new Dictionary<VisualElement, bool>();
    private static int _hoverCount = 0;

    public static bool IsPointerOverBlockingElement => _hoverCount > 0;

    /// <summary>
    /// Returns the smallest registered blocking element on this panel that
    /// contains the panel-space point. Used so full-screen Ignore hosts do not
    /// leak cursor picking through to HUD / world panels.
    /// </summary>
    public static VisualElement PickBlockingElement(IPanel panel, Vector2 panelPosition)
    {
        if (panel == null)
            return null;

        VisualElement best = null;
        var bestArea = float.MaxValue;
        foreach (var pair in _registered)
        {
            var element = pair.Key;
            if (element == null || element.panel != panel)
                continue;
            if (!IsDisplayedInHierarchy(element))
                continue;

            var bounds = element.worldBound;
            if (!bounds.Contains(panelPosition))
                continue;

            var area = Mathf.Max(0f, bounds.width) * Mathf.Max(0f, bounds.height);
            if (area >= bestArea)
                continue;

            bestArea = area;
            best = element;
        }

        return best;
    }

    private static bool IsDisplayedInHierarchy(VisualElement element)
    {
        for (var current = element; current != null; current = current.parent)
        {
            if (current.resolvedStyle.display == DisplayStyle.None)
                return false;
        }

        return true;
    }

    public static void RegisterBlockingElement(VisualElement element)
    {
        if (element == null) return;
        if (_registered.ContainsKey(element)) return;

        // Start as not hovered
        _registered[element] = false;
        element.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
        element.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        // Ensure we clean up fully if the element is detached while hovered
        element.RegisterCallback<DetachFromPanelEvent>(_ => UnregisterBlockingElement(element));
    }

    public static void UnregisterBlockingElement(VisualElement element)
    {
        if (element == null) return;

        if (_registered.TryGetValue(element, out bool wasHovered))
        {
            // If it was hovered at the time of removal, reduce global hover count
            if (wasHovered)
            {
                _hoverCount = Mathf.Max(0, _hoverCount - 1);
            }

            _registered.Remove(element);
        }
        else
        {
            // Not registered; nothing to do
            return;
        }
        element.UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
        element.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
    }

    private static void OnPointerEnter(PointerEnterEvent e)
    {
        var ve = e.currentTarget as VisualElement;
        if (ve == null) return;

        if (_registered.TryGetValue(ve, out bool isHovered))
        {
            if (!isHovered)
            {
                _registered[ve] = true;
                _hoverCount++;
            }
        }
    }

    private static void OnPointerLeave(PointerLeaveEvent e)
    {
        var ve = e.currentTarget as VisualElement;
        if (ve == null) return;

        if (_registered.TryGetValue(ve, out bool isHovered))
        {
            if (isHovered)
            {
                _registered[ve] = false;
                _hoverCount = Mathf.Max(0, _hoverCount - 1);
            }
        }
    }
}
