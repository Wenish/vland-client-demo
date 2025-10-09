using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillEffectMechanicDash", menuName = "Game/Skills/Effects/Mechanic/Dash")]
public class SkillEffectMechanicDash : SkillEffectMechanic
{
    public enum DashDirection
    {
        MovementDirection,
        FacingDirection
    }
    public DashDirection dashDirection = DashDirection.MovementDirection;
    
    [Tooltip("Distance to dash forward")]
    public float dashDistance = 5f;
    [Tooltip("Speed applied forward to perform the dash (mass-independent if using VelocityChange)")]
    public float dashSpeed = 20f;
    public override List<UnitController> DoMechanic(
        CastContext castContext,
        List<UnitController> targets)
    {
        Debug.Log($"Dashing {castContext.caster.name} forward with speed {dashSpeed}. Distance: {dashDistance}");
        var newTargetList = new List<UnitController> { castContext.caster };

        Vector2 dashDir = Vector2.zero;
        if (dashDirection == DashDirection.MovementDirection)
        {
            dashDir = new Vector2(castContext.caster.horizontalInput, castContext.caster.verticalInput).normalized;
            if (dashDir == Vector2.zero)
            {
                // If not moving, dash in facing direction instead
                dashDir = new Vector2(castContext.caster.transform.forward.x, castContext.caster.transform.forward.z);
            }
        }

        if (dashDirection == DashDirection.FacingDirection)
        {
            dashDir = new Vector2(castContext.caster.transform.forward.x, castContext.caster.transform.forward.z);
        }
        
        var dir3 = new Vector3(dashDir.x, 0f, dashDir.y);
        castContext.caster.StartDash(dir3, dashSpeed, dashDistance);

        return newTargetList;
    }
}