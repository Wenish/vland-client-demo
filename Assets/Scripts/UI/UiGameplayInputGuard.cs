using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Prevents WASD / arrow keys from focusing gameplay HUD buttons while moving.
public static class UiGameplayInputGuard
{
    private static readonly HashSet<VisualElement> RegisteredRoots = new HashSet<VisualElement>();

    public static void Apply(
        VisualElement root,
        bool blockMovementKeys = true,
        bool disableButtonKeyboardFocus = true)
    {
        if (root == null || RegisteredRoots.Contains(root))
            return;

        RegisteredRoots.Add(root);
        root.RegisterCallback<DetachFromPanelEvent>(_ => RegisteredRoots.Remove(root));

        if (disableButtonKeyboardFocus)
            DisableButtonKeyboardFocus(root);

        root.RegisterCallback<NavigationMoveEvent>(evt =>
        {
            evt.StopImmediatePropagation();
            BlurFocusedDescendant(root);
        }, TrickleDown.TrickleDown);

        if (!blockMovementKeys)
            return;

        root.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (!IsGameplayNavigationKey(evt.keyCode))
                return;

            evt.StopImmediatePropagation();
            BlurFocusedDescendant(root);
        }, TrickleDown.TrickleDown);
    }

    public static void DisableButtonKeyboardFocus(VisualElement root)
    {
        if (root == null)
            return;

        root.Query<Button>().ForEach(button => button.focusable = false);
    }

    private static bool IsGameplayNavigationKey(KeyCode keyCode)
    {
        switch (keyCode)
        {
            case KeyCode.W:
            case KeyCode.A:
            case KeyCode.S:
            case KeyCode.D:
            case KeyCode.UpArrow:
            case KeyCode.DownArrow:
            case KeyCode.LeftArrow:
            case KeyCode.RightArrow:
            case KeyCode.Space:
                return true;
            default:
                return false;
        }
    }

    private static void BlurFocusedDescendant(VisualElement root)
    {
        var panel = root.panel;
        if (panel?.focusController?.focusedElement is not VisualElement focused)
            return;

        if (root == focused || focused.FindCommonAncestor(root) == root)
            focused.Blur();
    }
}
