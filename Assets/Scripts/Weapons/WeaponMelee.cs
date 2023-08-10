using System.Collections.Generic;
using UnityEngine;

public class WeaponMelee : Weapon
{
    public Mode mode = Mode.Linear;
    public float coneAngleRadians = 90f;
    public int numRays = 21;
    public float Weighting = 0.5f;
    protected override void PerformAttack(UnitController unit)
    {
        // Get the position and rotation of the unit
        Vector3 unitPosition = unit.transform.position + Vector3.up;
        Quaternion unitRotation = unit.transform.rotation;


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
                if (enemy != null && !enemy.IsDead)
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
                float score = calcScore(Weighting, unit.transform, enemy.transform.position, mode);
                if (score < bestScore)
                {
                    closestEnemy = enemy;
                    bestScore = score;
                }
            }
            // Deal damage to the closest enemy
            closestEnemy.TakeDamage(attackPower, unit);
        }
        // If only one enemy was hit, deal damage to that enemy
        else if (enemiesHit.Count == 1)
        {
            var enemy = enemiesHit[0];
            enemy.TakeDamage(attackPower, unit);
        }
    }
    
    public enum Mode {
        Quadratic,
        Linear
    }

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

    void OnDrawGizmos()
    {
        // Set the color of the gizmo
        Gizmos.color = Color.red;


        Vector3 unitPosition = transform.position + Vector3.up;
        Quaternion unitRotation = transform.rotation;

        // Cast rays in a cone shape to detect enemies
        for (int i = 0; i < numRays; i++)
        {
            // Calculate the angle of the current ray
            float angle = -coneAngleRadians + (coneAngleRadians * 2.0f / (numRays - 1) * i);

            // Rotate the direction vector by the angle
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * unitRotation * Vector3.forward;
            Gizmos.DrawRay(unitPosition, direction * this.attackRange);
        }
    }
}