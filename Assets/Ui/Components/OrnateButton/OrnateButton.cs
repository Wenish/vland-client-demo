using ShadowInfection.Audio;
using ShadowInfection.DI;
using UnityEngine.UIElements;

[UxmlElement]
public partial class OrnateButton : Button
{
    public const string UssClassName = "si-button";

    private bool _hoverPlayed;
    private bool _cursorPushed;

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
        if (!_cursorPushed)
        {
            _cursorPushed = true;
            UiCursorRefresh.PushInteractiveHover();
        }

        if (_hoverPlayed)
            return;

        _hoverPlayed = true;
        PlayUi(SfxPlayer.Ids.UiButtonHover);
    }

    private void OnPointerLeave(PointerLeaveEvent evt)
    {
        ReleaseInteractiveHover();
    }

    private void OnClicked(ClickEvent evt)
    {
        ReleaseInteractiveHover();
        PlayUi(SfxPlayer.Ids.UiButtonClick);
    }

    private void ReleaseInteractiveHover()
    {
        _hoverPlayed = false;
        if (!_cursorPushed)
            return;

        _cursorPushed = false;
        UiCursorRefresh.PopInteractiveHover();
    }

    private static void PlayUi(string soundId)
    {
        if (GameLifetimeScope.TryResolve<ISfxPlayer>(out var sfx))
            sfx.Play(soundId);
    }
}
