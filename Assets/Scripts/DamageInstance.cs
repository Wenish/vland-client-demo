/// <summary>
/// Carries typed damage components for a single hit.
/// Physical and Magic are reduced by Armor/MagicResist and DamageReduction.
/// True damage bypasses all resistances and reductions.
/// </summary>
public enum DamageSourceKind : byte
{
    Default = 0,
    BasicAttack = 1
}

public struct DamageInstance
{
    public float physical;
    public float magic;
    public float trueDamage;
    public DamageSourceKind sourceKind;
    public bool isCritical;

    public bool IsEmpty => physical <= 0f && magic <= 0f && trueDamage <= 0f;

    public DamageInstance(float physical, float magic, float trueDamage, DamageSourceKind sourceKind = DamageSourceKind.Default, bool isCritical = false)
    {
        this.physical = physical;
        this.magic = magic;
        this.trueDamage = trueDamage;
        this.sourceKind = sourceKind;
        this.isCritical = isCritical;
    }

    public static DamageInstance Physical(float amount, DamageSourceKind sourceKind = DamageSourceKind.Default) => new DamageInstance(amount, 0f, 0f, sourceKind);
    public static DamageInstance Magic(float amount) => new DamageInstance(0f, amount, 0f);
    public static DamageInstance True(float amount) => new DamageInstance(0f, 0f, amount);
}
