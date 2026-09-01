using System;
using System.Collections.Generic;
using ShadowInfection.DI;
using ShadowInfection.Items;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class CharacterManager : MonoBehaviour
{
    public const int MaxCharacters = 6;
    public const string MaleModelName = "ninja";
    public const string FemaleModelName = "shadowWarrior";

    private const string RosterPrefsKey = "CharacterRoster_v1";
    private const string LegacyLoadoutPrefsKey = "LocalLoadout_v1";

    public event Action<CharacterSaveData> OnActiveCharacterChanged;
    public event Action OnRosterChanged;

    private CharacterRosterSave _roster = new CharacterRosterSave();
    private static CharacterManager current;

    private void Awake()
    {
        if (current != null && current != this)
        {
            Destroy(gameObject);
            return;
        }

        current = this;
        DontDestroyOnLoad(gameObject);
        LoadRoster();
    }

    private void OnDestroy()
    {
        if (current == this)
            current = null;
    }

    public IReadOnlyList<CharacterSaveData> Characters => _roster.Characters;

    public string ActiveCharacterId => _roster.ActiveCharacterId;

    public bool HasActiveCharacter => GetActive() != null;

    public CharacterSaveData GetActive()
    {
        return FindById(_roster.ActiveCharacterId);
    }

    public CharacterSaveData FindById(string id)
    {
        if (string.IsNullOrEmpty(id) || _roster.Characters == null)
            return null;

        for (int i = 0; i < _roster.Characters.Count; i++)
        {
            var character = _roster.Characters[i];
            if (character != null && character.Id == id)
                return character;
        }

        return null;
    }

    public static string GetModelName(CharacterGender gender)
    {
        return gender == CharacterGender.Female ? FemaleModelName : MaleModelName;
    }

    public string GetActiveModelName()
    {
        var active = GetActive();
        return active != null ? GetModelName(active.Gender) : MaleModelName;
    }

    public bool CanCreateCharacter()
    {
        return _roster.Characters != null && _roster.Characters.Count < MaxCharacters;
    }

    public bool DeleteCharacter(string id)
    {
        var character = FindById(id);
        if (character == null)
            return false;

        bool wasActive = _roster.ActiveCharacterId == id;
        _roster.Characters.Remove(character);

        if (wasActive)
        {
            _roster.ActiveCharacterId = _roster.Characters.Count > 0
                ? _roster.Characters[0].Id
                : string.Empty;
            PushActiveLoadoutToManager(notify: true);
            OnActiveCharacterChanged?.Invoke(GetActive());
        }

        SaveRoster();
        OnRosterChanged?.Invoke();
        return true;
    }

    public CharacterSaveData CreateCharacter(string name, CharacterGender gender)
    {
        if (!CanCreateCharacter())
        {
            Debug.LogWarning("[CharacterManager] Character soft cap reached.");
            return null;
        }

        string sanitized = ApplicationSettings.SanitizeNickname(name);
        if (sanitized.Length < ApplicationSettings.MinNicknameLength)
        {
            Debug.LogWarning("[CharacterManager] Character name too short.");
            return null;
        }

        var character = CharacterSaveData.CreateNew(sanitized, gender);
        CharacterInventory.EnsureLists(character);
        TryGrantItemOn(character, ItemIds.StarterDagger, persist: false);
        _roster.Characters.Add(character);
        SaveRoster();
        OnRosterChanged?.Invoke();
        SelectActive(character.Id);
        return character;
    }

    public bool SelectActive(string id)
    {
        var character = FindById(id);
        if (character == null)
            return false;

        if (_roster.ActiveCharacterId == id)
        {
            PushActiveLoadoutToManager(notify: true);
            OnActiveCharacterChanged?.Invoke(character);
            return true;
        }

        _roster.ActiveCharacterId = id;
        SaveRoster();
        PushActiveLoadoutToManager(notify: true);
        OnRosterChanged?.Invoke();
        OnActiveCharacterChanged?.Invoke(character);
        return true;
    }

    public void UpdateActiveLoadout(LocalLoadout loadout)
    {
        var active = GetActive();
        if (active == null || loadout == null)
            return;

        // Character.Name is source of truth — do not overwrite from loadout.UnitName.
        active.ApplyLoadout(loadout);
        SaveRoster();
    }

    public void RenameActive(string name)
    {
        var active = GetActive();
        if (active == null)
            return;

        active.Name = ApplicationSettings.SanitizeNickname(name);
        SaveRoster();
        PushActiveLoadoutToManager(notify: true);
        OnRosterChanged?.Invoke();
        OnActiveCharacterChanged?.Invoke(active);
    }

    public void PushActiveLoadoutToManager(bool notify)
    {
        var loadoutManager = GameServices.Loadout;
        if (loadoutManager == null)
            return;

        var active = GetActive();
        if (active == null)
        {
            loadoutManager.ApplyFromCharacter(new LocalLoadout(), notify);
            return;
        }

        loadoutManager.ApplyFromCharacter(active.ToLoadout(), notify);
    }

    public bool TryGrantItem(string itemId)
    {
        return TryGrantItemOn(GetActive(), itemId, persist: true);
    }

    public bool TryDestroyEquipment(string instanceId)
    {
        var active = GetActive();
        CharacterInventory.EnsureLists(active);
        if (CharacterInventory.IsEquipped(active, instanceId))
            return false;
        if (!CharacterInventory.TryDestroyEquipment(active, instanceId))
            return false;

        SaveRoster();
        OnRosterChanged?.Invoke();
        return true;
    }

    public bool TryDestroyStack(string itemId, int amount = 1)
    {
        var active = GetActive();
        CharacterInventory.EnsureLists(active);
        if (!CharacterInventory.TryDestroyStack(active, itemId, amount))
            return false;

        SaveRoster();
        OnRosterChanged?.Invoke();
        return true;
    }

    public bool TryDestroyItem(string instanceId, string itemId)
    {
        if (!string.IsNullOrWhiteSpace(instanceId))
            return TryDestroyEquipment(instanceId);

        return TryDestroyStack(itemId);
    }

    public void PersistRoster()
    {
        SaveRoster();
        OnRosterChanged?.Invoke();
    }

    private bool TryGrantItemOn(CharacterSaveData character, string itemId, bool persist)
    {
        if (character == null || string.IsNullOrWhiteSpace(itemId))
            return false;

        var items = GameServices.Databases != null ? GameServices.Databases.Items : null;
        if (items == null || !items.TryGet(itemId, out var definition) || definition == null)
        {
            Debug.LogWarning($"[CharacterManager] Unknown item '{itemId}'.");
            return false;
        }

        CharacterInventory.EnsureLists(character);
        if (!CharacterInventory.TryGrant(character, definition))
            return false;

        if (persist)
        {
            SaveRoster();
            OnRosterChanged?.Invoke();
        }

        return true;
    }

    private void LoadRoster()
    {
        _roster = new CharacterRosterSave();

        if (PlayerPrefs.HasKey(RosterPrefsKey))
        {
            try
            {
                var json = PlayerPrefs.GetString(RosterPrefsKey, "{}");
                var loaded = JsonUtility.FromJson<CharacterRosterSave>(json);
                if (loaded != null)
                    _roster = loaded;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CharacterManager] Failed to load roster: {e.Message}");
                _roster = new CharacterRosterSave();
            }
        }

        if (_roster.Characters == null)
            _roster.Characters = new List<CharacterSaveData>();

        for (var i = 0; i < _roster.Characters.Count; i++)
            CharacterInventory.EnsureLists(_roster.Characters[i]);

        MigrateLegacyLoadoutIfNeeded();

        if (GetActive() == null && _roster.Characters.Count > 0)
            _roster.ActiveCharacterId = _roster.Characters[0].Id;
    }

    private void MigrateLegacyLoadoutIfNeeded()
    {
        if (_roster.Characters.Count > 0)
            return;

        if (!PlayerPrefs.HasKey(LegacyLoadoutPrefsKey))
            return;

        try
        {
            var json = PlayerPrefs.GetString(LegacyLoadoutPrefsKey, "{}");
            var legacy = JsonUtility.FromJson<LocalLoadout>(json) ?? new LocalLoadout();
            string name = ApplicationSettings.SanitizeNickname(legacy.UnitName);
            if (name.Length < ApplicationSettings.MinNicknameLength)
                name = "Hero";

            var character = CharacterSaveData.CreateNew(name, CharacterGender.Male, legacy);
            CharacterInventory.EnsureLists(character);
            _roster.Characters.Add(character);
            _roster.ActiveCharacterId = character.Id;
            SaveRoster();
            Debug.Log("[CharacterManager] Migrated LocalLoadout_v1 into first character.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CharacterManager] Legacy loadout migration failed: {e.Message}");
        }
    }

    private void SaveRoster()
    {
        try
        {
            var json = JsonUtility.ToJson(_roster);
            PlayerPrefs.SetString(RosterPrefsKey, json);
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CharacterManager] Failed to save roster: {e.Message}");
        }
    }
}
