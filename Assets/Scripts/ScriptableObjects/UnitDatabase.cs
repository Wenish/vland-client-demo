using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitDatabase", menuName = "Game/UnitDatabase")]
public class UnitDatabase : ScriptableObject
{
    public List<UnitData> allUnits = new List<UnitData>();

    public UnitData GetUnitByName(string name)
    {
        return allUnits.Find(unit => unit.unitName == name);
    }
}