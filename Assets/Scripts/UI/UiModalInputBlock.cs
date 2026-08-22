using UnityEngine;

/// <summary>
/// Suppresses world/camera gameplay input while a modal UI overlay is open
/// (character select/create, etc.). Independent of pointer-hover blocking.
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
