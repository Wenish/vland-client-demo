using System.Collections.Generic;
using UnityEngine;

public class WeaponMelee : Weapon
{
    public Mode mode = Mode.Linear;
    [SerializeField]
    private float coneAngleRadians = 90f;
    [SerializeField]
    private int numRays = 21;
    [SerializeField]
    private float Weighting = 0.5f;
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
                float scrore = calcScore(Weighting, unit.transform, enemy.transform.position, mode);
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

        var height = attackRange * 2;
        var width = attackRange * 2;

        for (int w = 0; w < width; w++)
        {
            for(int h = 0; h < height; h++)
            {
                Vector3 target = new Vector3(w - attackRange + 0.5f, 0, h - attackRange + 0.5f) + transform.position;
                var score = calcScore(Weighting, transform, target, mode);
                Gizmos.color = new Color(score, score, 0);
                if (score > 0.7f) {
                    Gizmos.color = Color.white;
                }
                Gizmos.DrawSphere(target, 0.03f);
            }
            
        }
    }
}