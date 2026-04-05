using System.Collections.Generic;
using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "NewWeaponMelee", menuName = "Game/Weapon/Melee")]
public class WeaponMeleeData : WeaponData
{
    [Header("Meele Specific")]
    public Mode mode = Mode.Linear;
    public float maxHits = 1;
    public float coneAngleRadians = 90f;
    public int numRays = 21;
    public float weighting = 0.5f;

    public override void PerformAttack(UnitController attacker)
    {
        Vector3 unitPosition = attacker.transform.position;
        Vector3 attackerForwardXZ = Vector3.ProjectOnPlane(attacker.transform.forward, Vector3.up);
        if (attackerForwardXZ.sqrMagnitude < 1e-6f)
        {
            attackerForwardXZ = Vector3.forward;
        }
        attackerForwardXZ.Normalize();


        // List to store the enemies hit by the attack cone
        List<UnitController> enemiesHit = new List<UnitController>();

        // Use an overlap sphere for broad phase, then apply cone/range checks on XZ only.
        // Add vertical padding so airborne knockup targets are still included in broad phase.
        const float airborneDetectionHeightPadding = 5f;
        float broadPhaseRadius = Mathf.Sqrt((attackRange * attackRange) + (airborneDetectionHeightPadding * airborneDetectionHeightPadding));
        Collider[] colliders = Physics.OverlapSphere(unitPosition, broadPhaseRadius);
        foreach (var collider in colliders)
        {
            UnitController enemy = collider.GetComponentInParent<UnitController>();
            if (enemy == null || enemy == attacker || enemy.IsDead || enemy.team == attacker.team)
            {
                continue;
            }

            Vector3 toEnemy = enemy.transform.position - unitPosition;
            Vector3 toEnemyXZ = Vector3.ProjectOnPlane(toEnemy, Vector3.up);
            float planarDistance = toEnemyXZ.magnitude;

            if (planarDistance > attackRange)
            {
                continue;
            }

            if (planarDistance > 1e-6f)
            {
                float angleToEnemy = Vector3.Angle(attackerForwardXZ, toEnemyXZ);
                if (angleToEnemy > coneAngleRadians)
                {
                    continue;
                }
            }

            if (!enemiesHit.Contains(enemy))
            {
                enemiesHit.Add(enemy);
            }
        }

        var hasHitAnyEnemy = enemiesHit.Count > 0;
        if (!hasHitAnyEnemy) return;

        var damage = CalculateDamage(attacker);

        // If only one enemy was hit, deal damage to that enemy


        if (enemiesHit.Count > 1)
        {
            // Sort enemies by their score
            enemiesHit.Sort((a, b) =>
            {
                float scoreA = calcScore(weighting, attacker.transform, a.transform.position, mode);
                float scoreB = calcScore(weighting, attacker.transform, b.transform.position, mode);
                return scoreA.CompareTo(scoreB);
            });

            // Hit as many enemies as maxHits
            for (int i = 0; i < Mathf.Min(maxHits, enemiesHit.Count); i++)
            {
                enemiesHit[i].TakeDamage(DamageInstance.Physical(damage, DamageSourceKind.BasicAttack), attacker);
                enemiesHit[i].RaiseOnAttackHitReceivedEvent(attacker);
            }

        }
        else if (enemiesHit.Count == 1)
        {
            var enemy = enemiesHit[0];
            enemy.TakeDamage(DamageInstance.Physical(damage, DamageSourceKind.BasicAttack), attacker);
            enemy.RaiseOnAttackHitReceivedEvent(attacker);
            return;
        }
    }

    public enum Mode { Linear, Quadratic }

    private float calcScore(float weighting, Transform unit, Vector3 target, Mode mode)
    {
        float angleNormalized = Mathf.Clamp(Vector3.Angle(unit.rotation * Vector3.forward, target - unit.position), 0, coneAngleRadians) / coneAngleRadians;
        float distanceNormalized = Mathf.Clamp(Vector3.Distance(unit.position, target), 0, attackRange) / attackRange;
        if (mode == Mode.Linear)
        {
            float score = (weighting * distanceNormalized) + ((1 - weighting) * angleNormalized);
            return score;
        }

        if (mode == Mode.Quadratic)
        {
            float score = (weighting * distanceNormalized * distanceNormalized) + ((1 - weighting) * angleNormalized * angleNormalized);
            return score;
        }
        return -1;
    }
}