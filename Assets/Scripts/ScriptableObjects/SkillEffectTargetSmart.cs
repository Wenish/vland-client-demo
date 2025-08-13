using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSkillEffectTargetSmart", menuName = "Game/Skills/Effects/Target/Smart")]
public class SkillEffectTargetSmart : SkillEffectTarget
{
    [Tooltip("The range within which the skill can target units.")]
    public float range;

    [Header("Smart Targeting")]
    public float searchRadius = 5f;
    public LayerMask unitLayer;

    [Header("Weights (0..1 typical)")]
    [Tooltip("How much to favor targets closer to the aim point. 0 = ignore aim point; pick purely by distance.")]
    public float weightAimPointDistance = 1f;

    [Tooltip("How much to favor targets closer to the caster. 0 = ignore caster position; pick purely by aim point.")]
    public float weightCasterDistance = 0f;

    [Tooltip("How much to favor targets in the forward direction of the caster. 0 = ignore facing; pick purely by aim point/distance.")]
    public float weightForwardAlignment = 0f;

    public override List<UnitController> GetTargets(CastContext castContext, List<UnitController> targets)
    {
        var caster = castContext.caster;

        Vector3 aimPoint = castContext.aimPoint ?? caster.transform.position;

        // Find all potential targets within the search radius
        Collider[] hitColliders = Physics.OverlapSphere(aimPoint, searchRadius, unitLayer);
        List<UnitController> potentialTargets = new List<UnitController>();
        foreach (Collider collider in hitColliders)
        {
            UnitController targetController = collider.GetComponentInParent<UnitController>();
            if (targetController == null || !IsWithinRange(caster, targetController)) continue;
            potentialTargets.Add(targetController);
        }

        var filteredTargets = ApplyCommonFilters(castContext, potentialTargets);

        var casterPos = caster.transform.position;
        var casterForward = caster.transform.forward;

        var scored = filteredTargets
            .Select(target =>
            {
                Vector3 upos = target.transform.position;
                float distAim = Vector3.Distance(upos, aimPoint);
                float distCaster = Vector3.Distance(upos, casterPos);
                float ang = Vector3.Angle(casterForward, (upos - casterPos).normalized);

                // Normalize factors
                float ndAim = distAim / Mathf.Max(searchRadius, 0.01f);
                float ndCaster = distCaster / Mathf.Max(range, 0.01f);
                float nAng = ang / 180f;

                float score =
                    ndAim * weightAimPointDistance +
                    ndCaster * weightCasterDistance +
                    nAng * weightForwardAlignment;

                return (unit: target, score);
            })
            .OrderBy(target => target.score)
            .Select(target => target.unit);

        var best = scored.FirstOrDefault();
        return best != null ? new List<UnitController> { best } : new List<UnitController>(0);

    }

    private bool IsWithinRange(UnitController caster, UnitController target)
    {
        if (caster == null || target == null) return false;

        Vector3 casterPos = caster.transform.position;
        Vector3 targetPos = target.transform.position;

        float maxRange = Mathf.Max(0f, range);
        return (targetPos - casterPos).sqrMagnitude <= maxRange * maxRange;
    }
}
