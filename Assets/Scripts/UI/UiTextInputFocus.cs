using UnityEngine;

/// <summary>
/// Suppresses gameplay and UI hotkeys while a UI Toolkit text field is focused,
/// so typing does not move, cast, ping, or toggle windows. Stays blocking for
/// the rest of the blur frame so Escape cannot open the menu on the same press.
/// </summary>
public static class UiTextInputFocus
{
    private static int depth;
    private static int suppressUntilFrame = -1;

    public static bool IsBlocking => depth > 0 || Time.frameCount == suppressUntilFrame;

    public static void Push()
    {
        depth++;
    }

    public static void Pop()
    {
        if (depth <= 0)
            return;

        depth--;
        if (depth == 0)
            suppressUntilFrame = Time.frameCount;
    }
}
