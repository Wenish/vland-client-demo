public enum StatType
{
    Health,
    MovementSpeed,
    Shield,
    TurnSpeed,
    DamageReduction,
    AttackSpeed
}

[System.Serializable]
public class StatModifier
{
    public StatType Type;
    public float Value;
    public ModifierType ModifierType;
}