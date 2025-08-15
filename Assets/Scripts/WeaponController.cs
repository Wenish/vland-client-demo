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

        if (isAttacking || IsAttackOnCooldown || attacker.unitActionState.IsActive) return;

        isAttacking = true;
        attacker.RaiseOnAttackStartEvent();

        lastAttackTime = NetworkTime.time;

        var delay = weaponData.attackTime * 1000;
        attacker.unitActionState.SetUnitActionState(UnitActionState.ActionType.Attacking, NetworkTime.time, weaponData.attackTime, weaponData.weaponName);
        StatModifier moveSpeedModifier = new StatModifier() {
            Type = StatType.MovementSpeed,
            ModifierType = ModifierType.Percent,
            Value = weaponData.moveSpeedPercentWhileAttacking,
        };
        attacker.unitMediator.Stats.ApplyModifier(moveSpeedModifier);
        await Task.Delay((int)delay);

        if (attacker == null) return;
        attacker.unitActionState.SetUnitActionStateToIdle();
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