using UnityEngine;

[CreateAssetMenu(fileName = "NewModel", menuName = "Game/Model/Model")]
public class ModelData : ScriptableObject
{
    public string modelName;
    public GameObject prefab;

    [Header("Default Animation Set")]
    public AnimationSetData defaultAnimationSet;

    [Header("Weapon-Specific Animation Sets")]
    public WeaponAnimationEntry[] weaponAnimationOverrides;

    public AnimationSetData GetAnimationSetForWeapon(WeaponType weaponType)
    {
        foreach (var entry in weaponAnimationOverrides)
        {
            if (entry.weaponType == weaponType)
                return entry.animationSet;
        }

        return defaultAnimationSet;
    }
}

[System.Serializable]
public struct WeaponAnimationEntry
{
    public WeaponType weaponType;
    public AnimationSetData animationSet;
}