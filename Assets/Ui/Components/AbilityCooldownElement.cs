using System;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class AbilityCooldownElement : VisualElement
{
    private Image _iconImage;
    private VisualElement _iconOverlay;
    private VisualElement _cooldownOverlay;
    private Label _cooldownLabel;


    [SerializeField, DontCreateProperty]
    private float _cooldownRemaining;
    [UxmlAttribute, CreateProperty]
    public float CooldownRemaining 
    {
        get => _cooldownRemaining;
        set
        {

            _cooldownRemaining = Math.Clamp(value, 0f, float.PositiveInfinity);
            _cooldownLabel.visible = _cooldownRemaining > 0f;
            _cooldownLabel.text = _cooldownRemaining < 1 ? _cooldownRemaining.ToString("F1") : Mathf.FloorToInt(_cooldownRemaining).ToString();
            // MarkDirtyRepaint();
        }
    }

    [SerializeField, DontCreateProperty]
    private float _cooldownProgress;

    [UxmlAttribute, CreateProperty]
    public float CooldownProgress 
    {
        get => _cooldownProgress;
        set
        {
            _cooldownProgress = Mathf.Clamp(value, 0f, 100f);
            _cooldownOverlay.style.height = Length.Percent(_cooldownProgress);
            _iconOverlay.style.visibility = _cooldownProgress > 0f ? Visibility.Visible : Visibility.Hidden;
            // MarkDirtyRepaint();
        }
    }

    [SerializeField, DontCreateProperty]
    private Texture2D _iconTexture;
    [UxmlAttribute, CreateProperty]
    public Texture2D IconTexture 
    {
        get => _iconTexture;
        set
        {
            _iconTexture = value;
            _iconImage.image = value;
            MarkDirtyRepaint();
        }
    }

    public AbilityCooldownElement()
    {
        AddToClassList("ability-container");

        _iconImage = new Image { name = "Icon" };
        _iconImage.AddToClassList("ability-icon");
        Add(_iconImage);

        _iconOverlay = new VisualElement { name = "IconOverlay" };
        _iconOverlay.AddToClassList("icon-overlay");
        Add(_iconOverlay);

        _cooldownOverlay = new VisualElement { name = "CooldownOverlay" };
        _cooldownOverlay.AddToClassList("cooldown-overlay");
        Add(_cooldownOverlay);

        var  cooldownLabelContainer = new VisualElement { name = "CooldownLabelContainer" };
        cooldownLabelContainer.AddToClassList("cooldown-label-container");
        Add(cooldownLabelContainer);


        _cooldownLabel = new Label { name = "CooldownLabel" };
        _cooldownLabel.AddToClassList("cooldown-label");
        cooldownLabelContainer.Add(_cooldownLabel);
    }

    public void SetIcon(Texture2D texture)
    {
        _iconImage.image = texture;
    }
}
