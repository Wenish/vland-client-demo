using System.Threading.Tasks;
using UnityEngine;

public abstract class Weapon : MonoBehaviour
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

    // The time until the next attack can be performed
    public float attackCooldown = 0.0f;

    // 0 - 1
    public float moveSpeedPercentWhileAttacking = 0.5f;

    

    public enum WeaponType
    {
        Sword,
        Daggers,
        Bow,
        Gun
    }

    // Called when the attack button is pressed
    public async void Attack(UnitController attacker)
    {
        // Check if the attack is on cooldown
        if (attackCooldown > 0.0f)
        {
            return;
        }
        attacker.RaiseOnAttackStartEvent();
        var attackerMoveSpeed = attacker.moveSpeed;
        attacker.moveSpeed = attacker.moveSpeed * moveSpeedPercentWhileAttacking;

        // Set the attack cooldown
        attackCooldown = attackTime + attackSpeed;
        Debug.Log("Attack Weapon");
        var delay = attackTime * 1000;
        Debug.Log(delay);
        await Task.Delay((int)delay);

        // Perform the attack
        PerformAttack(attacker);
        attacker.moveSpeed = attackerMoveSpeed;
    }

    // Called every frame
    void Update()
    {
        // Reduce the attack cooldown
        attackCooldown = Mathf.Max(0.0f, attackCooldown - Time.deltaTime);
    }

    // Perform the attack
    protected abstract void PerformAttack(UnitController attacker);
}