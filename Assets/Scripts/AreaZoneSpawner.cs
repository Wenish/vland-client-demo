using Mirror;
using UnityEngine;

public class AreaZoneSpawner : MonoBehaviour, IAreaZoneSpawner
{
    public GameObject areaZonePrefab;

    private void Awake()
    {
        areaZonePrefab = NetworkManager.singleton.spawnPrefabs.Find(prefab => prefab.name == "AreaZone");
    }

    public GameObject SpawnAreaZone(AreaZoneData areaZoneData, Vector3 position, Quaternion rotation)
    {
        GameObject areaZoneInstance = Instantiate(areaZonePrefab, position, rotation);

        // Spawn on the server first; initialization (SetAreaZoneName) is done by the caller
        // after subscribing to events to ensure the first tick can be observed.
        NetworkServer.Spawn(areaZoneInstance);

        return areaZoneInstance;
    }
}