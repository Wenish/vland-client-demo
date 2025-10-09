using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillEffectTargetLinear", menuName = "Game/Skills/Effects/Target/Linear")]
public class SkillEffectTargetLinear : SkillEffectTarget
{
    public float range = 5f;
    public float width = 1f;
    public LayerMask unitLayer;


    public override List<UnitController> GetTargets(CastContext castContext, List<UnitController> targets)
    {
        List<UnitController> collected = new List<UnitController>();
        Vector3 origin = castContext.caster.transform.position + Vector3.up;
        Vector3 direction = castContext.caster.transform.forward;

        RaycastHit[] hits = Physics.SphereCastAll(origin, width / 2f, direction, range, unitLayer);

        foreach (RaycastHit hit in hits)
        {
            UnitController targetController = hit.collider.GetComponentInParent<UnitController>();
            if (targetController == null) continue;
            collected.Add(targetController);
        }

        var filtered = ApplyCommonFilters(castContext, collected);

#if UNITY_EDITOR
        SkillEffectTargetLinearDebugDrawer drawer = castContext.caster.GetComponent<SkillEffectTargetLinearDebugDrawer>();
        if (drawer == null)
        {
            drawer = castContext.caster.gameObject.AddComponent<SkillEffectTargetLinearDebugDrawer>();
        }

        drawer.origin = origin; // Adjust to ground level
        drawer.direction = direction;
        drawer.range = range;
        drawer.width = width;
        drawer.draw = true;
#endif

        return new List<UnitController>(filtered);
    }
}