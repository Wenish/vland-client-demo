using Mirror;
using UnityEngine;

public class PropsPushVolume : NetworkBehaviour
{
    [SerializeField] float impulseStrength = 2f;
    [SerializeField] float maxImpulse = 2;
    [SerializeField] float lift = 0.5f;

    Rigidbody playerRb;

    void Awake()
    {
        playerRb = GetComponentInParent<Rigidbody>();
    }

    // Runs on server only; still receives physics callbacks there
    [ServerCallback]
    void OnTriggerStay(Collider other)
    {
        var rb = other.attachedRigidbody;
        if (rb == null || rb.isKinematic) return;

        if (other.gameObject.layer != LayerMask.NameToLayer("Props")) return;

        Vector3 playerPos = playerRb ? playerRb.worldCenterOfMass : transform.position;
        Vector3 contact = other.ClosestPoint(playerPos);
        Vector3 dir = contact - playerPos;
        if (dir.sqrMagnitude < 1e-6f) dir = other.transform.position - playerPos;
        if (dir.sqrMagnitude < 1e-6f) return;
        dir.Normalize();

        float approachSpeed = playerRb ? Vector3.Dot(playerRb.linearVelocity, dir) : 0f;
        if (approachSpeed <= 0f) return;

        Vector3 impulse = dir * (approachSpeed * impulseStrength);
        if (lift > 0f) impulse += Vector3.up * lift;

        if (impulse.magnitude > maxImpulse) impulse = impulse.normalized * maxImpulse;

        rb.AddForceAtPosition(impulse, contact, ForceMode.Impulse);
    }
}
