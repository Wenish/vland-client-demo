using System.Collections.Generic;
using MyGame.Events;
using UnityEngine;

public class InteractionZone : MonoBehaviour
{
    public int interactionId;
    public InteractionType interactionType;
    public int goldCost = 0;

    private HashSet<UnitController> unitsInZone = new HashSet<UnitController>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<UnitController>(out var unit))
        {
            unitsInZone.Add(unit);
            EventManager.Instance.Publish(new UnitEnteredInteractionZone(unit, this));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<UnitController>(out var unit))
        {
            unitsInZone.Remove(unit);
            EventManager.Instance.Publish(new UnitExitedInteractionZone(unit, this));
        }
    }

    void OnDisable()
    {
        foreach (var unit in unitsInZone)
        {
            EventManager.Instance.Publish(new UnitExitedInteractionZone(unit, this));
        }
        unitsInZone.Clear();
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