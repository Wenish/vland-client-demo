using Mirror;
using MyGame.Events;
using UnityEngine;
using System.Collections.Generic;

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

    private const int BotConnectionIdStart = -100000;
    private int _nextBotConnectionId = BotConnectionIdStart;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        roomManager = FindAnyObjectByType<MyNetworkRoomManager>();
    }

    private void Start()
    {
        if (!isServer)
        {
            return;
        }

        spawnIndex = 0;
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
        if (!isServer || conn == null)
        {
            return;
        }

        SpawnPlayerUnitInternal(conn.connectionId, "Player");
    }

    [Server]
    public GameObject SpawnBotPlayerUnit(string unitName = "Player")
    {
        int botConnectionId = AllocateBotConnectionId();
        return SpawnPlayerUnitInternal(botConnectionId, string.IsNullOrWhiteSpace(unitName) ? "Player" : unitName);
    }

    [Server]
    public bool DespawnBotPlayerUnit(int botConnectionId)
    {
        if (!IsBotConnectionId(botConnectionId))
        {
            return false;
        }

        for (int i = 0; i < playerUnits.Count; i++)
        {
            var playerUnit = playerUnits[i];
            if (playerUnit.ConnectionId != botConnectionId)
            {
                continue;
            }

            playerUnits.RemoveAt(i);
            if (playerUnit.Unit != null)
            {
                NetworkServer.Destroy(playerUnit.Unit);
            }
            return true;
        }

        return false;
    }

    [Server]
    public int GetHumanPlayerCount()
    {
        int count = 0;
        for (int i = 0; i < playerUnits.Count; i++)
        {
            var playerUnit = playerUnits[i];
            if (playerUnit.Unit == null)
            {
                continue;
            }

            if (!IsBotConnectionId(playerUnit.ConnectionId))
            {
                count++;
            }
        }

        return count;
    }

    [Server]
    public int GetBotPlayerCount()
    {
        int count = 0;
        for (int i = 0; i < playerUnits.Count; i++)
        {
            var playerUnit = playerUnits[i];
            if (playerUnit.Unit == null)
            {
                continue;
            }

            if (IsBotConnectionId(playerUnit.ConnectionId))
            {
                count++;
            }
        }

        return count;
    }

    [Server]
    public void GetBotConnectionIds(List<int> output)
    {
        if (output == null)
        {
            return;
        }

        output.Clear();
        for (int i = 0; i < playerUnits.Count; i++)
        {
            var playerUnit = playerUnits[i];
            if (playerUnit.Unit == null)
            {
                continue;
            }

            if (IsBotConnectionId(playerUnit.ConnectionId))
            {
                output.Add(playerUnit.ConnectionId);
            }
        }
    }

    [Server]
    public bool IsBotConnectionId(int connectionId)
    {
        return connectionId < 0;
    }

    [Server]
    private int AllocateBotConnectionId()
    {
        while (true)
        {
            int candidate = _nextBotConnectionId--;
            bool isUsed = false;

            for (int i = 0; i < playerUnits.Count; i++)
            {
                if (playerUnits[i].ConnectionId == candidate)
                {
                    isUsed = true;
                    break;
                }
            }

            if (!isUsed)
            {
                return candidate;
            }
        }
    }

    [Server]
    private GameObject SpawnPlayerUnitInternal(int connectionId, string unitName)
    {
        for (int i = 0; i < playerUnits.Count; i++)
        {
            var existing = playerUnits[i];
            if (existing.ConnectionId == connectionId && existing.Unit != null)
            {
                return existing.Unit;
            }
        }

        Vector3 spawnPoint = GetNextPlayerSpawnPoint();
        var unit = UnitSpawner.Instance.SpawnUnit(unitName, spawnPoint, Quaternion.Euler(0f, 0f, 0f));

        if (unit != null)
        {
            var rb = unit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        playerUnits.Add(new PlayerUnit { ConnectionId = connectionId, Unit = unit });
        EventManager.Instance.Publish(new PlayerUnitSpawnedEvent(connectionId, unit));
        RpcPlayerUnitSpawned(connectionId, unit);
        return unit;
    }

    [SerializeField] private float spawnCircleRadius = 2f;
    [SerializeField] private float spawnAngleStepDegrees = 90f;
    [SerializeField] private float spawnCollisionRadius = 0.8f;
    [SerializeField] private int maxSpawnAttempts = 12;
    private int spawnIndex = 0;

    public Vector3 GetNextPlayerSpawnPoint()
    {
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            float angleRad = (spawnIndex + attempt) * spawnAngleStepDegrees * Mathf.Deg2Rad;
            Vector3 candidate = new Vector3(
                Mathf.Cos(angleRad) * spawnCircleRadius,
                0f,
                Mathf.Sin(angleRad) * spawnCircleRadius
            );

            if (IsSpawnPointFree(candidate))
            {
                spawnIndex++;
                return candidate;
            }
        }

        spawnIndex++;
        float fallbackAngle = spawnIndex * spawnAngleStepDegrees * Mathf.Deg2Rad;
        return new Vector3(
            Mathf.Cos(fallbackAngle) * spawnCircleRadius,
            0f,
            Mathf.Sin(fallbackAngle) * spawnCircleRadius
        );
    }

    [Server]
    private bool IsSpawnPointFree(Vector3 position)
    {
        bool isBlocked = Physics.CheckSphere(position, spawnCollisionRadius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        return !isBlocked;
    }

    [ClientRpc]
    public void RpcPlayerUnitSpawned(int connectionId, GameObject unit)
    {
        if (isServer) return;
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