using System;
using UnityEngine;

public class LoadoutManager : MonoBehaviour
{
    public static LoadoutManager Instance { get; private set; }

    public event Action<LocalLoadout> OnLoadoutChanged; // fires after save

    private const string PlayerPrefsKey = "LocalLoadout_v1";
    private LocalLoadout _current = new LocalLoadout();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadFromPrefs();
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

    public void SetSlotWeapon(string weaponId)
    {
        _current.WeaponId = weaponId;
        SaveAndNotify();
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
        SaveToPrefs();
        OnLoadoutChanged?.Invoke(_current);
    }

    private void LoadFromPrefs()
    {
        if (!PlayerPrefs.HasKey(PlayerPrefsKey))
        {
            _current = new LocalLoadout
            {
                UnitName = ApplicationSettings.Instance != null ? ApplicationSettings.Instance.Nickname : "Player",
                WeaponId = string.Empty,
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

            Debug.Log($"[LoadoutManager] Loaded prefs: {json}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LoadoutManager] Failed to load prefs: {e.Message}");
            _current = new LocalLoadout();
        }
    }

    private void SaveToPrefs()
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
}
