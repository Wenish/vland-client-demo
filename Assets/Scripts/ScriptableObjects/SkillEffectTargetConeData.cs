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
        List<UnitController> result = new List<UnitController>();


        int casterTeam = castContext.caster.team;
        Vector3 casterPosition = castContext.caster.transform.position;
        Vector3 forward = castContext.caster.transform.forward;

        // Get all colliders in range
        Collider[] hits = Physics.OverlapSphere(casterPosition, range, unitLayer);
        foreach (var hit in hits)
        {
            GameObject obj = hit.gameObject;
            if (obj == castContext.caster) continue;

            UnitController unit = obj.GetComponent<UnitController>();
            if (unit == null) continue;
            if (unit.IsDead) continue;

            // Check if on enemy team
            if (unit.team == casterTeam) continue;

            // Check if within angle
            Vector3 directionToTarget = (unit.transform.position - casterPosition).normalized;
            float angleToTarget = Vector3.Angle(forward, directionToTarget);
            if (angleToTarget <= angle * 0.5f)
            {
                result.Add(unit);
            }
        }

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

        return result;
    }
}