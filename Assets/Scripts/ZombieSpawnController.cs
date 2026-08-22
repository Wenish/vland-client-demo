using ShadowInfection.World;
using UnityEngine;

public class ZombieSpawnController : MonoBehaviour
{
    public bool isActive = false;
    public int spawnGroupId = 0;

    private void OnEnable()
    {
        ZombieSpawnRegistry.RegisterOrDefer(this);
    }

    private void OnDisable()
    {
        ZombieSpawnRegistry.UnregisterOrDefer(this);
    }
}
