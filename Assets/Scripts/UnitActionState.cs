using System;
using Mirror;
using UnityEngine;

public class UnitActionState : NetworkBehaviour
{

    [SerializeField, SyncVar(hook = nameof(OnActionStateDataChanged))]
    private ActionStateData _state = new ActionStateData();
    public ActionStateData state => _state;

    [SerializeField, SyncVar(hook = nameof(OnChildStateDataChanged))]
    private ActionStateData _childState = new ActionStateData();
    public ActionStateData childState => _childState;

    public bool IsActive => _state.type != ActionType.Idle;
    public bool HasChild => _childState.type != ActionType.Idle;

    public event Action<UnitActionState> OnActionStateChanged = delegate { };

    [Server]
    public void SetUnitActionState(ActionType newActionType, double startTime, double duration, string name)
    {
        _state = new ActionStateData
        {
            type = newActionType,
            startTime = startTime,
            duration = duration,
            name = name
        };
        OnActionStateChanged.Invoke(this);
    }

    [Server]
    public void SetUnitActionStateToIdle()
    {
        SetUnitActionState(ActionType.Idle, NetworkTime.time, 0, "");
    }

    [Server]
    public void SetChildActionState(ActionType type, double startTime, double duration, string name)
    {
        _childState = new ActionStateData
        {
            type = type,
            startTime = startTime,
            duration = duration,
            name = name
        };
        OnActionStateChanged.Invoke(this);
    }

    [Server]
    public void ClearChildActionState()
    {
        _childState = new ActionStateData();
        OnActionStateChanged.Invoke(this);
    }

    [Client]
    private void OnActionStateDataChanged(ActionStateData oldValue, ActionStateData newValue)
    {
        if (isServer) return;
        OnActionStateChanged.Invoke(this);
    }

    [Client]
    private void OnChildStateDataChanged(ActionStateData oldValue, ActionStateData newValue)
    {
        if (isServer) return;
        OnActionStateChanged.Invoke(this);
    }


    [Serializable]
    public struct ActionStateData
    {
        public ActionType type;
        public double startTime;
        public double duration;
        public string name;
    }


    public enum ActionType : byte
    {
        Idle,
        Casting,
        Channeling,
        Attacking
    }
}