using System.Collections.Generic;
using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "NewWeaponMelee", menuName = "Game/Weapon/Melee")]
public class WeaponMeleeData : WeaponData
{
    [Header("Meele Specific")]
    public Mode mode = Mode.Linear;
    public float coneAngleRadians = 90f;
    public int numRays = 21;
    public float weighting = 0.5f;

    public override void PerformAttack(UnitController attacker)
    {
        // TODO: Implement the PerformAttack method for melee weapons
        Debug.Log("Performing melee attack with " + weaponName);
    }

    public enum Mode { Linear, Quadratic }
}