
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillEffectMechanicProjectile", menuName = "Game/Skills/Effects/Mechanic/Projectile")]
public class SkillEffectMechanicProjectile : SkillEffectMechanic
{
    [Required]
    [Expandable]
    public ProjectileData projectileData;

    [MinValue(0f)]
    public float spawnDistance = 0f;

    [Expandable]
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
        Quaternion spawnRotation = SkillAimUtil.ResolveSpawnRotation(castContext, target);

        Vector3 spawnPosition = targetPosition + spawnRotation * Vector3.forward * spawnDistance;

        var projectileInstance = castContext.ProjectileSpawner != null
            ? castContext.ProjectileSpawner.SpawnProjectile(projectileData, spawnPosition + Vector3.up, spawnRotation)
            : null;
        if (projectileInstance == null)
        {
            Debug.LogWarning("Projectile spawner is missing. Cannot spawn projectile.");
            return;
        }

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