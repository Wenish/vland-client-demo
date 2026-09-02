using UnityEngine;

/// <summary>
/// Suppresses world/camera gameplay input while a true modal UI overlay is open
/// (in-game menu, room lobby, character select/create, etc.). Floating panels
/// (character, inventory, vendor) use pointer-hover blocking instead.
/// </summary>
public static class UiModalInputBlock
{
    private static int depth;

    public static bool IsBlocked => depth > 0;

    public static void Push()
    {
        depth++;
    }

    public static void Pop()
    {
        depth = Mathf.Max(0, depth - 1);
    }

    public static void SetActive(bool active)
    {
        if (active)
        {
            if (depth == 0)
                depth = 1;
        }
        else
        {
            depth = 0;
        }
    }
}
