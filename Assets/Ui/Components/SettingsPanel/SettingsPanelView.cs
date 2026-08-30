using System;
using UnityEngine.UIElements;

namespace ShadowInfection.UI.SettingsPanel
{
    public enum SettingsTab
    {
        General,
        Graphics,
        Audio,
        Keybindings
    }

    public sealed class SettingsPanelView
    {
        private readonly VisualElement root;
        private readonly Label titleLabel;
        private readonly Button tabGeneral;
        private readonly Button tabGraphics;
        private readonly Button tabAudio;
        private readonly Button tabKeybindings;
        private readonly VisualElement contentGeneral;
        private readonly VisualElement contentGraphics;
        private readonly VisualElement contentAudio;
        private readonly VisualElement contentKeybindings;
        private readonly Button resetButton;
        private readonly Button backButton;

        public event Action ResetClicked;
        public event Action BackClicked;
        public event Action<SettingsTab> TabChanged;

        public VisualElement Root => root;

        public TextField NicknameField { get; }
        public Toggle AudioToggle { get; }
        public SliderInt MasterSlider { get; }
        public SliderInt MusicSlider { get; }
        public SliderInt SfxSlider { get; }
        public SliderInt UiSlider { get; }
        public SliderInt VoiceSlider { get; }
        public SliderInt AmbientSlider { get; }
        public Toggle FullscreenToggle { get; }
        public DropdownField ResolutionDropdown { get; }

        public VisualElement KeybindingsList { get; }
        public VisualElement KeybindingsConflict { get; }
        public Label KeybindingsConflictText { get; }
        public Button KeybindingsConflictSwap { get; }
        public Button KeybindingsConflictCancel { get; }

        public SettingsTab ActiveTab { get; private set; } = SettingsTab.General;

        public SettingsPanelView(VisualElement searchRoot)
        {
            root = searchRoot?.Q<VisualElement>("SettingsPanel") ?? searchRoot;
            if (root == null)
                return;

            titleLabel = root.Q<Label>("SettingsPanelTitle");
            tabGeneral = root.Q<Button>("TabGeneral");
            tabGraphics = root.Q<Button>("TabGraphics");
            tabAudio = root.Q<Button>("TabAudio");
            tabKeybindings = root.Q<Button>("TabKeybindings");
            contentGeneral = root.Q<VisualElement>("TabContentGeneral");
            contentGraphics = root.Q<VisualElement>("TabContentGraphics");
            contentAudio = root.Q<VisualElement>("TabContentAudio");
            contentKeybindings = root.Q<VisualElement>("TabContentKeybindings");

            NicknameField = root.Q<TextField>("TextFieldNickname");
            AudioToggle = root.Q<Toggle>("ToggleAudio");
            MasterSlider = root.Q<SliderInt>("SliderAudioMaster");
            MusicSlider = root.Q<SliderInt>("SliderAudioMusic");
            SfxSlider = root.Q<SliderInt>("SliderAudioSfx");
            UiSlider = root.Q<SliderInt>("SliderAudioUi");
            VoiceSlider = root.Q<SliderInt>("SliderAudioVoice");
            AmbientSlider = root.Q<SliderInt>("SliderAudioAmbient");
            FullscreenToggle = root.Q<Toggle>("ToggleWindowFullscreen");
            ResolutionDropdown = root.Q<DropdownField>("DropdownFieldResolution");

            KeybindingsList = root.Q<VisualElement>("KeybindingsList");
            KeybindingsConflict = root.Q<VisualElement>("KeybindingsConflict");
            KeybindingsConflictText = root.Q<Label>("KeybindingsConflictText");
            KeybindingsConflictSwap = root.Q<Button>("KeybindingsConflictSwap");
            KeybindingsConflictCancel = root.Q<Button>("KeybindingsConflictCancel");

            resetButton = root.Q<Button>("ButtonResetSettings");
            backButton = root.Q<Button>("ButtonBackToMenu");

            if (tabGeneral != null)
                tabGeneral.clicked += () => SetActiveTab(SettingsTab.General);
            if (tabGraphics != null)
                tabGraphics.clicked += () => SetActiveTab(SettingsTab.Graphics);
            if (tabAudio != null)
                tabAudio.clicked += () => SetActiveTab(SettingsTab.Audio);
            if (tabKeybindings != null)
                tabKeybindings.clicked += () => SetActiveTab(SettingsTab.Keybindings);
            if (resetButton != null)
                resetButton.clicked += () => ResetClicked?.Invoke();
            if (backButton != null)
                backButton.clicked += () => BackClicked?.Invoke();

            SetActiveTab(SettingsTab.General);
        }

        /// <summary>
        /// When embedded in an overlay window, hide the panel's own title and BACK —
        /// the host window provides title + close instead.
        /// </summary>
        public void SetEmbeddedInOverlay(bool embedded)
        {
            root?.EnableInClassList("settings-panel--embedded", embedded);
            if (titleLabel != null)
                titleLabel.style.display = embedded ? DisplayStyle.None : DisplayStyle.Flex;
            if (backButton != null)
                backButton.style.display = embedded ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public void SetActiveTab(SettingsTab tab)
        {
            var changed = ActiveTab != tab;
            ActiveTab = tab;
            SetContentVisible(contentGeneral, tab == SettingsTab.General);
            SetContentVisible(contentGraphics, tab == SettingsTab.Graphics);
            SetContentVisible(contentAudio, tab == SettingsTab.Audio);
            SetContentVisible(contentKeybindings, tab == SettingsTab.Keybindings);
            SetTabActive(tabGeneral, tab == SettingsTab.General);
            SetTabActive(tabGraphics, tab == SettingsTab.Graphics);
            SetTabActive(tabAudio, tab == SettingsTab.Audio);
            SetTabActive(tabKeybindings, tab == SettingsTab.Keybindings);
            if (changed)
                TabChanged?.Invoke(tab);
        }

        public void BlurResetButton()
        {
            resetButton?.Blur();
        }

        private static void SetContentVisible(VisualElement content, bool visible)
        {
            if (content == null)
                return;

            content.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static void SetTabActive(Button tab, bool active)
        {
            tab?.EnableInClassList("settings-tab--active", active);
        }
    }
}
