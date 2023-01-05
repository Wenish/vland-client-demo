using System.Collections.Generic;
using UnityEngine;

public class WeaponMelee : Weapon
{
    [SerializeField]
    private float coneAngleRadians = 90f;
    [SerializeField]
    private int numRays = 20;
    protected override void PerformAttack(GameObject unit)
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
                if (enemy != null)
                {
                    enemiesHit.Add(enemy);
                }
            }
            
            Debug.Log(hit.point);
        }

        Debug.Log(enemiesHit.Count);

        // If multiple enemies were hit, choose the one that is most in the center of the cone
        if (enemiesHit.Count > 1)
        {
            UnitController closestEnemy = enemiesHit[0];
            float closestAngle = Vector3.Angle(unitRotation * Vector3.forward, closestEnemy.transform.position - unitPosition);
            foreach (UnitController enemy in enemiesHit)
            {
                float angle = Vector3.Angle(unitRotation * Vector3.forward, enemy.transform.position - unitPosition);
                if (angle < closestAngle)
                {
                    closestEnemy = enemy;
                    closestAngle = angle;
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
            Debug.Log(direction);
            Gizmos.DrawRay(unitPosition, direction * this.attackRange);
        }
    }
}