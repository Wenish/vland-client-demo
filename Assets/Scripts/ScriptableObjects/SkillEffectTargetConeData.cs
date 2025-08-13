using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSkillEffect", menuName = "Game/Skills/Effects/Target/Cone")]
public class SkillEffectTargetCone : SkillEffectTarget
{
    public float range = 5f;
    public float angle = 45f;
    public LayerMask unitLayer;

    public override List<UnitController> GetTargets(CastContext castContext, List<UnitController> targets)
    {
        List<UnitController> collected = new List<UnitController>();

        Vector3 casterPosition = castContext.caster.transform.position;
        Vector3 forward = castContext.caster.transform.forward;

        // Get all colliders in range
        Collider[] hits = Physics.OverlapSphere(casterPosition, range, unitLayer);
        foreach (var hit in hits)
        {
            UnitController unit = hit.GetComponent<UnitController>();
            if (unit == null) continue;
            Vector3 directionToTarget = (unit.transform.position - casterPosition).normalized;
            float angleToTarget = Vector3.Angle(forward, directionToTarget);
            if (angleToTarget <= angle * 0.5f)
            {
                collected.Add(unit);
            }
        }

        var filtered = ApplyCommonFilters(castContext, collected);

#if UNITY_EDITOR
        SkillEffectTargetConeDebugDrawer drawer = castContext.caster.GetComponent<SkillEffectTargetConeDebugDrawer>();
        if (drawer == null)
        {
            drawer = castContext.caster.gameObject.AddComponent<SkillEffectTargetConeDebugDrawer>();
        }

        drawer.origin = casterPosition;
        drawer.forward = forward;
        drawer.range = range;
        drawer.angle = angle;
        drawer.draw = true;
#endif

        return new List<UnitController>(filtered);
    }
}