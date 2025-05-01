public enum StatType
{
    Health,
    MovementSpeed,
    Shield,
}

public enum ModifierType
{
    Flat,
    Percent
}

[System.Serializable]
public class StatModifier
{
    public StatType Type;
    public float Value;
    public ModifierType ModifierType;
}