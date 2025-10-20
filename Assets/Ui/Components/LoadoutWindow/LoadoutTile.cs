using System;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class LoadoutTile : VisualElement
{
    [SerializeField, DontCreateProperty]
    private Texture2D _icon;

    [UxmlAttribute, CreateProperty]
    public Texture2D Icon
    {
        get => _icon;
        set
        {
            _icon = value;
            if (_iconEl != null)
                _iconEl.style.backgroundImage = _icon != null ? new StyleBackground(_icon) : StyleKeyword.None;
        }
    }

    [SerializeField, DontCreateProperty]
    private string _displayName;

    [UxmlAttribute, CreateProperty]
    public string DisplayName
    {
        get => _displayName;
        set => _displayName = value;
    }

    [SerializeField, DontCreateProperty]
    private string _id;

    [UxmlAttribute, CreateProperty]
    public string Id
    {
        get => _id;
        set => _id = value;
    }

    [SerializeField, DontCreateProperty]
    private bool _selected;

    [UxmlAttribute, CreateProperty]
    public bool Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            EnableInClassList("selected", _selected);
        }
    }

    public event Action<LoadoutTile> Clicked;

    private VisualElement _iconEl;

    public LoadoutTile()
    {
        AddToClassList("loadout-tile");
    style.flexDirection = FlexDirection.Column;
    style.alignItems = Align.Stretch;
    style.justifyContent = Justify.Center;

        _iconEl = new VisualElement { name = "icon" };
        _iconEl.AddToClassList("loadout-tile__icon");
        _iconEl.style.backgroundImage = _icon != null ? new StyleBackground(_icon) : StyleKeyword.None;
        _iconEl.style.flexGrow = 1;
        _iconEl.style.width = Length.Percent(100);
        _iconEl.style.height = Length.Percent(100);
        Add(_iconEl);

        RegisterCallback<ClickEvent>(_ => Clicked?.Invoke(this));

        // Keep square aspect: set height equal to width when layout changes
        RegisterCallback<GeometryChangedEvent>(evt =>
        {
            style.height = evt.newRect.width;
        });
    }
}
