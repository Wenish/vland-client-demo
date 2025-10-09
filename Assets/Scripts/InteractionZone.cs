using System.Collections.Generic;
using MyGame.Events;
using UnityEngine;

public class InteractionZone : MonoBehaviour
{
    public int interactionId;
    public InteractionType interactionType;
    public int goldCost = 0;

    private HashSet<UnitController> unitsInZone = new HashSet<UnitController>();
    private Dictionary<UnitController, System.Action> deathListeners = new Dictionary<UnitController, System.Action>();
    private Dictionary<UnitController, System.Action> reviveListeners = new Dictionary<UnitController, System.Action>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<UnitController>(out var unit))
        {
            if (unitsInZone.Contains(unit)) return;
            unitsInZone.Add(unit);
            // Listen for death
            System.Action onDied = () => OnUnitDiedInZone(unit);
            unit.OnDied += onDied;
            deathListeners[unit] = onDied;
            // Listen for revive
            System.Action onRevive = () => OnUnitRevivedInZone(unit);
            unit.OnRevive += onRevive;
            reviveListeners[unit] = onRevive;
            if (!unit.IsDead)
            {
                EventManager.Instance.Publish(new UnitEnteredInteractionZone(unit, this));
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<UnitController>(out var unit))
        {
            RemoveUnitFromZone(unit);
        }
    }

    private void OnUnitDiedInZone(UnitController unit)
    {
        // Remove interaction ability, but keep listeners for revive
        if (unitsInZone.Contains(unit))
        {
            EventManager.Instance.Publish(new UnitExitedInteractionZone(unit, this));
        }
    }

    private void RemoveUnitFromZone(UnitController unit)
    {
        if (unit == null) return;
        if (unitsInZone.Remove(unit))
        {
            if (deathListeners.TryGetValue(unit, out var onDied))
            {
                unit.OnDied -= onDied;
                deathListeners.Remove(unit);
            }
            if (reviveListeners.TryGetValue(unit, out var onRevive))
            {
                unit.OnRevive -= onRevive;
                reviveListeners.Remove(unit);
            }
            EventManager.Instance.Publish(new UnitExitedInteractionZone(unit, this));
        }
    }

    private void OnUnitRevivedInZone(UnitController unit)
    {
        // Only fire if the unit is still in the zone
        if (unitsInZone.Contains(unit))
        {
            EventManager.Instance.Publish(new UnitEnteredInteractionZone(unit, this));
        }
    }

    void OnDisable()
    {
        foreach (var unit in new List<UnitController>(unitsInZone))
        {
            RemoveUnitFromZone(unit);
        }
        unitsInZone.Clear();
        deathListeners.Clear();
        reviveListeners.Clear();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        if (TryGetComponent<SphereCollider>(out var sphereCollider))
        {
            Gizmos.DrawWireSphere(transform.position, sphereCollider.radius * transform.localScale.x);
        }
        else if (TryGetComponent<BoxCollider>(out var boxCollider))
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
    }
}

public enum InteractionType : byte
{
    OpenGate,
    BuyWeapon
}