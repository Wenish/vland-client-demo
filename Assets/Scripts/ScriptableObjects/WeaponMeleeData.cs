using System.Collections.Generic;
using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "NewWeaponMelee", menuName = "Game/Weapon/Melee")]
public class WeaponMeleeData : WeaponData
{
    [Header("Meele Specific")]
    public Mode mode = Mode.Linear;
    public float coneAngleRadians = 90f;
    public int numRays = 21;
    public float weighting = 0.5f;

    public override void PerformAttack(UnitController attacker)
    {
        Vector3 unitPosition = attacker.transform.position + Vector3.up;
        Quaternion unitRotation = attacker.transform.rotation;

        
        // List to store the enemies hit by the attack cone
        List<UnitController> enemiesHit = new List<UnitController>();

                // Cast rays in a cone shape to detect enemies
        for (int i = 0; i < numRays; i++)
        {
            // Calculate the angle of the current ray
            float angle = -coneAngleRadians + (coneAngleRadians * 2.0f / (numRays - 1) * i);

            // Rotate the direction vector by the angle
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * unitRotation * Vector3.forward;

            // Use a raycast to detect if there is an enemy within the attack range and in the direction of the attack
            RaycastHit hit;
            if (Physics.Raycast(unitPosition, direction, out hit, attackRange))
            {
                // If the raycast hits an enemy, add the enemy to the list
                UnitController enemy = hit.collider.GetComponent<UnitController>();
                if (enemy != null && !enemy.IsDead && enemy.team != attacker.team)
                {
                    enemiesHit.Add(enemy);
                }
            }
        }

                // If multiple enemies were hit, choose the one that is most in the center of the cone
        if (enemiesHit.Count > 1)
        {
            UnitController closestEnemy = enemiesHit[0];
            float bestScore = 1;
            foreach (UnitController enemy in enemiesHit)
            {
                float score = calcScore(weighting, attacker.transform, enemy.transform.position, mode);
                if (score < bestScore)
                {
                    closestEnemy = enemy;
                    bestScore = score;
                }
            }
            // Deal damage to the closest enemy
            closestEnemy.TakeDamage(attackPower, attacker);
        }
        // If only one enemy was hit, deal damage to that enemy
        else if (enemiesHit.Count == 1)
        {
            var enemy = enemiesHit[0];
            enemy.TakeDamage(attackPower, attacker);
        }
    }

    public enum Mode { Linear, Quadratic }

    private float calcScore(float weighting, Transform unit, Vector3 target, Mode mode)
    {
        float angleNormalized = Mathf.Clamp(Vector3.Angle(unit.rotation * Vector3.forward, target - unit.position), 0, coneAngleRadians) / coneAngleRadians;
        float distanceNormalized = Mathf.Clamp(Vector3.Distance(unit.position, target), 0, attackRange) / attackRange;
        if (mode == Mode.Linear) {
            float score = (weighting * distanceNormalized) + ((1 - weighting) * angleNormalized);
            return score;
        }

        if (mode == Mode.Quadratic) {
            float score = (weighting * distanceNormalized * distanceNormalized) + ((1 - weighting) * angleNormalized * angleNormalized);
            return score;
        }
        return -1;
    }
}