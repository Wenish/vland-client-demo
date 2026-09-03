using System;

[Serializable]
public class LocalLoadout
{
    public string UnitName;
    public string PassiveId;
    public string Normal1Id;
    public string Normal2Id;
    public string Normal3Id;
    public string UltimateId;

    /// <summary>
    /// Beginner-friendly melee kit: Daggers + sustain, escape, and simple AoE ultimate.
    /// IDs must match SkillData.skillName. Weapon comes from equipped items.
    /// </summary>
    public static LocalLoadout CreateBeginnerDefault(string unitName = "")
    {
        return new LocalLoadout
        {
            UnitName = unitName ?? string.Empty,
            PassiveId = "Blessing Of Nature",
            Normal1Id = "Swiftness",
            Normal2Id = "Evade",
            Normal3Id = "Inner Barrier",
            UltimateId = "Let Hell Rain"
        };
    }

    public string[] GetNormals()
    {
        return new[] { Normal1Id, Normal2Id, Normal3Id };
    }

    public string[] GetPassives()
    {
        // single passive slot by design, still return array for API compatibility
        return new[] { PassiveId };
    }
}
