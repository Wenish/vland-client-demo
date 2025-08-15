using System;
using Mirror;
using UnityEngine;

public class UnitActionState : NetworkBehaviour
{

    [SerializeField, SyncVar(hook = nameof(OnActionStateDataChanged))]
    private ActionStateData _state = new ActionStateData();
    public ActionStateData state => _state;

    public bool IsActive => _state.type != ActionType.Idle;

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

    [Client]
    private void OnActionStateDataChanged(ActionStateData oldValue, ActionStateData newValue)
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