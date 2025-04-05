using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponDatabase", menuName = "Game/Weapon/Database")]
public class WeaponDatabase : ScriptableObject
{
    public List<WeaponData> allWeapons = new List<WeaponData>();

    public WeaponData GetWeaponByName(string name)
    {
        return allWeapons.Find(weapon => weapon.weaponName == name);
    }
}