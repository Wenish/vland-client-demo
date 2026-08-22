using UnityEngine;

public interface IUnitSpawner
{
    GameObject SpawnUnit(string unitName, Vector3 position, Quaternion rotation, bool isNpc = false);
    GameObject Spawn(UnitData unitData, Vector3 position, Quaternion rotation, bool isNpc = false);
}
