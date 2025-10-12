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

    [Header("UI")]
    public Texture2D iconTexture;

    [Header("Visuals")]
    public GameObject weaponModelRightHand;
    public GameObject weaponModelLeftHand;

    [Header("Visuals - Swing VFX")]
    [Tooltip("Optional slash/swing VFX prefab to spawn on successful hits.")]
    public GameObject swingVfxPrefab;
    [Tooltip("Local position offset relative to the spawn anchor (usually right hand).")]
    public Vector3 swingVfxPositionOffset = Vector3.zero;
    [Tooltip("Local rotation offset (Euler) relative to the spawn anchor.")]
    public Vector3 swingVfxRotationOffset = Vector3.zero;
    [Tooltip("If > 0, the spawned VFX will be destroyed after this lifetime in seconds. If 0, relies on self-destruction in the prefab.")]
    public float swingVfxLifetime = 0.3f;

    public float AttackCooldown => attackTime + attackSpeed;

    public abstract void PerformAttack(UnitController attacker);
}


public enum WeaponType : byte
{
    Unarmed,
    Sword,
    Daggers,
    Bow,
    Gun,
    Pistols
}