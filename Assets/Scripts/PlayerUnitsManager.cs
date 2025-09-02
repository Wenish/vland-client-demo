using System.Collections.Generic;
using Mirror;
using MyGame.Events;
using UnityEngine;

public class PlayerUnitsManager : NetworkBehaviour
{
    public static PlayerUnitsManager Instance { get; private set; }
    public GameObject UnitPrefab;

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
            Debug.Log($"Existing connection found: connectionId={conn.connectionId}");
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

    private void HandlePlayerEnterRoom(NetworkConnectionToClient conn)
    {
        // Handle player joining logic here
        Debug.Log($"Player connected: connectionId={conn.connectionId}");
        SpawnPlayerUnit(conn);
    }
    private void HandlePlayerExitRoom(NetworkConnectionToClient conn)
    {
        Debug.Log($"Player disconnected: connectionId={conn.connectionId}");
        DespawnPlayerUnit(conn);
    }

    public void SpawnPlayerUnit(NetworkConnectionToClient conn)
    {
        var existingPlayerUnit = playerUnits.Find(pu => pu.ConnectionId == conn.connectionId);
        if (existingPlayerUnit.Unit != null)
        {
            Debug.Log($"Player unit already exists for connectionId={conn.connectionId}");
            return;
        }

        var unit = UnitSpawner.Instance.SpawnUnit("Player", Vector3.zero, Quaternion.Euler(0f, 0f, 0f));
        playerUnits.Add(new PlayerUnit { ConnectionId = conn.connectionId, Unit = unit });
        EventManager.Instance.Publish(new PlayerUnitSpawnedEvent(conn.connectionId, unit));
        RpcPlayerUnitSpawned(conn.connectionId, unit);
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