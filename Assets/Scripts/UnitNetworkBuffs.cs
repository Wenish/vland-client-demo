using Mirror;
using UnityEngine;

public class UnitNetworkBuffs : NetworkBehaviour
{
    public readonly SyncList<NetworkBuffData> NetworkBuffs = new();

    [SerializeField]
    public BuffSystem _buffSystem;

    public class NetworkBuffData
    {
        public string BuffId;
        public float Duration;
        public float Remaining;
        public string SkillName;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        _buffSystem = GetComponent<UnitMediator>().Buffs;
        _buffSystem.OnBuffAdded += HandleBuffAdded;
        _buffSystem.OnBuffRemoved += HandleBuffRemoved;

    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("Initializing NetworkBuffs on client.");
        for (int i = 0; i < NetworkBuffs.Count; i++)
        {
            Debug.Log($"Client has buff {NetworkBuffs[i].BuffId} with {NetworkBuffs[i].Remaining}/{NetworkBuffs[i].Duration} remaining.");
            NetworkBuffs.OnAdd.Invoke(i);
        }
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        if (_buffSystem != null)
        {
            _buffSystem.OnBuffAdded -= HandleBuffAdded;
            _buffSystem.OnBuffRemoved -= HandleBuffRemoved;
        }
    }

    [ServerCallback]
    private void HandleBuffAdded(Buff buff)
    {
        Debug.Log($"Adding buff {buff.BuffId} to NetworkBuffs.");
        NetworkBuffs.Add(new NetworkBuffData
        {
            BuffId = buff.BuffId,
            Duration = buff.Duration,
            Remaining = buff.Remaining,
            SkillName = buff.SkillName
        });
    }

    [ServerCallback]
    private void HandleBuffRemoved(Buff buff)
    {
        Debug.Log($"Removing buff {buff.BuffId} from NetworkBuffs.");
        for (int i = 0; i < NetworkBuffs.Count; i++)
        {
            if (NetworkBuffs[i].BuffId == buff.BuffId)
            {
                NetworkBuffs.RemoveAt(i);
                break;

            }
        }
    }

}