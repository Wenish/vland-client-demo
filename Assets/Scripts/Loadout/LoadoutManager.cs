using System;
using ShadowInfection.DI;
using UnityEngine;

public class LoadoutManager : MonoBehaviour
{
    public event Action<LocalLoadout> OnLoadoutChanged; // fires after save

    private const string PlayerPrefsKey = "LocalLoadout_v1";
    private LocalLoadout _current = new LocalLoadout();
    private static LoadoutManager current;
    private bool _suppressCharacterWrite;

    private void Awake()
    {
        if (current != null && current != this)
        {
            Destroy(gameObject);
            return;
        }

        current = this;
        DontDestroyOnLoad(gameObject);
        LoadInitial();
    }

    private void Start()
    {
        // CharacterManager may register after Awake; re-sync once.
        var characters = GameServices.Characters;
        if (characters != null && characters.HasActiveCharacter)
            ApplyFromCharacter(characters.GetActive().ToLoadout(), notify: false);
    }

    private void OnDestroy()
    {
        if (current == this)
            current = null;
    }

    public LocalLoadout Get()
    {
        return _current;
    }

    public void Set(LocalLoadout loadout)
    {
        _current = loadout ?? new LocalLoadout();
        SaveAndNotify();
    }

    /// <summary>
    /// Replace in-memory loadout from the active character without writing back through CharacterManager first.
    /// </summary>
    public void ApplyFromCharacter(LocalLoadout loadout, bool notify)
    {
        _suppressCharacterWrite = true;
        _current = CloneLoadout(loadout);
        _suppressCharacterWrite = false;

        if (notify)
            OnLoadoutChanged?.Invoke(_current);
    }

    public void SetSlotPassive(string passiveId)
    {
        _current.PassiveId = passiveId;
        SaveAndNotify();
    }

    public void SetSlotNormal(int index, string skillId)
    {
        index = Mathf.Clamp(index, 0, 2);
        switch (index)
        {
            case 0: _current.Normal1Id = skillId; break;
            case 1: _current.Normal2Id = skillId; break;
            case 2: _current.Normal3Id = skillId; break;
        }
        SaveAndNotify();
    }

    public void SetSlotUltimate(string ultId)
    {
        _current.UltimateId = ultId;
        SaveAndNotify();
    }

    public void SetUnitName(string unitName)
    {
        _current.UnitName = unitName;
        SaveAndNotify();
    }

    private void SaveAndNotify()
    {
        Persist();
        OnLoadoutChanged?.Invoke(_current);
    }

    private void Persist()
    {
        if (_suppressCharacterWrite)
            return;

        var characters = GameServices.Characters;
        if (characters != null && characters.HasActiveCharacter)
        {
            characters.UpdateActiveLoadout(_current);
            return;
        }

        SaveLegacyPrefs();
    }

    private void LoadInitial()
    {
        var characters = GetComponent<CharacterManager>()
            ?? FindAnyObjectByType<CharacterManager>(FindObjectsInactive.Include);

        if (characters != null && characters.HasActiveCharacter)
        {
            _current = CloneLoadout(characters.GetActive().ToLoadout());
            return;
        }

        LoadLegacyPrefs();
    }

    private void LoadLegacyPrefs()
    {
        if (!PlayerPrefs.HasKey(PlayerPrefsKey))
        {
            _current = new LocalLoadout
            {
                UnitName = string.Empty,
                PassiveId = string.Empty,
                Normal1Id = string.Empty,
                Normal2Id = string.Empty,
                Normal3Id = string.Empty,
                UltimateId = string.Empty
            };
            return;
        }

        try
        {
            var json = PlayerPrefs.GetString(PlayerPrefsKey, "{}");
            _current = JsonUtility.FromJson<LocalLoadout>(json);
            if (_current == null) _current = new LocalLoadout();
            _current.UnitName = ApplicationSettings.SanitizeNickname(_current.UnitName);

            Debug.Log($"[LoadoutManager] Loaded prefs: {json}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LoadoutManager] Failed to load prefs: {e.Message}");
            _current = new LocalLoadout();
        }
    }

    private void SaveLegacyPrefs()
    {
        try
        {
            var json = JsonUtility.ToJson(_current);
            PlayerPrefs.SetString(PlayerPrefsKey, json);
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LoadoutManager] Failed to save prefs: {e.Message}");
        }
    }

    private static LocalLoadout CloneLoadout(LocalLoadout source)
    {
        if (source == null)
            return new LocalLoadout();

        return new LocalLoadout
        {
            UnitName = source.UnitName ?? string.Empty,
            PassiveId = source.PassiveId ?? string.Empty,
            Normal1Id = source.Normal1Id ?? string.Empty,
            Normal2Id = source.Normal2Id ?? string.Empty,
            Normal3Id = source.Normal3Id ?? string.Empty,
            UltimateId = source.UltimateId ?? string.Empty
        };
    }
}
