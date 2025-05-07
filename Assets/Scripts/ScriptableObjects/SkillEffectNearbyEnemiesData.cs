using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSkillEffect", menuName = "Game/Skills/Effects/Target/Nearby Enemies")]
public class SkillEffektNearbyEnemiesData : SkillEffectData
{
    public float range = 5f;

    public override List<UnitController> Execute(UnitController caster, List<UnitController> targets)
    {
        List<UnitController> result = new List<UnitController>();
        Collider[] hits = Physics.OverlapSphere(caster.transform.position, range);
        foreach (var hit in hits)
        {
            if (hit.gameObject.CompareTag("Enemy"))
            {
                var unitController = hit.GetComponent<UnitController>();
                if (unitController == null)
                {
                    Debug.LogWarning($"Hit object {hit.name} does not have a UnitController component.");
                    continue;
                }
                result.Add(unitController);
            }
        }
        return result;
    }
}