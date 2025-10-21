using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Global helper to track whether pointer is over any registered UI element
public static class UiPointerState
{
    private static readonly HashSet<VisualElement> _registered = new HashSet<VisualElement>();
    private static int _hoverCount = 0;

    public static bool IsPointerOverBlockingElement => _hoverCount > 0;

    public static void RegisterBlockingElement(VisualElement element)
    {
        if (element == null) return;
        if (_registered.Contains(element)) return;

        _registered.Add(element);
        element.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
        element.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        element.RegisterCallback<DetachFromPanelEvent>(_ => UnregisterBlockingElement(element));
    }

    public static void UnregisterBlockingElement(VisualElement element)
    {
        if (element == null) return;
        if (!_registered.Remove(element)) return;

        element.UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
        element.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
    }

    private static void OnPointerEnter(PointerEnterEvent e)
    {
        _hoverCount++;
    }

    private static void OnPointerLeave(PointerLeaveEvent e)
    {
        _hoverCount = Mathf.Max(0, _hoverCount - 1);
    }
}
