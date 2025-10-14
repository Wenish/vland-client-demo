using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillEffectTargetCircle", menuName = "Game/Skills/Effects/Target/Circle")]
public class SkillEffectTargetCircle : SkillEffectTarget
{
    public float radius;
    public bool includeInitialTargets = true;
    public LayerMask unitLayer;

    public override List<UnitController> GetTargets(CastContext castContext, List<UnitController> targets)
    {
        List<UnitController> collected = new List<UnitController>();

        if (includeInitialTargets)
        {
            collected.AddRange(targets);
        }


        foreach (var target in targets)
        {
            if (target == null) continue;

            var hits = Physics.OverlapSphere(target.transform.position, radius, unitLayer);
            foreach (var hit in hits)
            {
                var unit = hit.GetComponentInParent<UnitController>();
                if (unit == null) continue;
                if (unit == target) continue;
                if (collected.Contains(unit)) continue;
                collected.Add(unit);
            }

#if UNITY_EDITOR
            SkillEffectTargetConeDebugDrawer drawer = castContext.caster.GetComponent<SkillEffectTargetConeDebugDrawer>();
            if (drawer == null)
            {
                drawer = castContext.caster.gameObject.AddComponent<SkillEffectTargetConeDebugDrawer>();
            }

            drawer.origin = target.transform.position;
            drawer.forward = target.transform.forward;
            drawer.range = radius;
            drawer.angle = 360f;
            drawer.draw = true;
#endif
        }

        var filtered = ApplyCommonFilters(castContext, collected);



        return new List<UnitController>(filtered);
    }
}