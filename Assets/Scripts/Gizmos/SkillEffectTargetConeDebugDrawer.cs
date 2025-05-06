using UnityEngine;

[ExecuteAlways]
public class SkillEffectTargetConeDebugDrawer : MonoBehaviour
{
    public Vector3 origin;
    public Vector3 forward;
    public float range;
    public float angle;
    public bool draw = false;

    private void OnDrawGizmos()
    {
        if (!draw) return;

        Gizmos.color = Color.yellow;

        Vector3 leftDirection = Quaternion.Euler(0, -angle / 2f, 0) * forward.normalized;
        Vector3 rightDirection = Quaternion.Euler(0, angle / 2f, 0) * forward.normalized;

        Vector3 leftEnd = origin + leftDirection * range;
        Vector3 rightEnd = origin + rightDirection * range;

        // Draw arc edges
        Gizmos.DrawLine(origin, leftEnd);
        Gizmos.DrawLine(origin, rightEnd);

        // Optional: Draw arc curve as lines between points
        int segments = 20;
        float deltaAngle = angle / segments;
        Vector3 lastPoint = origin + Quaternion.Euler(0, -angle / 2f, 0) * forward.normalized * range;

        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = -angle / 2f + deltaAngle * i;
            Vector3 nextPoint = origin + Quaternion.Euler(0, currentAngle, 0) * forward.normalized * range;
            Gizmos.DrawLine(lastPoint, nextPoint);
            lastPoint = nextPoint;
        }
    }
}
