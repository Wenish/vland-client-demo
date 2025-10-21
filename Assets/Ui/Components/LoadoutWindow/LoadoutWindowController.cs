using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Vland.UI;

// Attach to a GameObject with a UIDocument. Assign the VisualTree to include LoadoutWindow.uxml and add the USS.
public class LoadoutWindowController : MonoBehaviour
{
    public UIDocument uiDocument;
    public VisualTreeAsset loadoutPanelUxml; // can be LoadoutPanel.uxml now
    public StyleSheet loadoutWindowUss;       // existing loadout element styles
    public StyleSheet loadoutPanelUss;        // right-side panel styles

    private LoadoutWindow _window;

    private DatabaseManager _db => DatabaseManager.Instance;
    private LoadoutManager _loadoutManager => LoadoutManager.Instance;

    private void Awake()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument missing.");
            return;
        }

        var root = uiDocument.rootVisualElement;
        root.styleSheets.Add(loadoutWindowUss);
        if (loadoutPanelUss != null) root.styleSheets.Add(loadoutPanelUss);
        if (loadoutPanelUxml != null)
        {
            loadoutPanelUxml.CloneTree(root);
        }

        _window = root.Q<LoadoutWindow>();
        if (_window == null)
        {
            Debug.LogError("LoadoutWindow element not found in UXML.");
            return;
        }

        // Auto-apply loadout on selection changes (with small debounce to coalesce swaps)
        _window.OnSelectionChanged += HandleSelectionChanged;

        // Default populate and listen to slot changes
        _window.OnActiveSlotChanged += slot =>
        {
            // When user clicks a slot in the slot bar, switch grid list accordingly
            if (slot == LoadoutSlot.Weapon) PopulateGridFor(LoadoutSlot.Weapon);
            else if (slot == LoadoutSlot.Passive) PopulateGridFor(LoadoutSlot.Passive);
            else if (slot == LoadoutSlot.Ultimate) PopulateGridFor(LoadoutSlot.Ultimate);
            else PopulateGridFor(LoadoutSlot.Normal1);
        };
    }

    private void PopulateGridFor(LoadoutSlot slot)
    {
        var items = new List<LoadoutItem>();
        if (_db == null)
        {
            Debug.LogWarning("DatabaseManager missing.");
            return;
        }

        if (slot == LoadoutSlot.Weapon)
        {
            if (_db.weaponDatabase != null)
            {
                foreach (var w in _db.weaponDatabase.allWeapons.Where(w => w != null))
                {
                    items.Add(new LoadoutItem
                    {
                        id = w.weaponName,
                        name = w.weaponName,
                        icon = w.iconTexture,
                        slot = LoadoutSlot.Weapon
                    });
                }
            }
        }
        else if (slot == LoadoutSlot.Passive)
        {
            if (_db.skillDatabase != null)
            {
                foreach (var s in _db.skillDatabase.allSkills.Where(s => s != null && s.skillType == SkillType.Passive))
                {
                    items.Add(new LoadoutItem
                    {
                        id = s.skillName,
                        name = s.skillName,
                        icon = s.iconTexture,
                        slot = LoadoutSlot.Passive
                    });
                }
            }
        }
        else if (slot == LoadoutSlot.Ultimate)
        {
            if (_db.skillDatabase != null)
            {
                foreach (var s in _db.skillDatabase.allSkills.Where(s => s != null && s.skillType == SkillType.Ultimate))
                {
                    items.Add(new LoadoutItem
                    {
                        id = s.skillName,
                        name = s.skillName,
                        icon = s.iconTexture,
                        slot = LoadoutSlot.Ultimate
                    });
                }
            }
        }
        else
        {
            // normals
            if (_db.skillDatabase != null)
            {
                foreach (var s in _db.skillDatabase.allSkills.Where(s => s != null && s.skillType == SkillType.Normal))
                {
                    items.Add(new LoadoutItem
                    {
                        id = s.skillName,
                        name = s.skillName,
                        icon = s.iconTexture,
                        slot = LoadoutSlot.Normal1 // all normals compatible; UI target slot set via _window.SetActiveSlot
                    });
                }
            }
        }

        _window.SetItems(items, slot == LoadoutSlot.Normal1 || slot == LoadoutSlot.Normal2 || slot == LoadoutSlot.Normal3 ? LoadoutSlot.Normal1 : slot);
    }

    private bool _applyPending;
    private Coroutine _applyRoutine;

    private void HandleSelectionChanged(LoadoutSlot slot, string id)
    {
        if (_applyPending) return;
        _applyPending = true;
        _applyRoutine = StartCoroutine(ApplyAtEndOfFrame());
    }

    private System.Collections.IEnumerator ApplyAtEndOfFrame()
    {
        // wait one frame to coalesce multiple events from swaps
        yield return null;
        ApplyCurrentLoadout();
        _applyPending = false;
        _applyRoutine = null;
    }

    private void ApplyCurrentLoadout()
    {
        LocalLoadout newLocalLoadout = new LocalLoadout
        {
            UnitName = ApplicationSettings.Instance?.Nickname ?? "Player",
            WeaponId = _window.GetSelectedId(LoadoutSlot.Weapon) ?? string.Empty,
            PassiveId = _window.GetSelectedId(LoadoutSlot.Passive) ?? string.Empty,
            Normal1Id = _window.GetSelectedId(LoadoutSlot.Normal1) ?? string.Empty,
            Normal2Id = _window.GetSelectedId(LoadoutSlot.Normal2) ?? string.Empty,
            Normal3Id = _window.GetSelectedId(LoadoutSlot.Normal3) ?? string.Empty,
            UltimateId = _window.GetSelectedId(LoadoutSlot.Ultimate) ?? string.Empty
        };

        _loadoutManager.Set(newLocalLoadout);
    }
}
