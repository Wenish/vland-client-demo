using System.Linq;
using Mirror;
using UnityEngine;

// Optional in-game debug UI to test loadout changes quickly.
public class LoadoutDebugPanel : MonoBehaviour
{
    private string unitName = "Player";
    // selection indices
    private int selWeapon = -1;
    private int selNormal1 = -1;
    private int selNormal2 = -1;
    private int selNormal3 = -1;
    private int selUltimate = -1;
    private int selPassive1 = -1;
    private int selPassive2 = -1;

    // data options
    private string[] weaponOptions;
    private string[] normalSkillOptions;
    private string[] ultimateSkillOptions;
    private string[] passiveSkillOptions;

    // dropdown state
    private bool openWeapon, openNormal1, openNormal2, openNormal3, openUltimate, openPassive1;
    private Vector2 scrollWeapon, scrollNormal1, scrollNormal2, scrollNormal3, scrollUltimate, scrollPassive1;

    public PlayerLoadout _playerLoudout;

    public DatabaseManager databaseManager;

    void Start()
    {
        unitName = ApplicationSettings.Instance.Nickname;
        databaseManager = DatabaseManager.Instance;
        RefreshOptions();
    }

    void Update()
    {
        if (_playerLoudout == null)
        {
            _playerLoudout = FindObjectsByType<PlayerLoadout>(FindObjectsSortMode.None).FirstOrDefault(p => p.isLocalPlayer);
        }

        // If DB was assigned later for some reason
        if ((weaponOptions == null || normalSkillOptions == null || ultimateSkillOptions == null || passiveSkillOptions == null) && databaseManager != null)
            RefreshOptions();
    }

    void OnGUI()
    {
        if (_playerLoudout == null) return;

        GUILayout.BeginArea(new Rect(Screen.width - 330, Screen.height - 620, 320, 500), GUI.skin.box);
        var leftStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft };

        GUILayout.Label("Loadout Debug Panel", leftStyle);
        unitName = LabeledTextField("Name", unitName);

        GUILayout.Space(4);
        GUILayout.Label("Weapon", leftStyle);
        DrawDropdown(ref selWeapon, ref openWeapon, ref scrollWeapon, weaponOptions, "Select Weapon");

        GUILayout.Space(6);
        GUILayout.Label("Normal Skills (Q/E/R)", leftStyle);
        DrawDropdown(ref selNormal1, ref openNormal1, ref scrollNormal1, normalSkillOptions, "Q");
        DrawDropdown(ref selNormal2, ref openNormal2, ref scrollNormal2, normalSkillOptions, "E");
        DrawDropdown(ref selNormal3, ref openNormal3, ref scrollNormal3, normalSkillOptions, "R");

        GUILayout.Space(6);
        GUILayout.Label("Ultimate (F)", leftStyle);
        DrawDropdown(ref selUltimate, ref openUltimate, ref scrollUltimate, ultimateSkillOptions, "Ultimate");

        GUILayout.Space(6);
        GUILayout.Label("Passive", leftStyle);
        DrawDropdown(ref selPassive1, ref openPassive1, ref scrollPassive1, passiveSkillOptions, "Passive 1");
        // DrawDropdown(ref selPassive2, ref openPassive2, ref scrollPassive2, passiveSkillOptions, "Passive 2");

        if (GUILayout.Button("Apply Loadout"))
        {
            var weaponName = GetSelectedOrEmpty(weaponOptions, selWeapon);
            var normals = new[]
            {
                GetSelectedOrEmpty(normalSkillOptions, selNormal1),
                GetSelectedOrEmpty(normalSkillOptions, selNormal2),
                GetSelectedOrEmpty(normalSkillOptions, selNormal3)
            };
            var passives = new[]
            {
                GetSelectedOrEmpty(passiveSkillOptions, selPassive1),
                GetSelectedOrEmpty(passiveSkillOptions, selPassive2)
            };
            var ultimate = GetSelectedOrEmpty(ultimateSkillOptions, selUltimate);

            _playerLoudout.CmdRequestSetLoadout(unitName, weaponName, normals, ultimate, passives);
        }

