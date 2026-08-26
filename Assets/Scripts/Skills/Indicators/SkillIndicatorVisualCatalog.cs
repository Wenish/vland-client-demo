using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Client-side lookup of SkillIndicatorData by asset name so TargetRpc payloads
/// can resolve textures/materials without sending UnityEngine.Object references.
/// </summary>
public static class SkillIndicatorVisualCatalog
{
    private static Dictionary<string, SkillIndicatorData> _byName;
    private static bool _loaded;

    public static SkillIndicatorData Get(string assetName)
    {
        if (string.IsNullOrEmpty(assetName))
            return null;

        EnsureLoaded();
        return _byName.TryGetValue(assetName, out var data) ? data : null;
    }

    public static void Register(SkillIndicatorData data)
    {
        if (data == null || string.IsNullOrEmpty(data.name))
            return;

        EnsureLoaded();
        _byName[data.name] = data;
    }

    private static void EnsureLoaded()
    {
        if (_loaded)
            return;

        _loaded = true;
        _byName = new Dictionary<string, SkillIndicatorData>();

        var loaded = Resources.LoadAll<SkillIndicatorData>(string.Empty);
        for (int i = 0; i < loaded.Length; i++)
        {
            var data = loaded[i];
            if (data == null || string.IsNullOrEmpty(data.name))
                continue;

            _byName[data.name] = data;
        }
    }
}
