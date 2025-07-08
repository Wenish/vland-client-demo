using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Effects/Mechanic/WarpToTarget")]
public class SkillEffectMechanicWarpToTargetData : SkillEffectMechanic
{
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
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero; // Stop movement (Unity 2022+)
                rb.position = firstTarget.transform.position; // Teleport instantly, no collision in between
            }
            else
            {
                castContext.caster.transform.position = firstTarget.transform.position;
            }
        }
        else
        {
            Debug.LogWarning("WarpToTarget should only be called on the server to ensure proper network sync.");
        }

        return newTargetList;
    }
}