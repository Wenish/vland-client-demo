using UnityEngine;

public interface IAreaZoneSpawner
{
    GameObject SpawnAreaZone(AreaZoneData areaZoneData, Vector3 position, Quaternion rotation);
}
