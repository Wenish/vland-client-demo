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

        RegisterCallback<PointerEnterEvent>(OnPointerEnter, TrickleDown.TrickleDown);
        RegisterCallback<PointerLeaveEvent>(OnPointerLeave, TrickleDown.TrickleDown);
        RegisterCallback<ClickEvent>(OnClicked, TrickleDown.TrickleDown);
        RegisterCallback<DetachFromPanelEvent>(_ => ReleaseInteractiveHover());
    }

    private void OnPointerEnter(PointerEnterEvent evt)
    {
        UiCursorRefresh.PushInteractiveHover();

        if (_hoverPlayed)
            return;

        _hoverPlayed = true;
        PlaySound("UiButtonHover");
    }

    private void OnPointerLeave(PointerLeaveEvent evt)
    {
        ReleaseInteractiveHover();
    }

    private void OnClicked(ClickEvent evt)
    {
        ReleaseInteractiveHover();
        PlaySound("UiButtonClick");
    }

    private void ReleaseInteractiveHover()
    {
        _hoverPlayed = false;
        UiCursorRefresh.PopInteractiveHover();
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
