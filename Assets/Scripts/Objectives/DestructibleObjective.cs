using Mirror;
using MyGame.Events;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(UnitController))]
public class DestructibleObjective : NetworkBehaviour
{
    [Header("Identity")]
    [SerializeField] private string objectiveId = "objective";

    [SyncVar(hook = nameof(HookDestroyedChanged))]
    private bool isDestroyed;

    [SyncVar]
    private int lastValidHitterTeamId = -1;

    [SyncVar]
    private uint lastValidHitterNetId;

    private UnitController _unit;

    public string ObjectiveId => objectiveId;
    public bool IsDestroyed => isDestroyed;
    public int LastValidHitterTeamId => lastValidHitterTeamId;
    public uint LastValidHitterNetId => lastValidHitterNetId;
    public UnitController ObjectiveUnit => _unit;

    public event System.Action<bool> OnDestroyedStateChanged = delegate { };
    public event System.Action<DestructibleObjective, UnitController, int> OnDestroyedServer = delegate { };
    public event System.Action<DestructibleObjective> OnRebuiltServer = delegate { };

    private void Awake()
    {
        _unit = GetComponent<UnitController>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (_unit == null)
        {
            _unit = GetComponent<UnitController>();
        }

        if (_unit == null)
        {
            Debug.LogError($"[{nameof(DestructibleObjective)}] Missing UnitController on {name}.", this);
            return;
        }

        if (EventManager.Instance != null)
        {
            EventManager.Instance.Subscribe<UnitDamagedEvent>(OnUnitDamagedEvent);
            EventManager.Instance.Subscribe<UnitDiedEvent>(OnUnitDiedEvent);
        }

        isDestroyed = _unit.IsDead;
        OnDestroyedStateChanged(isDestroyed);
    }

    public override void OnStopServer()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.Unsubscribe<UnitDamagedEvent>(OnUnitDamagedEvent);
            EventManager.Instance.Unsubscribe<UnitDiedEvent>(OnUnitDiedEvent);
        }

        base.OnStopServer();
    }

    [Server]
    public void ServerRebuildNow()
    {
        if (_unit == null)
        {
            return;
        }

        if (!isDestroyed && !_unit.IsDead)
        {
            return;
        }

        _unit.SetHealth(_unit.maxHealth);
        _unit.SetShield(0);

        isDestroyed = false;
        OnDestroyedStateChanged(false);
        OnRebuiltServer(this);
        EventManager.Instance?.Publish(new ObjectiveRebuiltEvent(_unit, objectiveId));
    }

    [Server]
    public void ServerSetLastValidHitter(UnitController attacker)
    {
        if (attacker == null)
        {
            return;
        }

        if (attacker.team < 0)
        {
            return;
        }

        lastValidHitterTeamId = attacker.team;
        lastValidHitterNetId = attacker.netId;
    }

    private void OnUnitDamagedEvent(UnitDamagedEvent evt)
    {
        if (!isServer)
        {
            return;
        }

        if (_unit == null || evt == null || evt.Unit != _unit)
        {
            return;
        }

        if (evt.Attacker == null)
        {
            return;
        }

        ServerSetLastValidHitter(evt.Attacker);
    }

    private void OnUnitDiedEvent(UnitDiedEvent evt)
    {
        if (!isServer)
        {
            return;
        }

        if (_unit == null || evt == null || evt.Unit != _unit)
        {
            return;
        }

        HandleObjectiveDestroyed(evt.Killer);
    }

    [Server]
    private void HandleObjectiveDestroyed(UnitController killer)
    {
        if (isDestroyed)
        {
            return;
        }

        isDestroyed = true;
        OnDestroyedStateChanged(true);

        int resolvedTeam = -1;
        if (killer != null && killer.team >= 0)
        {
            resolvedTeam = killer.team;
        }
        else if (lastValidHitterTeamId >= 0)
        {
            resolvedTeam = lastValidHitterTeamId;
        }

        OnDestroyedServer(this, killer, resolvedTeam);
        EventManager.Instance?.Publish(new ObjectiveDestroyedEvent(_unit, objectiveId, killer, resolvedTeam));
    }

    private void HookDestroyedChanged(bool oldValue, bool newValue)
    {
        OnDestroyedStateChanged(newValue);
    }
}