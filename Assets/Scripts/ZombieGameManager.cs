using System;
using System.Threading.Tasks;
using Mirror;
using MyGame.Events;
using UnityEngine;
using UnityEngine.InputSystem;

public class ZombieGameManager : NetworkBehaviour
{
    public static ZombieGameManager Singleton { get; private set;}
    public GameObject ZombiePrefab;
    public ZombieSpawnController[] ZombieSpawns;

    [SyncVar(hook =  nameof(HookOnCurrentWaveChanged))]
    public int currentWave = 0;

    public int timeBetweenWaves = 10000;
    public int timeBetweenSpawns = 500;
    public int maxZombiesAlive = 5;
    public int zombiesPerWaveMultiplier = 5;
    public bool isGamePaused = false;

    [SerializeField]
    private int zombiesAlive = 0;
    [SerializeField]
    private bool isSpawingWave = false;
    // Start is called before the first frame update

    public event Action<int> OnNewWaveStarted = delegate {};
    void Awake()
    {
        Singleton = this;
        GetAllZombieSpawnInScene();
        ZombiePrefab = NetworkManager.singleton.spawnPrefabs.Find(prefab => prefab.name == "Unit");
    }

    void Start()
    {
        if (!isServer) return;
        EventManager.Instance.Subscribe<UnitDiedEvent>(OnUnitDied);
    }

    void OnDestroy()
    {
        EventManager.Instance.Unsubscribe<UnitDiedEvent>(OnUnitDied);

        StopAllCoroutines();
    }


    // Update is called once per frame
    void Update()
    {
        if (!isServer) return;
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            isGamePaused = !isGamePaused;
        }

        if (isSpawingWave) return;
        if (isGamePaused) return;


        if (zombiesAlive == 0)
        {
            isSpawingWave = true;
            _ = SpawnWave();
        }
    }

    [Server]
    async Task SpawnWave()
    {
        await Task.Delay(timeBetweenWaves);
        if (!isServer) return;

        currentWave++;
        var zombiesToSpawnThisWave = zombiesPerWaveMultiplier * currentWave;
        Quaternion spawnRotation = Quaternion.Euler(0f, 0f, 0f);
        for (int i = 0; i < zombiesToSpawnThisWave; i++)
        {
            if (zombiesAlive < maxZombiesAlive)
            {
                zombiesAlive++;
                SpawnZombie(GetZombieSpawnPosition(), spawnRotation);
            }
            else
            {
                i--;
            }
            await Task.Delay(timeBetweenSpawns);
            if (!isServer) return;
        }

        isSpawingWave = false;
    }

    void HookOnCurrentWaveChanged(int oldValue, int newValue)
    {
        RasiseOnNewWaveStartedEvent();
    }

    Vector3 GetZombieSpawnPosition()
    {
        var activeSpawns = Array.FindAll(ZombieSpawns, spawn => spawn.isActive);
        if (activeSpawns.Length == 0)
        {
            Debug.LogError("No active zombie spawns available.");
            return Vector3.zero;
        }
        int spawnIndex = UnityEngine.Random.Range(0, activeSpawns.Length);
        return activeSpawns[spawnIndex].transform.position;
    }

    [Server]
    void SpawnZombie(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        var zombieNameToSpawn = UnityEngine.Random.value < 0.8f ? "Infected" : "Crawler";

        var zombie = UnitSpawner.Instance.SpawnUnit(zombieNameToSpawn, spawnPosition, spawnRotation);
        if (zombie == null)
        {
            Debug.LogError("Failed to spawn zombie.");
            return;
        }
        var unitController = zombie.GetComponent<UnitController>();
        unitController.OnDied += async () => {
            zombiesAlive--;
            // Destroy the zombie
            await Task.Delay(5000);
            if (!isServer) return;
            NetworkServer.Destroy(zombie);
        };
        
        unitController.moveSpeed = Mathf.Max(0, unitController.moveSpeed + UnityEngine.Random.Range(-0.1f, 0.5f));
        var newMaxHealth = unitController.maxHealth;
        newMaxHealth += currentWave * 2;

        unitController.maxHealth = newMaxHealth;
        unitController.health = newMaxHealth;
        
        zombie.AddComponent<AiZombieController>();
    }

    void GetAllZombieSpawnInScene()
    {
        ZombieSpawns = FindObjectsByType<ZombieSpawnController>(FindObjectsSortMode.None);
    }
    
    public void RasiseOnNewWaveStartedEvent()
    {
        OnNewWaveStarted(currentWave);
        EventManager.Instance.Publish(new WaveStartedEvent(currentWave, zombiesPerWaveMultiplier * currentWave));
    }

    [Server]
    public void OnUnitDied(UnitDiedEvent unitDiedEvent)
    {
        var hasZombieDied = unitDiedEvent.Unit.unitType == UnitType.Zombie;
        if (!hasZombieDied) return;

        ZombieDropGold(unitDiedEvent.Unit, unitDiedEvent.Killer, 10);
    }

    [Server]
    public void ZombieDropGold(UnitController zombie, UnitController killer, int amount)
    {
        if (killer == null) return;
        if (killer.unitType != UnitType.Player) return;
        EventManager.Instance.Publish(new UnitDroppedGoldEvent(zombie, amount, killer));
        RpcZombieDroppedGold(amount, zombie, killer);
        EventManager.Instance.Publish(new PlayerReceivesGoldEvent(killer, amount));
        RpcPlayerReceivedGold(amount, killer);
    }

    [ClientRpc]
    public void RpcZombieDroppedGold(int amount, UnitController zombie, UnitController killer)
    {
        if (isServer) return;
        EventManager.Instance.Publish(new UnitDroppedGoldEvent(zombie, amount, killer));
    }

    [ClientRpc]
    public void RpcPlayerReceivedGold(int amount, UnitController player)
    {
        if (isServer) return;
        EventManager.Instance.Publish(new PlayerReceivesGoldEvent(player, amount));
    }
}
