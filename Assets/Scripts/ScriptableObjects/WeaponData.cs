using UnityEngine;

public abstract class WeaponData : ScriptableObject
{
    public string weaponName;
    public WeaponType weaponType;
    
    [Header("Stats")]
    public int attackPower = 10;
    public float attackRange = 5.0f;
    public float attackTime = 0.2f;
    public float attackSpeed = 1.0f;
    public float moveSpeedPercentWhileAttacking = 0.5f;

    public float AttackCooldown => attackTime + attackSpeed;

    public abstract void PerformAttack(UnitController attacker);
}


public enum WeaponType
{
    Unarmed,
    Sword,
    Daggers,
    Bow,
    Gun
}