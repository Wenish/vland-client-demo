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

    public float AttackCooldownRemaining => weaponData.AttackCooldown - (float)(NetworkTime.time - lastAttackTime);
    public float AttackCooldownProgress => (AttackCooldownRemaining / weaponData.AttackCooldown) * 100f;

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

        lastAttackTime = NetworkTime.time;

        var delay = weaponData.attackTime * 1000;
        StatModifier moveSpeedModifier = new StatModifier() {
            Type = StatType.MovementSpeed,
            ModifierType = ModifierType.Percent,
            Value = weaponData.moveSpeedPercentWhileAttacking - 1,
        };
        attacker.unitMediator.Stats.ApplyModifier(moveSpeedModifier);
        await Task.Delay((int)delay);

        if (attacker == null) return;
        attacker.unitMediator.Stats.RemoveModifier(moveSpeedModifier);
        if (attacker.IsDead)
        {
            isAttacking = false;
            return;
        }

        weaponData.PerformAttack(attacker);
        isAttacking = false;
    }
}