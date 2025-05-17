public enum StatType
{
    Health,
    MovementSpeed,
    Shield,
    TurnSpeed,
}

[System.Serializable]
public class StatModifier
{
    public StatType Type;
    public float Value;
    public ModifierType ModifierType;
}