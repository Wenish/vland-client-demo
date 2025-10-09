using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Effects/Mechanic/WarpToTarget", fileName = "SkillEffectMechanicWarpToTarget")]
public class SkillEffectMechanicWarpToTargetData : SkillEffectMechanic
{
    public Vector2 warpOffset = Vector2.zero;
    public override List<UnitController> DoMechanic(
        CastContext castContext,
        List<UnitController> targets)
    {
        Debug.Log($"Warping {castContext.caster.name} to first target in list.");
        Debug.Log($"Number of targets: {targets.Count}");
        var firstTarget = targets.Count > 0 ? targets[0] : null;
        var newTargetList = new List<UnitController>();

        if (!firstTarget) return newTargetList;

        newTargetList.Add(firstTarget);

        // Move the caster to the first target's position (server-side for Mirror)
        if (Mirror.NetworkServer.active)
        {
            var rb = castContext.caster.GetComponent<Rigidbody>();
            // Compute destination using target position plus rotated offset (offset is in target's local XZ plane)
            var targetTransform = firstTarget.transform;
            var destination = targetTransform.position;
            if (warpOffset != Vector2.zero)
            {
                var localOffset = new Vector3(warpOffset.x, 0f, warpOffset.y);
                var rotatedOffset = targetTransform.rotation * localOffset;
                destination += rotatedOffset;
            }
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero; // Stop movement (Unity 2022+)
                rb.position = destination; // Teleport instantly, no collision in between
            }
            else
            {
                castContext.caster.transform.position = destination;
            }
        }
        else
        {
            Debug.LogWarning("WarpToTarget should only be called on the server to ensure proper network sync.");
        }

        return newTargetList;
    }
}