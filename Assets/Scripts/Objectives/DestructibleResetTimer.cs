using System.Collections;
using Mirror;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(DestructibleObjective))]
public class DestructibleResetTimer : NetworkBehaviour
{
    [SerializeField, Min(0.1f)] private float rebuildDelaySeconds = 20f;

    private DestructibleObjective _objective;
    private Coroutine _rebuildCoroutine;

    private void Awake()
    {
        _objective = GetComponent<DestructibleObjective>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (_objective == null)
        {
            _objective = GetComponent<DestructibleObjective>();
        }

        if (_objective != null)
        {
            _objective.OnDestroyedServer += OnDestroyedServer;
            _objective.OnRebuiltServer += OnRebuiltServer;
        }
    }

    public override void OnStopServer()
    {
        if (_objective != null)
        {
            _objective.OnDestroyedServer -= OnDestroyedServer;
            _objective.OnRebuiltServer -= OnRebuiltServer;
        }

        if (_rebuildCoroutine != null)
        {
            StopCoroutine(_rebuildCoroutine);
            _rebuildCoroutine = null;
        }

        base.OnStopServer();
    }

    [Server]
    private void OnDestroyedServer(DestructibleObjective objective, UnitController killer, int resolvedTeamId)
    {
        if (_rebuildCoroutine != null)
        {
            StopCoroutine(_rebuildCoroutine);
        }

        _rebuildCoroutine = StartCoroutine(RebuildAfterDelay());
    }

    [Server]
    private void OnRebuiltServer(DestructibleObjective objective)
    {
        if (_rebuildCoroutine == null)
        {
            return;
        }

        StopCoroutine(_rebuildCoroutine);
        _rebuildCoroutine = null;
    }

    [Server]
    private IEnumerator RebuildAfterDelay()
    {
        yield return new WaitForSeconds(rebuildDelaySeconds);
        _rebuildCoroutine = null;
        _objective?.ServerRebuildNow();
    }
}