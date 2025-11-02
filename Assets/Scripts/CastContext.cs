using UnityEngine;

public class CastContext
{
    public UnitController caster;
    public NetworkedSkillInstance skillInstance;
    public Vector3? aimPoint;
    public Vector3? aimDirection;

    private bool _isCancelled = false;
    public bool IsCancelled => _isCancelled;
    public void Cancel()
    {
        _isCancelled = true;
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