        if (!_playerLoudout.LastLoadoutOk && !string.IsNullOrEmpty(_playerLoudout.LastLoadoutError))
        {
            GUILayout.Label($"Error: {_playerLoudout.LastLoadoutError}");
        }
        GUILayout.EndArea();
    }

    private void RefreshOptions()
    {
        if (databaseManager == null) return;

        // Weapons
        if (databaseManager.weaponDatabase != null)
        {
            weaponOptions = databaseManager.weaponDatabase.allWeapons
                .Where(w => w != null && !string.IsNullOrWhiteSpace(w.weaponName))
                .Select(w => w.weaponName)
                .Distinct()
                .OrderBy(n => n)
                .ToArray();
        }

        // Skills by type
        if (databaseManager.skillDatabase != null)
        {
            var all = databaseManager.skillDatabase.allSkills.Where(s => s != null && !string.IsNullOrWhiteSpace(s.skillName));
            normalSkillOptions = all.Where(s => s.skillType == SkillType.Normal).Select(s => s.skillName).Distinct().OrderBy(n => n).ToArray();
            ultimateSkillOptions = all.Where(s => s.skillType == SkillType.Ultimate).Select(s => s.skillName).Distinct().OrderBy(n => n).ToArray();
            passiveSkillOptions = all.Where(s => s.skillType == SkillType.Passive).Select(s => s.skillName).Distinct().OrderBy(n => n).ToArray();
        }

        // Reset selections if out of range
        ValidateSelection(ref selWeapon, weaponOptions);
        ValidateSelection(ref selNormal1, normalSkillOptions);
        ValidateSelection(ref selNormal2, normalSkillOptions);
        ValidateSelection(ref selNormal3, normalSkillOptions);
        ValidateSelection(ref selUltimate, ultimateSkillOptions);
        ValidateSelection(ref selPassive1, passiveSkillOptions);
        ValidateSelection(ref selPassive2, passiveSkillOptions);
    }

    private void ValidateSelection(ref int index, string[] options)
    {
        if (options == null || options.Length == 0) { index = -1; return; }
        if (index < -1 || index >= options.Length) index = -1;
    }

    private string GetSelectedOrEmpty(string[] options, int index)
    {
        if (options == null || index < 0 || index >= options.Length) return string.Empty;
        return options[index];
    }

    private void DrawDropdown(ref int selectedIndex, ref bool isOpen, ref Vector2 scroll, string[] options, string placeholder)
    {
        GUILayout.BeginHorizontal();
        var label = (options != null && selectedIndex >= 0 && selectedIndex < (options?.Length ?? 0)) ? options[selectedIndex] : placeholder;
        if (GUILayout.Button(label, GUILayout.Width(280)))
        {
            // toggle open, and close others implicitly by letting caller manage flags
            isOpen = !isOpen;
            // close other dropdowns by setting all flags false except this one
            if (isOpen) CloseAllDropdownsExcept(ref isOpen);
        }
        GUILayout.EndHorizontal();

        if (isOpen)
        {
            GUILayout.BeginVertical("box");
            if (options == null || options.Length == 0)
            {
                GUILayout.Label("No options");
            }
            else
            {
                scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(120));
                for (int i = 0; i < options.Length; i++)
                {
                    if (GUILayout.Button(options[i]))
                    {
                        selectedIndex = i;
                        isOpen = false;
                    }
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();
        }
    }

    private void CloseAllDropdownsExcept(ref bool keepOpen)
    {
        // Close all, then set the one passed by ref back to true
        openWeapon = openNormal1 = openNormal2 = openNormal3 = openUltimate = openPassive1 = false;
        keepOpen = true;
    }

    private string LabeledTextField(string label, string text)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(60));
        text = GUILayout.TextField(text, GUILayout.Width(220));
        GUILayout.EndHorizontal();
        return text;
    }
}
