using UnityEngine;

public class CastContext
{
    public UnitController caster;
    public NetworkedSkillInstance skillInstance;
    public Vector3? aimPoint;

    private bool _isCancelled = false;
    public bool IsCancelled => _isCancelled;
    public void Cancel()
    {
        _isCancelled = true;
    }
    public CastContext(UnitController caster, NetworkedSkillInstance skillInstance)
    {
        this.caster = caster;
        this.skillInstance = skillInstance;
    }
}
