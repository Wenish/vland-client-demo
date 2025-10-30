
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillEffectMechanicProjectile", menuName = "Game/Skills/Effects/Mechanic/Projectile")]
public class SkillEffectMechanicProjectile : SkillEffectMechanic
{
    public ProjectileData projectileData;

    public float spawnDistance = 0f;

    public SkillEffectChainData onHitEffectChain;

    public override List<UnitController> DoMechanic(CastContext castContext, List<UnitController> targets)
    {
        foreach (var target in targets)
        {
            SpawnProjectile(castContext, target);
        }
        return targets;
    }

    private void SpawnProjectile(CastContext castContext, UnitController target)
    {
        if (projectileData == null)
        {
            Debug.LogWarning("ProjectileData is null. Cannot spawn projectile.");
            return;
        }

        Vector3 targetPosition = target.transform.position;
        Quaternion targetRotation = target.transform.rotation;

        Vector3 spawnPosition = targetPosition + targetRotation * Vector3.forward * spawnDistance;

        var projectileInstance = ProjectileSpawner.Instance.SpawnProjectile(projectileData, spawnPosition + Vector3.up, targetRotation);

        ProjectileController projectileController = projectileInstance.GetComponent<ProjectileController>();
        projectileController.shooter = target;

        projectileController.OnProjectileUnitHit += (hitInfo) =>
        {
            if (onHitEffectChain != null)
            {
                var hitTargets = new List<UnitController> { hitInfo.target };
                var coroutine = onHitEffectChain.ExecuteCoroutine(castContext, hitTargets);

                if (coroutine != null && hitInfo.target is MonoBehaviour mb)
                {
                    mb.StartCoroutine(coroutine);
                }
            }
        };

        projectileController.OnProjectileDestroyed += (proj) =>
        {
            proj.OnProjectileUnitHit -= (hitInfo) => { };
            proj.OnProjectileDestroyed -= (p) => { };
        };
    }
}