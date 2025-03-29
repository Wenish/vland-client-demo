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
    }

    void OnDestroy()
    {
        if (!isServer) return;
        EventManager.Instance.Unsubscribe<UnitDiedEvent>(OnUnitDied);
    }


    // Update is called once per frame
    void Update()
    {
        if (!isServer) return;
        if (isSpawingWave) return;

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
        var zombie = NetworkManager.Instantiate(ZombiePrefab, new Vector3(spawnPosition.x, 0, spawnPosition.z), spawnRotation);
        zombie.name = "Unit (Zombie)";

        var unitController = zombie.GetComponent<UnitController>();
        unitController.OnDied += async () => {
            zombiesAlive--;
            // Destroy the zombie
            await Task.Delay(5000);
            NetworkServer.Destroy(zombie);
        };
        unitController.unitType = UnitType.Zombie;
        unitController.SetMaxHealth(25);
        unitController.SetMaxShield(0);
        unitController.moveSpeed = 3;
        UnitEquipSword(unitController);
        NetworkServer.Spawn(zombie);
        zombie.AddComponent<AiZombieController>();
        var weaponMelee = zombie.GetComponent<WeaponMelee>();
        weaponMelee.attackPower = 40;
        weaponMelee.attackRange = 1.5f;
        weaponMelee.moveSpeedPercentWhileAttacking = 0.8f;
        weaponMelee.attackTime = 0.1f;
        weaponMelee.attackSpeed = 0.4f;
        weaponMelee.coneAngleRadians = 75;
        weaponMelee.numRays = 15;
    }

    void GetAllZombieSpawnInScene()
    {
        ZombieSpawns = FindObjectsByType<ZombieSpawnController>(FindObjectsSortMode.None);
    }

    [Server]
    void UnitEquipSword(UnitController unitController)
    {
        if (!unitController) return;
        if (unitController.weapon.attackCooldown > 0) return;

        WeaponMelee weaponMelee = unitController.GetComponent<WeaponMelee>();
        if (!weaponMelee) return;
        unitController.weapon = weaponMelee;
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

        ZombieDropGold(unitDiedEvent.Unit, unitDiedEvent.Killer);
    }

    [Server]
    public void ZombieDropGold(UnitController zombie, UnitController killer)
    {
        if (killer == null) return;
        if (killer.unitType != UnitType.Player) return;
        int amount = 10;
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
