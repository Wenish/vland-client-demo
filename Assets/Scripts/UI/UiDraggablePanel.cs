using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShadowInfection.UI
{
    public sealed class UiDraggablePanel
    {
        public const float DefaultViewportMargin = 20f;

        private readonly VisualElement host;
        private readonly VisualElement panel;
        private readonly VisualElement titleBar;
        private readonly Func<bool, (float left, float top)> defaultPosition;
        private readonly Func<VisualElement, bool> isDragExcluded;

        private bool dragging;
        private Vector2 dragPointerStart;
        private Vector2 dragPanelStart;
        private int dragPointerId;
        private bool isOpen;
        private float posLeft;
        private float posTop;
        private float writtenLeft = float.NaN;
        private float writtenTop = float.NaN;
        private bool hasPosition;

        public event Action PositionChanged;

        public VisualElement Panel => panel;
        public bool HasPosition => hasPosition;
        public bool IsDragging => dragging;

        public UiDraggablePanel(
            VisualElement host,
            VisualElement panel,
            VisualElement titleBar,
            Func<bool, (float left, float top)> defaultPosition = null,
            Func<VisualElement, bool> isDragExcluded = null)
        {
            this.host = host;
            this.panel = panel;
            this.titleBar = titleBar;
            this.defaultPosition = defaultPosition;
            this.isDragExcluded = isDragExcluded;

            if (titleBar != null)
            {
                titleBar.pickingMode = PickingMode.Position;
                titleBar.RegisterCallback<PointerDownEvent>(OnTitlePointerDown, TrickleDown.TrickleDown);
            }

            if (panel != null)
            {
                panel.RegisterCallback<PointerMoveEvent>(OnPanelPointerMove, TrickleDown.TrickleDown);
                panel.RegisterCallback<PointerUpEvent>(OnPanelPointerUp, TrickleDown.TrickleDown);
            }

            if (host != null)
                host.RegisterCallback<GeometryChangedEvent>(_ => OnGeometryChanged());
            if (panel != null)
                panel.RegisterCallback<GeometryChangedEvent>(_ => OnGeometryChanged());
        }

        public void SetOpen(bool open)
        {
            isOpen = open;
        }

        public void ApplyPosition(float left, float top)
        {
            hasPosition = true;
            posLeft = left;
            posTop = top;
            WritePosition(left, top);
            ClampToViewport();
        }

        public void ApplyDefaultPosition()
        {
            hasPosition = false;
            if (!TryComputeDefaultPosition(out var left, out var top))
                return;

            ApplyPosition(left, top);
        }

        public Vector2 GetPosition()
        {
            if (hasPosition)
                return new Vector2(posLeft, posTop);

            if (TryComputeDefaultPosition(out var left, out var top))
                return new Vector2(left, top);

            return Vector2.zero;
        }

        public bool HasUsableLayout()
        {
            return TryGetLayoutSize(out _, out _, out _, out _);
        }

        public void ClampToViewport()
        {
            if (!isOpen || !TryGetLayoutSize(out var hostWidth, out var hostHeight, out var width, out var height))
                return;

            var left = hasPosition ? posLeft : panel.resolvedStyle.left;
            var top = hasPosition ? posTop : panel.resolvedStyle.top;
            var maxLeft = Mathf.Max(DefaultViewportMargin, hostWidth - width - DefaultViewportMargin);
            var maxTop = Mathf.Max(DefaultViewportMargin, hostHeight - height - DefaultViewportMargin);
            left = Mathf.Clamp(left, DefaultViewportMargin, maxLeft);
            top = Mathf.Clamp(top, DefaultViewportMargin, maxTop);
            posLeft = left;
            posTop = top;
            hasPosition = true;
            WritePosition(left, top);
        }

        public static bool ClampPosition(
            float hostWidth,
            float hostHeight,
            float panelWidth,
            float panelHeight,
            ref float left,
            ref float top,
            float margin = DefaultViewportMargin)
        {
            if (hostWidth <= 0f || hostHeight <= 0f || panelWidth <= 0f || panelHeight <= 0f)
                return false;

            var maxLeft = Mathf.Max(margin, hostWidth - panelWidth - margin);
            var maxTop = Mathf.Max(margin, hostHeight - panelHeight - margin);
            left = Mathf.Clamp(left, margin, maxLeft);
            top = Mathf.Clamp(top, margin, maxTop);
            return true;
        }

        private void OnGeometryChanged()
        {
            if (!isOpen || dragging)
                return;

            if (!hasPosition)
                ApplyDefaultPosition();
            else
                ClampToViewport();
        }

        private void WritePosition(float left, float top)
        {
            if (panel == null)
                return;
            if (Mathf.Approximately(writtenLeft, left) && Mathf.Approximately(writtenTop, top))
                return;

            writtenLeft = left;
            writtenTop = top;
            panel.style.position = Position.Absolute;
            panel.style.left = left;
            panel.style.top = top;
            panel.style.right = StyleKeyword.Auto;
            panel.style.bottom = StyleKeyword.Auto;
        }

        private bool TryComputeDefaultPosition(out float left, out float top)
        {
            left = 0f;
            top = 0f;
            if (defaultPosition != null)
            {
                var computed = defaultPosition(hasPosition);
                left = computed.left;
                top = computed.top;
                return true;
            }

            if (!TryGetLayoutSize(out var hostWidth, out var hostHeight, out var width, out var height))
                return false;

            left = Mathf.Clamp(hostWidth * 0.06f, 72f, 128f);
            top = (hostHeight - height) * 0.42f;
            return true;
        }

        public bool TryGetLayoutSize(out float hostWidth, out float hostHeight, out float width, out float height)
        {
            hostWidth = 0f;
            hostHeight = 0f;
            width = 0f;
            height = 0f;
            if (panel == null || host == null || host.panel == null)
                return false;
            if (host.resolvedStyle.display == DisplayStyle.None)
                return false;

            hostWidth = host.resolvedStyle.width;
            hostHeight = host.resolvedStyle.height;
            width = panel.resolvedStyle.width;
            height = panel.resolvedStyle.height;
            return hostWidth > 0f && hostHeight > 0f && width > 0f && height > 0f;
        }

        private void OnTitlePointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || panel == null || titleBar == null)
                return;
            if (IsDragExcluded(evt.target as VisualElement))
                return;

            dragging = true;
            dragPointerId = evt.pointerId;
            dragPointerStart = (Vector2)evt.position;
            dragPanelStart = hasPosition
                ? new Vector2(posLeft, posTop)
                : new Vector2(panel.resolvedStyle.left, panel.resolvedStyle.top);
            panel.CapturePointer(evt.pointerId);
            evt.StopImmediatePropagation();
        }

        private void OnPanelPointerMove(PointerMoveEvent evt)
        {
            if (!dragging || panel == null || evt.pointerId != dragPointerId)
                return;

            var delta = (Vector2)evt.position - dragPointerStart;
            ApplyPosition(dragPanelStart.x + delta.x, dragPanelStart.y + delta.y);
            evt.StopImmediatePropagation();
        }

        private void OnPanelPointerUp(PointerUpEvent evt)
        {
            if (!dragging || evt.pointerId != dragPointerId)
                return;

            EndDrag();
            evt.StopImmediatePropagation();
        }

        private void EndDrag()
        {
            if (!dragging)
                return;

            dragging = false;
            if (panel != null && panel.HasPointerCapture(dragPointerId))
                panel.ReleasePointer(dragPointerId);
            PositionChanged?.Invoke();
        }

        private bool IsDragExcluded(VisualElement target)
        {
            if (isDragExcluded != null && isDragExcluded(target))
                return true;

            for (var element = target; element != null; element = element.parent)
            {
                if (element == titleBar || element == panel)
                    return false;
                if (element is Button || element.name == "closeButton")
                    return true;
            }

            return false;
        }
    }
}
