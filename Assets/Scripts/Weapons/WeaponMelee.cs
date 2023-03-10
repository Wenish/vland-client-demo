using System.Collections.Generic;
using UnityEngine;

public class WeaponMelee : Weapon
{
    [SerializeField]
    private float coneAngleRadians = 90f;
    [SerializeField]
    private int numRays = 21;
    protected override void PerformAttack(UnitController unit)
    {
        Debug.Log("PerformAttack Weapon");
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
                if (enemy != null && !enemy.isDead)
                {
                    Debug.Log(enemy.isDead);
                    enemiesHit.Add(enemy);
                }
            }
        }

        // If multiple enemies were hit, choose the one that is most in the center of the cone
        if (enemiesHit.Count > 1)
        {
            UnitController closestEnemy = enemiesHit[0];
            /*
            float closestAngle = Vector3.Angle(unitRotation * Vector3.forward, closestEnemy.transform.position - unitPosition);
            foreach (UnitController enemy in enemiesHit)
            {
                // (gewichtung * distance.normalize^2) + ((1 - gewichtung) * angle.normalized^2)
                float angle = Vector3.Angle(unitRotation * Vector3.forward, enemy.transform.position - unitPosition);
                Debug.Log(angle);
                if (angle < closestAngle)
                {
                    closestEnemy = enemy;
                    closestAngle = angle;
                }
            }
            */

            // (gewichtung * distance.normalize^2) + ((1 - gewichtung) * angle.normalized^2)
            float weighting = 0.5f;
            float bestScore = (weighting * 1) + ((1 - weighting) * 1);
            Debug.Log(bestScore);
            foreach (UnitController enemy in enemiesHit)
            {
                
                float angleNormalized = Mathf.Clamp(Vector3.Angle(unitRotation * Vector3.forward, enemy.transform.position - unitPosition), 0, coneAngleRadians) / coneAngleRadians;
                float distanceNormalized = Mathf.Clamp(Vector3.Distance(unitPosition, enemy.transform.position), 0, attackRange) / attackRange;
                float scrore = (weighting * distanceNormalized) + ((1 - weighting) * angleNormalized);
                if (scrore < bestScore)
                {
                    closestEnemy = enemy;
                    bestScore = scrore;
                }
            }

            // Deal damage to the closest enemy
            closestEnemy.TakeDamage(attackPower);
        }
        // If only one enemy was hit, deal damage to that enemy
        else if (enemiesHit.Count == 1)
        {
            enemiesHit[0].TakeDamage(attackPower);
        }
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