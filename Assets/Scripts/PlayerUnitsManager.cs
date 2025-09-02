using Mirror;
using MyGame.Events;
using UnityEngine;

public class PlayerUnitsManager : NetworkBehaviour
{
    public static PlayerUnitsManager Instance { get; private set; }

    private MyNetworkRoomManager roomManager;

    public struct PlayerUnit
    {
        public int ConnectionId;
        public GameObject Unit;
    }

    public readonly SyncList<PlayerUnit> playerUnits = new SyncList<PlayerUnit>();


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        roomManager = FindFirstObjectByType<MyNetworkRoomManager>();
    }

    private void Start()
    {
        FindAllExistingConnections();
    }

    public void FindAllExistingConnections()
    {
        foreach (var conn in NetworkServer.connections.Values)
        {
            SpawnPlayerUnit(conn);
        }
    }

    void OnEnable()
    {
        roomManager.OnPlayerEnterRoom += HandlePlayerEnterRoom;
        roomManager.OnPlayerExitRoom += HandlePlayerExitRoom;
    }
    void OnDisable()
    {
        roomManager.OnPlayerEnterRoom -= HandlePlayerEnterRoom;
        roomManager.OnPlayerExitRoom -= HandlePlayerExitRoom;
    }

    public GameObject GetPlayerUnit(int connectionId)
    {
        var playerUnit = playerUnits.Find(pu => pu.ConnectionId == connectionId);
        if (playerUnit.Unit == null) return null;

        return playerUnit.Unit;
    }   

    private void HandlePlayerEnterRoom(NetworkConnectionToClient conn)
    {
        SpawnPlayerUnit(conn);
    }
    private void HandlePlayerExitRoom(NetworkConnectionToClient conn)
    {
        DespawnPlayerUnit(conn);
    }

    public void SpawnPlayerUnit(NetworkConnectionToClient conn)
    {
        var existingPlayerUnit = playerUnits.Find(pu => pu.ConnectionId == conn.connectionId);
        if (existingPlayerUnit.Unit != null) return; // Already spawned

        var spawnPoint = GetNextPlayerSpawnPoint();
        var unit = UnitSpawner.Instance.SpawnUnit("Player", spawnPoint, Quaternion.Euler(0f, 0f, 0f));
        playerUnits.Add(new PlayerUnit { ConnectionId = conn.connectionId, Unit = unit });
        EventManager.Instance.Publish(new PlayerUnitSpawnedEvent(conn.connectionId, unit));
        RpcPlayerUnitSpawned(conn.connectionId, unit);
    }

    [SerializeField] private float spawnCircleRadius = 2f;
    [SerializeField] private float spawnAngleStepDegrees = 90f;
    private int spawnIndex = 0;

    public Vector3 GetNextPlayerSpawnPoint()
    {
        float angleRad = spawnIndex * spawnAngleStepDegrees * Mathf.Deg2Rad;
        Vector3 spawnPoint = new Vector3(Mathf.Cos(angleRad) * spawnCircleRadius, 0f, Mathf.Sin(angleRad) * spawnCircleRadius);
        spawnIndex++;
        return spawnPoint;
    }

    [ClientRpc]
    public void RpcPlayerUnitSpawned(int connectionId, GameObject unit)
    {
        if (!isServer) return;
        EventManager.Instance.Publish(new PlayerUnitSpawnedEvent(connectionId, unit));
    }

    public void DespawnPlayerUnit(NetworkConnectionToClient conn)
    {
        var existingPlayerUnit = playerUnits.Find(pu => pu.ConnectionId == conn.connectionId);
        if (existingPlayerUnit.Unit != null)
        {
            playerUnits.Remove(existingPlayerUnit);
            NetworkServer.Destroy(existingPlayerUnit.Unit);
        }
    }
}