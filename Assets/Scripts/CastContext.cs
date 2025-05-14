public class CastContext
{
    public UnitController caster;
    public NetworkedSkillInstance skillInstance;

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
