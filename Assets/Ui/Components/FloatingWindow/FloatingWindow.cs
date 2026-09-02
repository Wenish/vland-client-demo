using System;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShadowInfection.UI
{
    public enum FloatingWindowPlacement
    {
        Center,
        CenterLeft,
        CenterRight
    }

    [UxmlElement]
    public partial class FloatingWindow : VisualElement
    {
        public const string UssClassName = "floating-window";
        public const string DraggableUssClassName = "floating-window--draggable";
        public const string HeaderUssClassName = "floating-window__header";
        public const string TitleUssClassName = "floating-window__title";
        public const string CloseUssClassName = "floating-window__close";
        public const string ContentUssClassName = "floating-window__content";

        private readonly VisualElement header;
        private readonly Label titleLabel;
        private readonly OrnateButton closeButton;
        private readonly VisualElement content;

        private UiDraggablePanel dragController;
        private bool isOpen;

        [SerializeField, DontCreateProperty]
        private string titleText = string.Empty;

        [SerializeField, DontCreateProperty]
        private bool isDraggable;

        [SerializeField, DontCreateProperty]
        private float windowWidth;

        [SerializeField, DontCreateProperty]
        private float windowHeight;

        [SerializeField, DontCreateProperty]
        private FloatingWindowPlacement defaultPlacement = FloatingWindowPlacement.Center;

        [SerializeField, DontCreateProperty]
        private float defaultLeft = -1f;

        [SerializeField, DontCreateProperty]
        private float defaultTop = -1f;

        public event Action CloseClicked;
        public event Action PositionChanged;

        public override VisualElement contentContainer => content ?? this;

        public UiDraggablePanel DragController => dragController;

        [UxmlAttribute("title"), CreateProperty]
        public string Title
        {
            get => titleText;
            set
            {
                titleText = value ?? string.Empty;
                if (titleLabel != null)
                    titleLabel.text = titleText;
            }
        }

        [UxmlAttribute("draggable"), CreateProperty]
        public bool IsDraggable
        {
            get => isDraggable;
            set
            {
                isDraggable = value;
                ApplyDraggableState();
                EnsureDragController();
            }
        }

        [UxmlAttribute("width"), CreateProperty]
        public float WindowWidth
        {
            get => windowWidth;
            set
            {
                windowWidth = Mathf.Max(0f, value);
                ApplySize();
            }
        }

        [UxmlAttribute("height"), CreateProperty]
        public float WindowHeight
        {
            get => windowHeight;
            set
            {
                windowHeight = Mathf.Max(0f, value);
                ApplySize();
            }
        }

        [UxmlAttribute("default-placement"), CreateProperty]
        public FloatingWindowPlacement DefaultPlacement
        {
            get => defaultPlacement;
            set => defaultPlacement = value;
        }

        [UxmlAttribute("default-left"), CreateProperty]
        public float DefaultLeft
        {
            get => defaultLeft;
            set => defaultLeft = value;
        }

        [UxmlAttribute("default-top"), CreateProperty]
        public float DefaultTop
        {
            get => defaultTop;
            set => defaultTop = value;
        }

        public FloatingWindow()
        {
            pickingMode = PickingMode.Position;
            AddToClassList("si-panel");
            AddToClassList(UssClassName);

            header = new VisualElement { name = "headerRow", pickingMode = PickingMode.Position };
            header.AddToClassList(HeaderUssClassName);

            titleLabel = new Label(titleText)
            {
                name = "header",
                pickingMode = PickingMode.Ignore
            };
            titleLabel.AddToClassList(TitleUssClassName);

            closeButton = new OrnateButton
            {
                name = "closeButton",
                tooltip = "Close"
            };
            closeButton.AddToClassList("si-button--icon");
            closeButton.AddToClassList(CloseUssClassName);

            var closeIcon = new VisualElement { pickingMode = PickingMode.Ignore };
            closeIcon.AddToClassList("si-button__icon");
            closeButton.Add(closeIcon);
            closeButton.clicked += () => CloseClicked?.Invoke();

            header.Add(titleLabel);
            header.Add(closeButton);

            content = new VisualElement { name = "windowContent", pickingMode = PickingMode.Position };
            content.AddToClassList(ContentUssClassName);

            hierarchy.Add(header);
            hierarchy.Add(content);

            RegisterCallback<AttachToPanelEvent>(_ => EnsureDragController());
        }

        public void SetOpen(bool open)
        {
            isOpen = open;
            dragController?.SetOpen(open);
        }

        public void SetCloseButtonVisible(bool visible)
        {
            if (closeButton == null)
                return;

            closeButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            closeButton.SetEnabled(visible);
        }

        public void ApplyPosition(float left, float top) => dragController?.ApplyPosition(left, top);

        public void ApplyDefaultPosition() => dragController?.ApplyDefaultPosition();

        public Vector2 GetPosition() => dragController != null ? dragController.GetPosition() : Vector2.zero;

        public bool HasUsableLayout() => dragController != null && dragController.HasUsableLayout();

        public void ClampToViewport() => dragController?.ClampToViewport();

        private void ApplyDraggableState()
        {
            EnableInClassList(DraggableUssClassName, isDraggable);
            if (isDraggable)
                style.position = Position.Absolute;
        }

        private void ApplySize()
        {
            if (windowWidth > 0f)
                style.width = windowWidth;
            if (windowHeight > 0f)
                style.height = windowHeight;
        }

        private void EnsureDragController()
        {
            if (!isDraggable || dragController != null || parent == null)
                return;

            dragController = new UiDraggablePanel(parent, this, header, ComputeDefaultPosition);
            dragController.PositionChanged += () => PositionChanged?.Invoke();
            dragController.SetOpen(isOpen);
        }

        private (float left, float top) ComputeDefaultPosition(bool _)
        {
            var hasLeft = defaultLeft >= 0f;
            var hasTop = defaultTop >= 0f;
            if (hasLeft && hasTop)
                return (defaultLeft, defaultTop);

            if (dragController == null
                || !dragController.TryGetLayoutSize(out var hostWidth, out var hostHeight, out var width, out var height))
            {
                return (hasLeft ? defaultLeft : 96f, hasTop ? defaultTop : 120f);
            }

            var left = defaultPlacement switch
            {
                FloatingWindowPlacement.Center => (hostWidth - width) * 0.5f,
                FloatingWindowPlacement.CenterRight => Mathf.Clamp(
                    hostWidth * 0.52f,
                    360f,
                    hostWidth - width - UiDraggablePanel.DefaultViewportMargin),
                _ => Mathf.Clamp(hostWidth * 0.18f, 72f, hostWidth * 0.35f)
            };
            var top = (hostHeight - height) * 0.5f;

            if (hasLeft)
                left = defaultLeft;
            if (hasTop)
                top = defaultTop;

            return (left, top);
        }
    }
}
