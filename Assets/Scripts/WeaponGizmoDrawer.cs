using UnityEngine;

#if UNITY_EDITOR
[ExecuteInEditMode]
#endif
public class WeaponGizmoDrawer : MonoBehaviour
{
    private WeaponData weaponData;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        var unitController = GetComponent<UnitController>();
        if (unitController == null) return;
        weaponData = unitController.currentWeapon;
        if (weaponData == null) return;

        Gizmos.color = Color.yellow;

        Vector3 origin = transform.position + Vector3.up;

        if (weaponData is WeaponMeleeData melee)
        {
            DrawMeleeGizmo(melee);
        }
        else if (weaponData is WeaponRangedData ranged)
        {
            DrawRangedGizmo(origin, transform.forward, ranged);
        }
    }

    void DrawMeleeGizmo(WeaponMeleeData melee)
    {
        // Set the color of the gizmo
        Gizmos.color = Color.red;


        Vector3 unitPosition = transform.position + Vector3.up;
        Quaternion unitRotation = transform.rotation;

        // Cast rays in a cone shape to detect enemies
        for (int i = 0; i < melee.numRays; i++)
        {
            // Calculate the angle of the current ray
            float angle = -melee.coneAngleRadians + (melee.coneAngleRadians * 2.0f / (melee.numRays - 1) * i);

            // Rotate the direction vector by the angle
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * unitRotation * Vector3.forward;
            Gizmos.DrawRay(unitPosition, direction * melee.attackRange);
        }
    }

    void DrawRangedGizmo(Vector3 origin, Vector3 direction, WeaponRangedData ranged)
    {
        var spawnDistance = ranged.spawnDistance;
        origin += direction * spawnDistance;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(origin, direction * ranged.projectile.range);

        Vector3 endPoint = origin + direction * ranged.projectile.range;
        Gizmos.DrawWireSphere(endPoint, 0.2f);
    }
#endif
}