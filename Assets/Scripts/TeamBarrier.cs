using System.Collections.Generic;
using Mirror;
using UnityEngine;

[DisallowMultipleComponent]
public class TeamBarrier : MonoBehaviour
{
    [Header("Colliders")]
    [Tooltip("Solid collider that should block disallowed teams.")]
    [SerializeField] private Collider barrierCollider;

    [Tooltip("Trigger collider used to detect units approaching/leaving the barrier.")]
    [SerializeField] private Collider detectionTrigger;

    [Header("Access")]
    [Tooltip("Teams in this list can pass through the barrier.")]
    [SerializeField] private List<int> allowedTeams = new List<int> { 0 };

    private readonly Dictionary<UnitController, HashSet<Collider>> collidersByUnit = new Dictionary<UnitController, HashSet<Collider>>();
    private readonly HashSet<UnitController> trackedUnits = new HashSet<UnitController>();
    private readonly Dictionary<UnitController, System.Action<UnitController>> teamChangedHandlersByUnit = new Dictionary<UnitController, System.Action<UnitController>>();

    private void Reset()
    {
        AutoAssignColliders();
    }

    private void OnValidate()
    {
        AutoAssignColliders();

        if (barrierCollider != null)
        {
            barrierCollider.isTrigger = false;
        }

        if (detectionTrigger != null)
        {
            detectionTrigger.isTrigger = true;
        }
    }

    private void OnDisable()
    {
        UnsubscribeAllTeamChanged();
        RestoreAllIgnoredCollisions();
    }

    private void OnDestroy()
    {
        UnsubscribeAllTeamChanged();
        RestoreAllIgnoredCollisions();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!NetworkServer.active)
        {
            return;
        }

        TrySetPassThrough(other, true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!NetworkServer.active)
        {
            return;
        }

        TrySetPassThrough(other, false);
    }

    private void TrySetPassThrough(Collider other, bool shouldPass)
    {
        if (barrierCollider == null || detectionTrigger == null)
        {
            return;
        }

        UnitController unit = other.GetComponentInParent<UnitController>();
        if (unit == null)
        {
            return;
        }

        if (shouldPass)
        {
            RegisterUnitCollider(unit, other);
            EnsureTeamChangedSubscription(unit);
        }
        else
        {
            UnregisterUnitCollider(unit, other);
        }

        bool isAllowed = allowedTeams.Contains(unit.team);
        ApplyPassThroughForUnit(unit, isAllowed && shouldPass);
    }

    private void RegisterUnitCollider(UnitController unit, Collider collider)
    {
        if (!collidersByUnit.TryGetValue(unit, out HashSet<Collider> colliders))
        {
            colliders = new HashSet<Collider>();
            collidersByUnit[unit] = colliders;
        }

        colliders.Add(collider);
        trackedUnits.Add(unit);
    }

    private void UnregisterUnitCollider(UnitController unit, Collider collider)
    {
        if (!collidersByUnit.TryGetValue(unit, out HashSet<Collider> colliders))
        {
            return;
        }

        colliders.Remove(collider);
        if (colliders.Count == 0)
        {
            collidersByUnit.Remove(unit);
            trackedUnits.Remove(unit);
            RemoveTeamChangedSubscription(unit);
        }
    }

    private void ApplyPassThroughForUnit(UnitController unit, bool shouldIgnore)
    {
        if (!collidersByUnit.TryGetValue(unit, out HashSet<Collider> colliders))
        {
            return;
        }

        foreach (Collider unitCollider in colliders)
        {
            if (unitCollider == null) continue;
            Physics.IgnoreCollision(barrierCollider, unitCollider, shouldIgnore);
        }
    }

    private void EnsureTeamChangedSubscription(UnitController unit)
    {
        if (teamChangedHandlersByUnit.ContainsKey(unit))
        {
            return;
        }

        System.Action<UnitController> handler = _ =>
        {
            if (!NetworkServer.active || barrierCollider == null)
            {
                return;
            }

            if (!trackedUnits.Contains(unit))
            {
                return;
            }

            bool isAllowed = allowedTeams.Contains(unit.team);
            ApplyPassThroughForUnit(unit, isAllowed);
        };

        teamChangedHandlersByUnit[unit] = handler;
        unit.OnTeamChanged += handler;
    }

    private void RemoveTeamChangedSubscription(UnitController unit)
    {
        if (!teamChangedHandlersByUnit.TryGetValue(unit, out System.Action<UnitController> handler))
        {
            return;
        }

        if (unit != null)
        {
            unit.OnTeamChanged -= handler;
        }

        teamChangedHandlersByUnit.Remove(unit);
    }

    private void UnsubscribeAllTeamChanged()
    {
        foreach (KeyValuePair<UnitController, System.Action<UnitController>> pair in teamChangedHandlersByUnit)
        {
            if (pair.Key == null) continue;
            pair.Key.OnTeamChanged -= pair.Value;
        }

        teamChangedHandlersByUnit.Clear();
    }

    private void CleanupNullUnits()
    {
        List<UnitController> deadUnits = null;

        foreach (UnitController unit in trackedUnits)
        {
            if (unit != null)
            {
                continue;
            }

            deadUnits ??= new List<UnitController>();
            deadUnits.Add(unit);
        }

        if (deadUnits == null)
        {
            return;
        }

        for (int i = 0; i < deadUnits.Count; i++)
        {
            UnitController deadUnit = deadUnits[i];
            trackedUnits.Remove(deadUnit);
            collidersByUnit.Remove(deadUnit);
            RemoveTeamChangedSubscription(deadUnit);
        }
    }

    private void RestoreAllIgnoredCollisions()
    {
        if (barrierCollider == null)
        {
            collidersByUnit.Clear();
            trackedUnits.Clear();
            return;
        }

        CleanupNullUnits();

        foreach (HashSet<Collider> colliders in collidersByUnit.Values)
        {
            foreach (Collider ignored in colliders)
            {
                if (ignored == null) continue;
                Physics.IgnoreCollision(barrierCollider, ignored, false);
            }
        }

        collidersByUnit.Clear();
        trackedUnits.Clear();
    }

    private void AutoAssignColliders()
    {
        Collider[] colliders = GetComponents<Collider>();

        if (barrierCollider == null)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (!colliders[i].isTrigger)
                {
                    barrierCollider = colliders[i];
                    break;
                }
            }
        }

        if (detectionTrigger == null)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].isTrigger)
                {
                    detectionTrigger = colliders[i];
                    break;
                }
            }
        }
    }
}