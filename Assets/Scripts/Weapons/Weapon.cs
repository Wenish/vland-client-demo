using System.Threading.Tasks;
using Mirror;
using UnityEngine;

public abstract class Weapon : NetworkBehaviour
{
    // The weapon type (e.g. sword, daggers, bow, gun)
    public WeaponType weaponType;
    // The attack power of the weapon
    public int attackPower = 10;

    // The attack range of the weapon
    public float attackRange = 5.0f;

    // The time it takes to perform the attack
    public float attackTime = 0.2f;

    // The attack speed of the weapon
    public float attackSpeed = 1.0f;

    // The cooldown period (time between attacks)
    public float attackCooldown = 0.0f;

    // 0 - 1
    public float moveSpeedPercentWhileAttacking = 0.5f;

    public bool IsAttacking = false;

    // Time when the last attack occurred
    public double lastAttackTime = -Mathf.Infinity;

    public enum WeaponType
    {
        Sword,
        Daggers,
        Bow,
        Gun
    }

    // Called when the attack button is pressed
    [Server]
    public async Task Attack(UnitController attacker)
    {
        if (IsAttacking) return;

        // Check if enough time has passed since the last attack
        if (NetworkTime.time - lastAttackTime < attackCooldown) return;

        IsAttacking = true;
        attacker.RaiseOnAttackStartEvent();
        var attackerMoveSpeed = attacker.moveSpeed;
        attacker.moveSpeed = attacker.moveSpeed * moveSpeedPercentWhileAttacking;

        // Set the time of the last attack
        lastAttackTime = NetworkTime.time;

        // Perform the attack cooldown calculation
        attackCooldown = attackTime + attackSpeed;
        var delay = attackTime * 1000;
        await Task.Delay((int)delay);

        // Perform the attack
        PerformAttack(attacker);
        attacker.moveSpeed = attackerMoveSpeed;
    
        IsAttacking = false;
    }

    // Called every frame
    void Update()
    {
        if (!isServer) return;

        // Optionally, you could update the cooldown here too if you want visual feedback
    }

    // Perform the attack
    protected abstract void PerformAttack(UnitController attacker);
}