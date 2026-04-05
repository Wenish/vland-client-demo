/// <summary>
/// Carries typed damage components for a single hit.
/// Physical and Magic are reduced by Armor/MagicResist and DamageReduction.
/// True damage bypasses all resistances and reductions.
/// </summary>
public struct DamageInstance
{
    public float physical;
    public float magic;
    public float trueDamage;

    public bool IsEmpty => physical <= 0f && magic <= 0f && trueDamage <= 0f;

    public DamageInstance(float physical, float magic, float trueDamage)
    {
        this.physical = physical;
        this.magic = magic;
        this.trueDamage = trueDamage;
    }

    public static DamageInstance Physical(float amount) => new DamageInstance(amount, 0f, 0f);
    public static DamageInstance Magic(float amount) => new DamageInstance(0f, amount, 0f);
    public static DamageInstance True(float amount) => new DamageInstance(0f, 0f, amount);
}
