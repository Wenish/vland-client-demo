using UnityEngine;

public class CastContext
{
    public UnitController caster;
    public NetworkedSkillInstance skillInstance;
    public Vector3? aimPoint;
    public Quaternion? aimRotation;

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
    }
}
