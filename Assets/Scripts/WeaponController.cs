using System.Threading.Tasks;
using Mirror;
using UnityEngine;

public class WeaponController : NetworkBehaviour
{
    public WeaponData weaponData;

    [SerializeField]
    private double lastAttackTime = -Mathf.Infinity;
    [SerializeField]
    private bool isAttacking;

    public bool IsAttackOnCooldown => NetworkTime.time - lastAttackTime < weaponData.AttackCooldown;

    [Server]
    public async Task Attack(UnitController attacker)
    {
        if (weaponData == null) {
            Debug.LogError("Weapon data is not assigned.");
            return;
        };
        
        if (isAttacking || IsAttackOnCooldown) return;

        isAttacking = true;
        attacker.RaiseOnAttackStartEvent();
        var originalSpeed = attacker.moveSpeed;
        attacker.moveSpeed = attacker.moveSpeed * weaponData.moveSpeedPercentWhileAttacking;

        lastAttackTime = NetworkTime.time;

        var delay = weaponData.attackTime * 1000;
        await Task.Delay((int)delay);

        weaponData.PerformAttack(attacker);
        attacker.moveSpeed = originalSpeed;
        isAttacking = false;
    }
}