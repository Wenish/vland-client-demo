using System;
using System.Threading.Tasks;
using Mirror;
using MyGame.Events;
using UnityEngine;

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
        ZombiePrefab = MyNetworkRoomManager.singleton.spawnPrefabs.Find(prefab => prefab.name == "Unit");
    }

    void Start()
    {
        if (!isServer) return;
        EventManager.Instance.Subscribe<UnitDiedEvent>(OnUnitDied);
        EventManager.Instance.Subscribe<UnitDamagedEvent>(OnUnitDamagedEvent);
    }

    void OnDestroy()
    {
        if (!isServer) return;
        EventManager.Instance.Unsubscribe<UnitDiedEvent>(OnUnitDied);
        EventManager.Instance.Unsubscribe<UnitDamagedEvent>(OnUnitDamagedEvent);
    }


    // Update is called once per frame
    void Update()
    {
        if (!isServer) return;
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
        }

        isSpawingWave = false;
    }

    void HookOnCurrentWaveChanged(int oldValue, int newValue)
    {
        RasiseOnNewWaveStartedEvent();
    }

    Vector3 GetZombieSpawnPosition()
    {
        int spawnIndex = UnityEngine.Random.Range(0, ZombieSpawns.Length);
        return ZombieSpawns[spawnIndex].transform.position;
    }

    [Server]
    void SpawnZombie(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        var zombie = UnitSpawner.Instance.SpawnUnit("zombie", spawnPosition, spawnRotation);
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
            NetworkServer.Destroy(zombie);
        };
        
        unitController.moveSpeed = Mathf.Max(0, unitController.moveSpeed + UnityEngine.Random.Range(-1.5f, 0.5f));
        
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
    void OnUnitDamagedEvent(UnitDamagedEvent unitDamagedEvent)
    {
        var hasZombieTakenDamage = unitDamagedEvent.Unit.unitType == UnitType.Zombie;
        if (!hasZombieTakenDamage) return;
        var hasPlayerAttacked = unitDamagedEvent.Attacker.unitType == UnitType.Player;
        if (!hasPlayerAttacked) return;

        ZombieDropGold(unitDamagedEvent.Unit, unitDamagedEvent.Attacker, 1);
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
