using System;
using System.Collections;
using Mirror;
using UnityEngine;

/// <summary>
/// Attached to spawned units to manage their lifetime and provide a despawn event.
/// All logic runs server-side only.
/// </summary>
public class SpawnedUnitLifetime : MonoBehaviour
{
    public event Action OnDespawning = delegate { };

    private float _despawnDelay;
    private Coroutine _lifetimeCoroutine;
    private bool _isDespawning;

    public void Initialize(float lifetime, float despawnDelay)
    {
        _despawnDelay = despawnDelay;

        if (lifetime > 0f)
        {
            _lifetimeCoroutine = StartCoroutine(LifetimeRoutine(lifetime));
        }
    }

    public void Despawn()
    {
        Despawn(_despawnDelay);
    }

    public void Despawn(float delay)
    {
        if (_isDespawning) return;
        _isDespawning = true;

        if (_lifetimeCoroutine != null)
        {
            StopCoroutine(_lifetimeCoroutine);
            _lifetimeCoroutine = null;
        }

        OnDespawning.Invoke();

        if (delay > 0f)
        {
            StartCoroutine(DelayedDestroy(delay));
        }
        else
        {
            DestroyUnit();
        }
    }

    private IEnumerator LifetimeRoutine(float lifetime)
    {
        yield return new WaitForSeconds(lifetime);
        Despawn();
    }

    private IEnumerator DelayedDestroy(float delay)
    {
        yield return new WaitForSeconds(delay);
        DestroyUnit();
    }

    private void DestroyUnit()
    {
        if (gameObject != null)
        {
            NetworkServer.Destroy(gameObject);
        }
    }
}
