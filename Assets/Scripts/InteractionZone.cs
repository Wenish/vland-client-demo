using MyGame.Events;
using UnityEngine;

public class InteractionZone : MonoBehaviour
{
    public string interactionId;
    public InteractionType interactionType;
    public int goldCost = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<UnitController>(out var unit))
        {
            EventManager.Instance.Publish(new UnitEnteredInteractionZone(unit, this));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<UnitController>(out var unit))
        {
            EventManager.Instance.Publish(new UnitExitedInteractionZone(unit, this));
        }
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