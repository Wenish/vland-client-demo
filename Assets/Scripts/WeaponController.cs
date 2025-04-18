using System.Threading.Tasks;
using Mirror;
using UnityEngine;

public class WeaponController : NetworkBehaviour
{
    public WeaponData weaponData;

    [SerializeField, SyncVar]
    private double lastAttackTime = -Mathf.Infinity;
    [SerializeField]
    private bool isAttacking;

    public bool IsAttackOnCooldown => NetworkTime.time - lastAttackTime < weaponData.AttackCooldown;

    public float AttackCooldownRemaining { get; private set; } = 0;
    public float AttackCooldownProgress { get; private set; } = 0;

    void Update()
    {
        AttackCooldownRemaining = weaponData.AttackCooldown - (float)(NetworkTime.time - lastAttackTime);
        AttackCooldownProgress = (AttackCooldownRemaining / weaponData.AttackCooldown) * 100f;
    }

    [Server]
    public async Task Attack(UnitController attacker)
    {
        if (weaponData == null)
        {
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

        if (attacker == null) return;

        if (attacker.IsDead)
        {
            attacker.moveSpeed = originalSpeed;
            isAttacking = false;
            return;
        }

        weaponData.PerformAttack(attacker);
        attacker.moveSpeed = originalSpeed;
        isAttacking = false;
    }
}