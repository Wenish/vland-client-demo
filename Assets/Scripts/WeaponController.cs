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
    [SerializeField, SyncVar]
    private int mainMagazineRemaining;
    [SerializeField, SyncVar]
    private int offMagazineRemaining;
    [SerializeField, SyncVar]
    private double mainReloadEndTime;
    [SerializeField, SyncVar]
    private double offReloadEndTime;
    [SerializeField]
    private bool isAttacking;

    public bool HasOffHandAttack =>
        offHandWeaponData != null && ItemRules.CanAttackWithOffHand(offHandWeaponData.weaponType);

    public bool CanStartMainAttack =>
        CanStartHand(weaponData, lastMainAttackTime, mainMagazineRemaining, mainReloadEndTime);

    public bool CanStartOffHandAttack =>
        HasOffHandAttack
        && CanStartHand(offHandWeaponData, lastOffHandAttackTime, offMagazineRemaining, offReloadEndTime);

    public bool IsAttackOnCooldown => !CanStartMainAttack && !CanStartOffHandAttack;

    public bool HasMagazineAmmo =>
        weaponData is WeaponMagazineRangedData
        || (HasOffHandAttack && offHandWeaponData is WeaponMagazineRangedData);

    public int MagazineAmmoRemaining
    {
        get
        {
            var total = 0;
            if (weaponData is WeaponMagazineRangedData)
                total += mainMagazineRemaining;
            if (HasOffHandAttack && offHandWeaponData is WeaponMagazineRangedData)
                total += offMagazineRemaining;
            return total;
        }
    }

    public float AttackCooldownRemaining
    {
        get
        {
            if (CanStartMainAttack || CanStartOffHandAttack)
                return 0f;

            var mainRemaining = EffectiveCooldownRemaining(weaponData, lastMainAttackTime, mainReloadEndTime);
            if (!HasOffHandAttack)
                return mainRemaining;

            return Mathf.Min(
                mainRemaining,
                EffectiveCooldownRemaining(offHandWeaponData, lastOffHandAttackTime, offReloadEndTime));
        }
    }

    public float AttackCooldown
    {
        get
        {
            if (CanStartMainAttack)
                return EffectiveCooldownDuration(weaponData, mainReloadEndTime);
            if (CanStartOffHandAttack)
                return EffectiveCooldownDuration(offHandWeaponData, offReloadEndTime);

            var mainCooldown = EffectiveCooldownDuration(weaponData, mainReloadEndTime);
            if (!HasOffHandAttack)
                return mainCooldown;

            var offCooldown = EffectiveCooldownDuration(offHandWeaponData, offReloadEndTime);
            var mainRemaining = EffectiveCooldownRemaining(weaponData, lastMainAttackTime, mainReloadEndTime);
            var offRemaining = EffectiveCooldownRemaining(offHandWeaponData, lastOffHandAttackTime, offReloadEndTime);
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

    private void Update()
    {
        if (!isServer)
            return;
        TickMagazineHands();
    }

    public void SetHeldWeapons(WeaponData main, WeaponData offHand)
    {
        var mainChanged = !ReferenceEquals(weaponData, main);
        var offChanged = !ReferenceEquals(offHandWeaponData, offHand);
        weaponData = main;
        offHandWeaponData = offHand;
        if (!isServer)
            return;
        if (mainChanged)
            ResetMagazineHand(false);
        if (offChanged)
            ResetMagazineHand(true);
    }

    private bool TryChooseAttackingHand(out bool useOffHand)
    {
        useOffHand = false;
        var mainReady = CanStartMainAttack;
        var offReady = CanStartOffHandAttack;
        if (!mainReady && !offReady)
            return false;

        useOffHand = offReady && !mainReady;
        return true;
    }

    [Server]
    public async Task Attack(UnitController attacker)
    {
        if (isAttacking || attacker == null || attacker.unitActionState.IsActive)
            return;

        TickMagazineHands();

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

        var damageMultiplier = useOffHand
            ? ItemRules.GetOffHandDamageMultiplier(swinging.weaponType)
            : 1f;
        if (swinging is WeaponRangedData ranged)
            ranged.PerformAttack(attacker, GameServices.Projectiles, damageMultiplier);
        else
            swinging.PerformAttack(attacker, damageMultiplier);

        ConsumeMagazineShot(useOffHand);
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

    private bool CanStartHand(
        WeaponData weapon,
        double lastAttackTime,
        int ammo,
        double reloadEndTime)
    {
        if (weapon == null)
            return false;
        if (weapon is WeaponMagazineRangedData)
        {
            if (IsMagazineReloading(reloadEndTime) || ammo <= 0)
                return false;
        }

        return !IsHandOnCooldown(lastAttackTime, weapon);
    }

    private float EffectiveCooldownRemaining(WeaponData weapon, double lastAttackTime, double reloadEndTime)
    {
        if (weapon is WeaponMagazineRangedData && IsMagazineReloading(reloadEndTime))
            return Mathf.Max(0f, (float)(reloadEndTime - NetworkTime.time));
        return HandCooldownRemaining(lastAttackTime, weapon);
    }

    private float EffectiveCooldownDuration(WeaponData weapon, double reloadEndTime)
    {
        if (weapon is WeaponMagazineRangedData magazine && IsMagazineReloading(reloadEndTime))
            return Mathf.Max(0.01f, magazine.reloadTime);
        return HandCooldown(weapon);
    }

    private static bool IsMagazineReloading(double reloadEndTime)
    {
        return reloadEndTime > 0d && NetworkTime.time < reloadEndTime;
    }

    [Server]
    private void TickMagazineHands()
    {
        TickMagazineHand(false);
        TickMagazineHand(true);
    }

    [Server]
    private void TickMagazineHand(bool offHand)
    {
        var weapon = offHand
            ? (HasOffHandAttack ? offHandWeaponData : null)
            : weaponData;
        var remaining = offHand ? offMagazineRemaining : mainMagazineRemaining;
        var reloadEndTime = offHand ? offReloadEndTime : mainReloadEndTime;
        var lastAttackTime = offHand ? lastOffHandAttackTime : lastMainAttackTime;

        if (weapon is not WeaponMagazineRangedData magazine)
        {
            SetMagazineState(offHand, 0, 0d);
            return;
        }

        var size = Mathf.Max(0, magazine.magazineSize);
        if (size <= 0)
        {
            SetMagazineState(offHand, 0, 0d);
            return;
        }

        var now = NetworkTime.time;

        if (IsMagazineReloading(reloadEndTime))
            return;

        if (reloadEndTime > 0d && now >= reloadEndTime)
        {
            SetMagazineState(offHand, size, 0d);
            return;
        }

        if (remaining <= 0)
        {
            SetMagazineState(offHand, remaining, now + Mathf.Max(0f, magazine.reloadTime));
            return;
        }

        if (isAttacking
            || remaining >= size
            || magazine.idleReloadDelay <= 0f
            || now - lastAttackTime < magazine.idleReloadDelay)
        {
            return;
        }

        SetMagazineState(offHand, remaining, now + Mathf.Max(0f, magazine.reloadTime));
    }

    [Server]
    private void ConsumeMagazineShot(bool useOffHand)
    {
        var weapon = useOffHand ? offHandWeaponData : weaponData;
        if (weapon is not WeaponMagazineRangedData magazine)
            return;

        var remaining = Mathf.Max(0, (useOffHand ? offMagazineRemaining : mainMagazineRemaining) - 1);
        var reloadEndTime = useOffHand ? offReloadEndTime : mainReloadEndTime;
        if (remaining <= 0)
            reloadEndTime = NetworkTime.time + Mathf.Max(0f, magazine.reloadTime);
        SetMagazineState(useOffHand, remaining, reloadEndTime);
    }

    [Server]
    private void ResetMagazineHand(bool offHand)
    {
        var weapon = offHand ? offHandWeaponData : weaponData;
        var remaining = 0;
        if (weapon is WeaponMagazineRangedData magazine)
            remaining = Mathf.Max(0, magazine.magazineSize);

        SetMagazineState(offHand, remaining, 0d);
        if (offHand)
            lastOffHandAttackTime = double.NegativeInfinity;
        else
            lastMainAttackTime = double.NegativeInfinity;
    }

    [Server]
    private void SetMagazineState(bool offHand, int remaining, double reloadEndTime)
    {
        if (offHand)
        {
            if (offMagazineRemaining != remaining)
                offMagazineRemaining = remaining;
            if (offReloadEndTime != reloadEndTime)
                offReloadEndTime = reloadEndTime;
            return;
        }

        if (mainMagazineRemaining != remaining)
            mainMagazineRemaining = remaining;
        if (mainReloadEndTime != reloadEndTime)
            mainReloadEndTime = reloadEndTime;
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
