using UnityEngine;

namespace ShadowInfection.UI
{
    public static class FloatingPanelLayout
    {
        public const string InventoryPosX = "InventoryPanelPosX";
        public const string InventoryPosY = "InventoryPanelPosY";
        public const string CharacterPosX = "CharacterPanelPosX";
        public const string CharacterPosY = "CharacterPanelPosY";
        public const float SideBySideGap = 16f;

        public static bool HasSavedPosition(string prefX, string prefY)
        {
            return PlayerPrefs.HasKey(prefX) && PlayerPrefs.HasKey(prefY);
        }

        public static bool TryReadPosition(string prefX, string prefY, out float left, out float top)
        {
            left = 0f;
            top = 0f;
            if (!HasSavedPosition(prefX, prefY))
                return false;

            left = PlayerPrefs.GetFloat(prefX);
            top = PlayerPrefs.GetFloat(prefY);
            return true;
        }

        public static void WritePosition(string prefX, string prefY, float left, float top)
        {
            PlayerPrefs.SetFloat(prefX, left);
            PlayerPrefs.SetFloat(prefY, top);
        }

        public static bool TryTileSideBySide(UiDraggablePanel leftPanel, UiDraggablePanel rightPanel, float gap = SideBySideGap)
        {
            if (leftPanel == null || rightPanel == null)
                return false;
            if (!leftPanel.HasUsableLayout() || !rightPanel.HasUsableLayout())
                return false;
            if (!leftPanel.TryGetLayoutSize(out var hostWidth, out var hostHeight, out var leftWidth, out var leftHeight))
                return false;
            if (!rightPanel.TryGetLayoutSize(out _, out _, out var rightWidth, out var rightHeight))
                return false;

            var totalWidth = leftWidth + gap + rightWidth;
            var startLeft = Mathf.Max(UiDraggablePanel.DefaultViewportMargin, (hostWidth - totalWidth) * 0.5f);
            var leftTop = (hostHeight - leftHeight) * 0.5f;
            var rightTop = (hostHeight - rightHeight) * 0.5f;

            leftPanel.ApplyPosition(startLeft, leftTop);
            rightPanel.ApplyPosition(startLeft + leftWidth + gap, rightTop);
            return true;
        }

        public static void ClampBoth(UiDraggablePanel leftPanel, UiDraggablePanel rightPanel)
        {
            leftPanel?.ClampToViewport();
            rightPanel?.ClampToViewport();
        }
    }
}
