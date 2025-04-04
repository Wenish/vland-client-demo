using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponRanged", menuName = "Game/Weapon/Ranged")]
public class WeaponRangedData : WeaponData
{
    [Header("Ranged Specific")]
    public float projectileSpeed = 10.0f;
    public float spawnDistance = 1f;

    public override void PerformAttack(UnitController attacker)
    {
        // TODO: Implement the PerformAttack method for ranged weapons
        Debug.Log("Performing ranged attack with " + weaponName);
    }
}