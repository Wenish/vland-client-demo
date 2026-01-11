using System.Threading;
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

    public bool IsAttackOnCooldown => NetworkTime.time - lastAttackTime < AttackCooldown;
    public float AttackCooldownRemaining => Mathf.Max(0f, AttackCooldown - (float)(NetworkTime.time - lastAttackTime));
    public float AttackCooldownProgress => (AttackCooldownRemaining / AttackCooldown) * 100f;
    // Higher attackSpeedMultiplier should result in faster (shorter) cooldowns
    public float AttackCooldown => (weaponData.attackTime + weaponData.attackSpeed) / Mathf.Max(attackSpeedMultiplier, 0.01f);

    private int attackIndex = 0;

    private float attackSpeedMultiplier => attackerMediator.Stats.GetStat(StatType.AttackSpeed);

    private UnitMediator attackerMediator;
    private CancellationTokenSource attackCancellationTokenSource;

    private void Awake()
    {
        attackerMediator = GetComponent<UnitMediator>();
    }

    [Server]
    public async Task Attack(UnitController attacker)
    {
        if (weaponData == null)
        {
            Debug.LogError("Weapon data is not assigned.");
            return;
        }
        ;

        if (isAttacking || IsAttackOnCooldown || attacker.unitActionState.IsActive) return;

        // Create a new cancellation token for this attack
        attackCancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = attackCancellationTokenSource.Token;

        isAttacking = true;
        attacker.RaiseOnAttackStartEvent(attackIndex);

        lastAttackTime = NetworkTime.time;

        // Scale attack animation/duration by attack speed (higher speed -> shorter duration)
        var attackDuration = weaponData.attackTime / Mathf.Max(attackSpeedMultiplier, 0.01f);
        var delay = attackDuration * 1000;
        attacker.unitActionState.SetUnitActionState(UnitActionState.ActionType.Attacking, NetworkTime.time, attackDuration, weaponData.weaponName);
        StatModifier moveSpeedModifier = new StatModifier()
        {
            Type = StatType.MovementSpeed,
            ModifierType = ModifierType.Percent,
            Value = weaponData.moveSpeedPercentWhileAttacking,
        };
        attacker.unitMediator.Stats.ApplyModifier(moveSpeedModifier);
        
        try
        {
            await Task.Delay((int)delay, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // Attack was interrupted
            attacker.unitActionState.SetUnitActionStateToIdle();
            attacker.unitMediator.Stats.RemoveModifier(moveSpeedModifier);
            isAttacking = false;
            return;
        }

        if (attacker == null) return;
        attacker.unitActionState.SetUnitActionStateToIdle();
        attacker.unitMediator.Stats.RemoveModifier(moveSpeedModifier);
        if (attacker.IsDead)
        {
            isAttacking = false;
            return;
        }

        weaponData.PerformAttack(attacker);
        attacker.RaiseOnAttackSwingEvent(attackIndex);
        isAttacking = false;
        attackIndex = (attackIndex + 1) % 2;
    }

    /// <summary>
    /// Cancels the current attack if one is in progress.
    /// </summary>
    public void CancelAttack()
    {
        if (attackCancellationTokenSource != null && !attackCancellationTokenSource.Token.IsCancellationRequested)
        {
            attackCancellationTokenSource.Cancel();
            attackCancellationTokenSource.Dispose();
            attackCancellationTokenSource = null;
        }
        isAttacking = false;
    }
}