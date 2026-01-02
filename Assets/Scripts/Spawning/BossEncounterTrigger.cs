using Mirror;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Trigger component for activating boss encounters.
/// Attach to a collider to trigger boss spawning when a player enters.
/// Useful for arena-style boss encounters or scripted encounters.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BossEncounterTrigger : NetworkBehaviour
{
    [Header("Trigger Configuration")]
    [Tooltip("The boss spawner to activate")]
    public BossSpawner bossSpawner;

    [Tooltip("Only trigger for units on this team (0 = any team)")]
    public int triggerTeam = 1; // Default to player team

    [Tooltip("Trigger only once")]
    public bool oneTimeUse = true;

    [Tooltip("Destroy trigger after use")]
    public bool destroyAfterUse = false;

    [Header("Events")]
    public UnityEvent onTriggerActivated;

    private bool hasBeenTriggered = false;

    private void Awake()
    {
        // Ensure the collider is a trigger
        Collider collider = GetComponent<Collider>();
        if (collider != null && !collider.isTrigger)
        {
            collider.isTrigger = true;
            Debug.LogWarning($"[BossEncounterTrigger] Collider on '{gameObject.name}' was not set as trigger. Setting it now.");
        }
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        if (hasBeenTriggered && oneTimeUse) return;

        // Check if the triggering object is a unit
        UnitController unit = other.GetComponent<UnitController>();
        if (unit == null) return;

        // Check team if specified
        if (triggerTeam != 0 && unit.team != triggerTeam) return;

        // Activate the boss encounter
        ActivateBossEncounter();
    }

    [Server]
    private void ActivateBossEncounter()
    {
        if (bossSpawner == null)
        {
            Debug.LogError($"[BossEncounterTrigger] No boss spawner assigned to trigger '{gameObject.name}'!");
            return;
        }

        bossSpawner.ActivateEncounter();

        hasBeenTriggered = true;
        onTriggerActivated?.Invoke();

        if (destroyAfterUse)
        {
            Destroy(gameObject);
        }
    }

    #region Editor Gizmos

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta * 0.5f;
        
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            
            if (collider is BoxCollider box)
            {
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (collider is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            
            if (collider is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size * 0.95f);
            }
            else if (collider is SphereCollider sphere)
            {
                Gizmos.DrawSphere(sphere.center, sphere.radius * 0.95f);
            }
        }

#if UNITY_EDITOR
        if (bossSpawner != null)
        {
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, bossSpawner.transform.position);
            
            Vector3 labelPos = transform.position + Vector3.up * 2f;
            UnityEditor.Handles.Label(labelPos, $"Boss Trigger\n→ {bossSpawner.name}");
        }
#endif
    }

    #endregion
}
