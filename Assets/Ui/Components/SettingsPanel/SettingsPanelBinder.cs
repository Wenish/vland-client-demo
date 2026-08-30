using System.Collections.Generic;
using ShadowInfection.DI;
using ShadowInfection.Input;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShadowInfection.UI.SettingsPanel
{
    public sealed class SettingsPanelBinder
    {
        private const int AudioDefaultValue = 100;

        private readonly SettingsPanelView view;
        private readonly KeybindingsPanelBinder keybindings;
        private ApplicationSettings settings;

        private EventCallback<ChangeEvent<int>> masterCallback;
        private EventCallback<ChangeEvent<int>> musicCallback;
        private EventCallback<ChangeEvent<int>> sfxCallback;
        private EventCallback<ChangeEvent<int>> uiCallback;
        private EventCallback<ChangeEvent<int>> voiceCallback;
        private EventCallback<ChangeEvent<int>> ambientCallback;
        private EventCallback<ChangeEvent<bool>> audioToggleCallback;
        private EventCallback<ChangeEvent<bool>> fullscreenToggleCallback;
        private EventCallback<ChangeEvent<string>> resolutionDropdownCallback;
        private EventCallback<ChangeEvent<string>> nicknameFieldCallback;

        private bool isBound;

        public SettingsPanelBinder(SettingsPanelView view)
        {
            this.view = view;
            keybindings = new KeybindingsPanelBinder(view);
        }

        public void Bind(ApplicationSettings nextSettings)
        {
            Unbind();
            settings = nextSettings;
            if (view == null || settings == null)
                return;

            masterCallback = evt => settings.SetMasterVolume(evt.newValue);
            musicCallback = evt => settings.SetMusicVolume(evt.newValue);
            sfxCallback = evt => settings.SetSfxVolume(evt.newValue);
            uiCallback = evt => settings.SetUiVolume(evt.newValue);
            voiceCallback = evt => settings.SetVoiceVolume(evt.newValue);
            ambientCallback = evt => settings.SetAmbientVolume(evt.newValue);
            audioToggleCallback = evt => settings.SetAudioEnabled(evt.newValue);
            fullscreenToggleCallback = evt => settings.SetWindowedFullscreenEnabled(evt.newValue);
            resolutionDropdownCallback = evt =>
            {
                if (view.ResolutionDropdown != null)
                    settings.SetResolutionIndex(view.ResolutionDropdown.index);
            };
            nicknameFieldCallback = evt => settings.SetNickname(evt.newValue);

            LoadAndApplyCurrentSettings();
            RegisterCallbacks();
            view.ResetClicked += OnResetClicked;
            keybindings.Bind(
                GameServices.Get<IInputBindingSession>(),
                GameServices.Get<IInputBindingCommands>());
            isBound = true;
        }

        public void Unbind()
        {
            if (!isBound)
                return;

            UnregisterCallbacks();
            if (view != null)
                view.ResetClicked -= OnResetClicked;

            keybindings.Unbind();
            settings = null;
            isBound = false;
        }

        public void Refresh()
        {
            LoadAndApplyCurrentSettings();
            keybindings.Refresh();
        }

        private void RegisterCallbacks()
        {
            if (view.MasterSlider != null)
                view.MasterSlider.RegisterValueChangedCallback(masterCallback);
            if (view.MusicSlider != null)
                view.MusicSlider.RegisterValueChangedCallback(musicCallback);
            if (view.SfxSlider != null)
                view.SfxSlider.RegisterValueChangedCallback(sfxCallback);
            if (view.UiSlider != null)
                view.UiSlider.RegisterValueChangedCallback(uiCallback);
            if (view.VoiceSlider != null)
                view.VoiceSlider.RegisterValueChangedCallback(voiceCallback);
            if (view.AmbientSlider != null)
                view.AmbientSlider.RegisterValueChangedCallback(ambientCallback);
            if (view.AudioToggle != null)
                view.AudioToggle.RegisterValueChangedCallback(audioToggleCallback);
            if (view.FullscreenToggle != null)
                view.FullscreenToggle.RegisterValueChangedCallback(fullscreenToggleCallback);
            if (view.ResolutionDropdown != null)
                view.ResolutionDropdown.RegisterValueChangedCallback(resolutionDropdownCallback);
            if (view.NicknameField != null)
                view.NicknameField.RegisterValueChangedCallback(nicknameFieldCallback);
        }

        private void UnregisterCallbacks()
        {
            if (view == null)
                return;

            if (view.MasterSlider != null && masterCallback != null)
                view.MasterSlider.UnregisterValueChangedCallback(masterCallback);
            if (view.MusicSlider != null && musicCallback != null)
                view.MusicSlider.UnregisterValueChangedCallback(musicCallback);
            if (view.SfxSlider != null && sfxCallback != null)
                view.SfxSlider.UnregisterValueChangedCallback(sfxCallback);
            if (view.UiSlider != null && uiCallback != null)
                view.UiSlider.UnregisterValueChangedCallback(uiCallback);
            if (view.VoiceSlider != null && voiceCallback != null)
                view.VoiceSlider.UnregisterValueChangedCallback(voiceCallback);
            if (view.AmbientSlider != null && ambientCallback != null)
                view.AmbientSlider.UnregisterValueChangedCallback(ambientCallback);
            if (view.AudioToggle != null && audioToggleCallback != null)
                view.AudioToggle.UnregisterValueChangedCallback(audioToggleCallback);
            if (view.FullscreenToggle != null && fullscreenToggleCallback != null)
                view.FullscreenToggle.UnregisterValueChangedCallback(fullscreenToggleCallback);
            if (view.ResolutionDropdown != null && resolutionDropdownCallback != null)
                view.ResolutionDropdown.UnregisterValueChangedCallback(resolutionDropdownCallback);
            if (view.NicknameField != null && nicknameFieldCallback != null)
                view.NicknameField.UnregisterValueChangedCallback(nicknameFieldCallback);
        }

        private void LoadAndApplyCurrentSettings()
        {
            if (view == null || settings == null)
                return;

            if (view.AudioToggle != null)
                view.AudioToggle.SetValueWithoutNotify(settings.IsAudioEnabled);
            if (view.MasterSlider != null)
                view.MasterSlider.SetValueWithoutNotify(settings.AudioMasterVolume);
            if (view.MusicSlider != null)
                view.MusicSlider.SetValueWithoutNotify(settings.AudioMusicVolume);
            if (view.SfxSlider != null)
                view.SfxSlider.SetValueWithoutNotify(settings.AudioSfxVolume);
            if (view.UiSlider != null)
                view.UiSlider.SetValueWithoutNotify(settings.AudioUiVolume);
            if (view.VoiceSlider != null)
                view.VoiceSlider.SetValueWithoutNotify(settings.AudioVoiceVolume);
            if (view.AmbientSlider != null)
                view.AmbientSlider.SetValueWithoutNotify(settings.AudioAmbientVolume);
            if (view.FullscreenToggle != null)
                view.FullscreenToggle.SetValueWithoutNotify(settings.IsWindowedFullscreenEnabled);

            if (view.ResolutionDropdown != null)
            {
                view.ResolutionDropdown.choices = GetResolutionChoices();
                view.ResolutionDropdown.index = settings.SelectedResolutionIndex;
            }

            if (view.NicknameField != null)
                view.NicknameField.SetValueWithoutNotify(settings.Nickname);
        }

        private static List<string> GetResolutionChoices()
        {
            var choices = new List<string>();
            foreach (Resolution res in Screen.resolutions)
            {
                choices.Add($"{res.width} x {res.height} @ {(int)res.refreshRateRatio.value}Hz");
            }

            return choices;
        }

        private void OnResetClicked()
        {
            if (keybindings.HandleResetIfActive())
            {
                view?.BlurResetButton();
                return;
            }

            if (settings == null)
                return;

            settings.SetAudioEnabled(true);
            settings.SetMasterVolume(AudioDefaultValue);
            settings.SetMusicVolume(AudioDefaultValue);
            settings.SetSfxVolume(AudioDefaultValue);
            settings.SetUiVolume(AudioDefaultValue);
            settings.SetVoiceVolume(AudioDefaultValue);
            settings.SetAmbientVolume(AudioDefaultValue);
            settings.SetResolutionIndex(settings.GetDefaultResolutionIndex());

            LoadAndApplyCurrentSettings();
            view?.BlurResetButton();
        }
    }
}
