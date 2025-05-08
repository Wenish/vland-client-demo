public class CastContext
{
    public UnitController caster;
    public NetworkedSkillInstance skillInstance;
    public CastContext(UnitController caster, NetworkedSkillInstance skillInstance)
    {
        this.caster = caster;
        this.skillInstance = skillInstance;
    }
}
