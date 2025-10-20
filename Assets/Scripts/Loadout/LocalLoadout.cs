using System;

[Serializable]
public class LocalLoadout
{
    public string UnitName;
    public string WeaponId;
    public string PassiveId;
    public string Normal1Id;
    public string Normal2Id;
    public string Normal3Id;
    public string UltimateId;

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
