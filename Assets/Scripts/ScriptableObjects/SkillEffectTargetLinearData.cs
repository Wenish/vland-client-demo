using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSkillEffect", menuName = "Game/Skills/Effects/Target/Linear")]
public class SkillEffectTargetLinear : SkillEffectData
{
    public float range = 5f;
    public float width = 1f;
    public LayerMask unitLayer;


    public override List<GameObject> Execute(GameObject caster, List<GameObject> targets)
    {
        List<GameObject> result = new List<GameObject>();

        if (caster == null) return result;

        UnitController casterController = caster.GetComponent<UnitController>();
        if (casterController == null) return result;

        Vector3 origin = caster.transform.position + Vector3.up;
        Vector3 direction = caster.transform.forward;

        RaycastHit[] hits = Physics.SphereCastAll(origin, width / 2f, direction, range, unitLayer);

        foreach (RaycastHit hit in hits)
        {
            UnitController targetController = hit.collider.GetComponentInParent<UnitController>();

            if (targetController != null &&
                targetController != casterController &&
                !targetController.IsDead &&
                targetController.team != casterController.team)
            {
                if (!result.Contains(targetController.gameObject))
                {
                    result.Add(targetController.gameObject);
                }
            }
        }

#if UNITY_EDITOR
        SkillEffectTargetLinearDebugDrawer drawer = caster.GetComponent<SkillEffectTargetLinearDebugDrawer>();
        if (drawer == null)
        {
            drawer = caster.AddComponent<SkillEffectTargetLinearDebugDrawer>();
        }

        drawer.origin = origin;
        drawer.direction = direction;
        drawer.range = range;
        drawer.width = width;
        drawer.draw = true;
#endif

        return result;
    }
}