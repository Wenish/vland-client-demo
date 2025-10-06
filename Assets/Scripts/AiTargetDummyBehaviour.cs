using Mirror;
using UnityEngine;

[RequireComponent(typeof(UnitController))]
public class AiTargetDummyBehavior : NetworkBehaviour
{
    private UnitController _unitController;

    [SerializeField] private float patrolDistance = 2f; // world units to each side
    [SerializeField] private float edgeTolerance = 0.05f; // hysteresis to avoid jitter on flip
    [SerializeField] private float returnTolerance = 0.1f; // how far off the patrol line before correcting back
    [SerializeField] private float returnStrength = 1.0f;   // weight of the homing component when slightly off
    [SerializeField] private float hardReturnDistance = 1.0f; // if pushed this far (perpendicular) -> ignore strafe and go straight back

    private Vector3 _spawnWorldPosition;
    private Vector3 _spawnRightWS;
    private float _spawnYaw;
    private int _strafeDirection = 1; // +1 = right, -1 = left

    // Initialize on the server when the object becomes active on the network
    public override void OnStartServer()
    {
        base.OnStartServer();

        _unitController = GetComponent<UnitController>();
        if (_unitController == null)
        {
            Debug.LogError("AiTargetDummyBehavior requires a UnitController component on the same GameObject.");
            enabled = false;
            return;
        }

        _spawnWorldPosition = transform.position;
        var spawnRotation = transform.rotation;
        _spawnYaw = spawnRotation.eulerAngles.y;
        _spawnRightWS = (spawnRotation * Vector3.right).normalized;

        // Lock the facing to the initial yaw so movement strafes relative to spawn direction
        _unitController.angle = _spawnYaw;
    }

    [ServerCallback]
    private void Update()
    {
        if (_unitController == null) return;

        // if unit is dead, do nothing
        if (_unitController.IsDead) {
            _unitController.horizontalInput = 0;
            _unitController.verticalInput = 0;
            return;
        }

        // Compute signed offset along initial right vector to detect edges
        Vector3 displacement = transform.position - _spawnWorldPosition;
        displacement.y = 0f;
        float offset = Vector3.Dot(displacement, _spawnRightWS);

        // Flip direction at patrol edges with hysteresis
        if (offset > patrolDistance + edgeTolerance)
        {
            _strafeDirection = -1;
        }
        else if (offset < -patrolDistance - edgeTolerance)
        {
            _strafeDirection = 1;
        }

        // Base desired world-space direction: strafe along initial right vector
        Vector3 desiredDir = _spawnRightWS * _strafeDirection;
        desiredDir.y = 0f;

        // Compute perpendicular (to patrol line) displacement on the XZ plane
        Vector3 parallel = _spawnRightWS * offset; // along patrol line
        Vector3 perpendicular = displacement - parallel; // off the patrol line
        perpendicular.y = 0f;

        // If pushed away from the patrol line, add a homing component back towards spawn line
        float perpMag = perpendicular.magnitude;
        if (perpMag > returnTolerance)
        {
            Vector3 correction = (-perpendicular).normalized; // toward spawn line

            // If very far, go straight back; otherwise blend with strafing for smoothness
            if (perpMag >= hardReturnDistance)
            {
                desiredDir = correction;
            }
            else
            {
                desiredDir = (desiredDir + correction * returnStrength);
            }
        }

        // Normalize to world-space input range
        Vector3 worldDir = desiredDir;
        worldDir.y = 0f;
        if (worldDir.sqrMagnitude > 1e-5f)
        {
            worldDir.Normalize();
        }

        // Map to UnitController world-space inputs (each clamped to [-1, 1])
        _unitController.horizontalInput = Mathf.Clamp(worldDir.x, -1f, 1f);
        _unitController.verticalInput = Mathf.Clamp(worldDir.z, -1f, 1f);

        // Keep facing locked to initial yaw
        _unitController.angle = _spawnYaw;
    }
}