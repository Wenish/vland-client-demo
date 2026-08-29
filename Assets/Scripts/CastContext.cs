using UnityEngine;
using ShadowInfection.DI;

public class CastContext
{
    public UnitController caster;
    public NetworkedSkillInstance skillInstance;
    public Vector3? aimPoint;
    public Quaternion? aimRotation;

    /// <summary>
    /// When true (client Left Alt, replicated via skill command):
    /// unit targeting prefers the caster if the effect's team mask allows Self,
    /// and mouse-placed skills (<c>AtAimPoint</c> / <c>spawnAtAimPoint</c>) resolve at the caster.
    /// </summary>
    public bool forceSelfTarget;

    /// <summary>
    /// Set by the active Cast/Channel mechanic: live mouse aim updates CastContext while true.
    /// </summary>
    public bool updatesAimDuringCast;
    public IUnitSpawner UnitSpawner { get; }
    public IProjectileSpawner ProjectileSpawner { get; }
    public IAreaZoneSpawner AreaZoneSpawner { get; }

    /// <summary>
    /// The unit that triggered this cast (e.g. the attacker when reflecting damage).
    /// May be null for manually-cast skills.
    /// </summary>
    public UnitController instigator;

    /// <summary>
    /// The raw damage value from the triggering hit. Populated by damage-event triggers.
    /// Used by mechanics such as <c>SkillEffectMechanicReflectDamage</c> in PercentOfIncoming mode.
    /// </summary>
    public int? incomingDamage;

    private bool _isCancelled = false;
    public bool IsCancelled => _isCancelled;
    public void Cancel()
    {
        _isCancelled = true;
    }

    private bool _pendingTrigger = false;
    public bool HasPendingTrigger => _pendingTrigger;
    public void SignalTrigger()
    {
        _pendingTrigger = true;
    }
    public bool ConsumePendingTrigger()
    {
        if (!_pendingTrigger) return false;
        _pendingTrigger = false;
        return true;
    }

    /// <summary>
    /// Instantly faces the caster toward the cast aim (used before turn-speed locks).
    /// </summary>
    public void SnapCasterFacingToAim()
    {
        if (caster == null)
            return;

        var indicator = skillInstance != null ? SkillAimPreviewUtil.Resolve(skillInstance) : null;
        if (!SkillAimUtil.ShouldSnapFacingToCastAim(
            caster,
            new Vector2(caster.horizontalInput, caster.verticalInput),
            indicator))
            return;

        if (aimPoint.HasValue)
        {
            caster.SnapFacingToAimPoint(aimPoint.Value);
            return;
        }

        if (aimRotation.HasValue)
            caster.SnapFacingToAimRotation(aimRotation.Value);
    }

    private bool _castCounted = false;
    // Call when an effect marked as "counts as casted" executes. Only counts once per cast.
    public void MarkCastCounted()
    {
        if (_castCounted) return;
        _castCounted = true;
        // only the server should set cooldown
        if (skillInstance != null)
        {
            // Guard if not on server, do nothing
            if (skillInstance.isServer)
            {
                skillInstance.OnCastCounted();
            }
        }
    }
    public CastContext(UnitController caster, NetworkedSkillInstance skillInstance)
    {
        this.caster = caster;
        this.skillInstance = skillInstance;
        UnitSpawner = GameServices.Units;
        ProjectileSpawner = GameServices.Projectiles;
        AreaZoneSpawner = GameServices.AreaZones;
    }
}
