using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class CastBar : VisualElement
{
    // --------- UXML-settable attributes ---------

    [SerializeField, DontCreateProperty]
    private VisualTreeAsset _template;

    /// <summary>
    /// Assign your CastBar.uxml here (via UI Builder inspector or raw UXML).
    /// </summary>
    [UxmlAttribute, CreateProperty]
    public VisualTreeAsset Template
    {
        get => _template;
        set
        {
            _template = value;
            TryBuildFromTemplate();
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
            if (_iconImage != null)
                _iconImage.style.backgroundImage = new StyleBackground(_iconTexture);
            MarkDirtyRepaint();
        }
    }

    [SerializeField, DontCreateProperty]
    private string _textName;

    [UxmlAttribute, CreateProperty]
    public string TextName
    {
        get => _textName;
        set
        {
            _textName = value;
            if (_labelName != null) _labelName.text = _textName;
        }
    }

    [SerializeField, DontCreateProperty]
    private string _textTime;

    [UxmlAttribute, CreateProperty]
    public string TextTime
    {
        get => _textTime;
        set
        {
            _textTime = value;
            if (_labelTime != null) _labelTime.text = _textTime;
        }
    }

    [SerializeField, DontCreateProperty]
    private float _progress; // 0..1

    /// <summary>Progress in [0..1]</summary>
    [UxmlAttribute, CreateProperty]
    public float Progress
    {
        get => _progress;
        set
        {
            _progress = Mathf.Clamp01(value);
            ApplyProgress();
        }
    }

    // --------- Cached parts (names come from your UXML) ---------
    private VisualElement _iconImage; // name="iconImage"
    private VisualElement _fillBar;   // name="fillBar"
    private Label _labelName;         // name="labelName"
    private Label _labelTime;         // name="labelTime"
    private VisualElement _feedback;    // name="feedback"

    public CastBar()
    {
    }

    private void TryBuildFromTemplate()
    {
        if (_template == null)
            return;

        // If re-assigned at runtime, clear and rebuild
        Clear();

        // Clone the UXML structure INTO this element
        _template.CloneTree(this);

        // Query parts by name
        _iconImage = this.Q<VisualElement>("iconImage");
        _fillBar   = this.Q<VisualElement>("fillBar");
        _labelName = this.Q<Label>("labelName");
        _labelTime = this.Q<Label>("labelTime");
        _feedback = this.Q<VisualElement>("feedback");

        // Apply any attributes already set before the template arrived
        if (_iconImage != null && _iconTexture != null)
            _iconImage.style.backgroundImage = new StyleBackground(_iconTexture);

        if (_labelName != null) _labelName.text = _textName;
        if (_labelTime != null) _labelTime.text = _textTime;
        ApplyProgress();
    }

    private void ApplyProgress()
    {
        if (_fillBar == null) return;
        _fillBar.style.width = Length.Percent(Mathf.Clamp01(_progress) * 100f);
    }

    // --------- Convenience APIs for runtime control ---------
    public void SetProgress(float t)
    {
        Progress = t; // routes through property
    }

    public void SetText(string name, string time)
    {
        TextName = name;
        TextTime = time;
    }

    public void SetIcon(Texture2D tex)
    {
        IconTexture = tex;
    }

    public void SetFeedbackColor(Color color)
    {
        if (_feedback != null)
        {
            _feedback.style.backgroundColor = color;
        }
    }

    public void ShowFeedback(bool show)
    {
        if (_feedback != null)
        {
            _feedback.style.visibility = show ? Visibility.Visible : Visibility.Hidden;
        }
    }
}
