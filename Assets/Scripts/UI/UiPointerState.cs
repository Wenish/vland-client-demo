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

    public static void RegisterBlockingElement(VisualElement element)
    {
        if (element == null) return;
        if (_registered.ContainsKey(element)) return;

        // Start as not hovered
        _registered[element] = false;
        Debug.Log("Registered blocking element count: " + _registered.Count);
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
