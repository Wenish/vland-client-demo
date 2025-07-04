using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Audio/SoundDatabase")]
public class SoundDatabase : ScriptableObject
{
    public List<SoundData> sounds = new List<SoundData>();

    private Dictionary<string, SoundData> _lookup;

    private void OnEnable() => BuildLookup();

    private void BuildLookup()
    {
        _lookup = new Dictionary<string, SoundData>();
        foreach (var s in sounds)
        {
            if (s != null && !_lookup.ContainsKey(s.soundName))
                _lookup[s.soundName] = s;
        }
    }

    public SoundData GetSound(string name)
    {
        if (_lookup == null || _lookup.Count == 0)
            BuildLookup();

        if (!_lookup.TryGetValue(name, out var data))
        {
            Debug.LogWarning($"Sound '{name}' not found in SoundDatabase.");
        }
        return data;
    }
}
