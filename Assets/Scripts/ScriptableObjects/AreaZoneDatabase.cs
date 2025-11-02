using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AreaZoneDatabase", menuName = "Game/AreaZone/Database")]
public class AreaZoneDatabase : ScriptableObject
{
    public List<AreaZoneData> allAreaZones = new List<AreaZoneData>();

    public AreaZoneData GetAreaZoneByName(string name)
    {
        return allAreaZones.Find(areaZone => areaZone.areaZoneName == name);
    }
}