using System;
using System.Threading;
using System.Threading.Tasks;
using Mirror;
using ShadowInfection.DI;
using ShadowInfection.Items;
using UnityEngine;

public class WeaponController : NetworkBehaviour
{
    public WeaponData weaponData;
    public WeaponData offHandWeaponData;

    [SerializeField, SyncVar]
    private double lastMainAttackTime = double.NegativeInfinity;
    [SerializeField, SyncVar]
    private double lastOffHandAttackTime = double.NegativeInfinity;
    [SerializeField]
    private bool isAttacking;

    public bool HasOffHandAttack =>
        offHandWeaponData != null && ItemRules.IsDualWieldWeapon(offHandWeaponData.weaponType);

    public bool CanStartMainAttack =>
        weaponData != null && !IsHandOnCooldown(lastMainAttackTime, weaponData);

    public bool CanStartOffHandAttack =>
        HasOffHandAttack && !IsHandOnCooldown(lastOffHandAttackTime, offHandWeaponData);

    public bool IsAttackOnCooldown => !CanStartMainAttack && !CanStartOffHandAttack;

    public float AttackCooldownRemaining
    {
        get
        {
            if (CanStartMainAttack || CanStartOffHandAttack)
                return 0f;

            var mainRemaining = HandCooldownRemaining(lastMainAttackTime, weaponData);
            if (!HasOffHandAttack)
                return mainRemaining;

            return Mathf.Min(
                mainRemaining,
                HandCooldownRemaining(lastOffHandAttackTime, offHandWeaponData));
        }
    }

    public float AttackCooldown
    {
        get
        {
            if (CanStartMainAttack)
                return HandCooldown(weaponData);
            if (CanStartOffHandAttack)
                return HandCooldown(offHandWeaponData);

            var mainCooldown = HandCooldown(weaponData);
            if (!HasOffHandAttack)
                return mainCooldown;

            var offCooldown = HandCooldown(offHandWeaponData);
            var mainRemaining = HandCooldownRemaining(lastMainAttackTime, weaponData);
            var offRemaining = HandCooldownRemaining(lastOffHandAttackTime, offHandWeaponData);
            return offRemaining < mainRemaining ? offCooldown : mainCooldown;
        }
    }

    public float AttackCooldownProgress
    {
        get
        {
            var cooldown = AttackCooldown;
            if (cooldown <= 0.0001f)
                return 0f;
            return (AttackCooldownRemaining / cooldown) * 100f;
        }
    }

    private float attackSpeedMultiplier => attackerMediator.Stats.GetStat(StatType.AttackSpeed);

    private UnitMediator attackerMediator;
    private CancellationTokenSource attackCancellationTokenSource;

    private void Awake()
    {
        attackerMediator = GetComponent<UnitMediator>();
    }

    public void SetHeldWeapons(WeaponData main, WeaponData offHand)
    {
        weaponData = main;
        offHandWeaponData = offHand;
    }

    private bool TryChooseAttackingHand(out bool useOffHand)
    {
        useOffHand = false;
        var mainReady = CanStartMainAttack;
        var offReady = CanStartOffHandAttack;
        if (!mainReady && !offReady)
            return false;

        if (mainReady && offReady)
        {
            useOffHand = lastOffHandAttackTime < lastMainAttackTime;
            return true;
        }

        useOffHand = offReady;
        return true;
    }

    [Server]
    public async Task Attack(UnitController attacker)
    {
        if (isAttacking || attacker == null || attacker.unitActionState.IsActive)
            return;

        if (!TryChooseAttackingHand(out var useOffHand))
            return;

        var swinging = useOffHand ? offHandWeaponData : weaponData;
        if (swinging == null)
            return;

        attackCancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = attackCancellationTokenSource.Token;

        isAttacking = true;
        var attackIndex = useOffHand ? 1 : 0;
        attacker.RaiseOnAttackStartEvent(attackIndex);

        var now = NetworkTime.time;
        if (useOffHand)
            lastOffHandAttackTime = now;
        else
            lastMainAttackTime = now;

        var attackDuration = swinging.attackTime / Mathf.Max(attackSpeedMultiplier, 0.01f);
        var delay = attackDuration * 1000;
        attacker.unitActionState.SetUnitActionState(
            UnitActionState.ActionType.Attacking,
            NetworkTime.time,
            attackDuration,
            swinging.weaponName);

        StatModifier moveSpeedModifier = new StatModifier()
        {
            Type = StatType.MovementSpeed,
            ModifierType = ModifierType.Percent,
            Value = swinging.moveSpeedPercentWhileAttacking,
        };
        attacker.unitMediator.Stats.ApplyModifier(moveSpeedModifier);

        try
        {
            await Task.Delay((int)delay, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            FinishCancelledAttack(attacker, moveSpeedModifier);
            return;
        }
        finally
        {
            DisposeAttackCancellation();
        }

        if (attacker == null)
            return;

        ClearAttackingStateIfOwned(attacker);
        attacker.unitMediator.Stats.RemoveModifier(moveSpeedModifier);
        if (attacker.IsDead)
        {
            isAttacking = false;
            return;
        }

        var damageMultiplier = useOffHand ? ItemRules.OffHandDamageMultiplier : 1f;
        if (swinging is WeaponRangedData ranged)
            ranged.PerformAttack(attacker, GameServices.Projectiles, damageMultiplier);
        else
            swinging.PerformAttack(attacker, damageMultiplier);

        attacker.RaiseOnAttackSwingEvent(attackIndex);
        isAttacking = false;
    }

    public void CancelAttack()
    {
        if (attackCancellationTokenSource != null && !attackCancellationTokenSource.Token.IsCancellationRequested)
        {
            attackCancellationTokenSource.Cancel();
        }
        isAttacking = false;
    }

    private float HandCooldown(WeaponData weapon)
    {
        if (weapon == null)
            return 0.01f;
        return (weapon.attackTime + weapon.attackSpeed) / Mathf.Max(attackSpeedMultiplier, 0.01f);
    }

    private bool IsHandOnCooldown(double lastAttackTime, WeaponData weapon)
    {
        return NetworkTime.time - lastAttackTime < HandCooldown(weapon);
    }

    private float HandCooldownRemaining(double lastAttackTime, WeaponData weapon)
    {
        return Mathf.Max(0f, HandCooldown(weapon) - (float)(NetworkTime.time - lastAttackTime));
    }

    private void FinishCancelledAttack(UnitController attacker, StatModifier moveSpeedModifier)
    {
        ClearAttackingStateIfOwned(attacker);
        if (attacker != null && attacker.unitMediator != null)
        {
            attacker.unitMediator.Stats.RemoveModifier(moveSpeedModifier);
        }
        isAttacking = false;
    }

    private static void ClearAttackingStateIfOwned(UnitController attacker)
    {
        if (attacker == null || attacker.unitActionState == null)
            return;
        if (attacker.unitActionState.state.type != UnitActionState.ActionType.Attacking)
            return;
        attacker.unitActionState.SetUnitActionStateToIdle();
    }

    private void DisposeAttackCancellation()
    {
        if (attackCancellationTokenSource == null)
            return;
        attackCancellationTokenSource.Dispose();
        attackCancellationTokenSource = null;
    }
}
