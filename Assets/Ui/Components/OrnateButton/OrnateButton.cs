using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class OrnateButton : Button
{
    public const string UssClassName = "si-button";

    private bool _hoverPlayed;

    public OrnateButton()
    {
        focusable = false;
        AddToClassList(UssClassName);

        RegisterCallback<PointerEnterEvent>(OnPointerEnter);
        RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        RegisterCallback<ClickEvent>(OnClicked, TrickleDown.TrickleDown);
    }

    private void OnPointerEnter(PointerEnterEvent evt)
    {
        if (_hoverPlayed)
        {
            return;
        }

        _hoverPlayed = true;
        PlaySound("UiButtonHover");
    }

    private void OnPointerLeave(PointerLeaveEvent evt)
    {
        _hoverPlayed = false;
    }

    private void OnClicked(ClickEvent evt)
    {
        PlaySound("UiButtonClick");
    }

    private static void PlaySound(string soundName)
    {
        if (SoundManager.Instance == null)
        {
            return;
        }

        SoundManager.Instance.PlaySound(soundName);
    }
}
