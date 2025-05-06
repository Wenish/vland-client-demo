using UnityEngine;

public class SkillEffectTargetLinearDebugDrawer : MonoBehaviour
{
    public Vector3 origin;
    public Vector3 direction;
    public float range;
    public float width;
    public bool draw = false;

    private void OnDrawGizmos()
    {
        if (!draw) return;

        Gizmos.color = Color.red;

        Vector3 forward = direction.normalized * range;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized * (width / 2f);

        Vector3 p1 = origin - right;
        Vector3 p2 = origin + right;
        Vector3 p3 = origin + forward + right;
        Vector3 p4 = origin + forward - right;

        Gizmos.DrawLine(p1, p2); // near edge
        Gizmos.DrawLine(p2, p3); // right edge
        Gizmos.DrawLine(p3, p4); // far edge
        Gizmos.DrawLine(p4, p1); // left edge
    }
}