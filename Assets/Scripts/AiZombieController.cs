using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class AiZombieController : MonoBehaviour
{
    public UnitController _unitController;
    public Vector3 Destination;
    public Vector3 _moveTarget;
    private NavMeshPath _path;
    private PlayerController[] _playerControllers;

    void Awake()
    {
        _unitController = gameObject.GetComponent<UnitController>();
    }
    void Start()
    {
        _path = new NavMeshPath();
        SetDestination(Vector3.zero);
    }

    // Update is called once per frame
    void Update()
    {
        if(_unitController.IsDead) return;
        // TODO: dont call GetAllPlayerControllers on every update (listen on event OnPlayerAdded)
        GetAllPlayerControllers();
        CalcNearestPlayer();
        CalcPathToDestination();
        SetMoveTarget();
        CalculateAngle();
        CalculateMoveInput();
    }

    void GetAllPlayerControllers()
    {
        _playerControllers = FindObjectsOfType<PlayerController>();
    }
    void CalcNearestPlayer()
    {
        Vector3[] positions = _playerControllers
            .Where(x => !x.Unit.GetComponent<UnitController>().IsDead)
            .Select(x => x.Unit.transform.position)
            .ToArray();
        var nearestPlayerPosition = GetNearestPlayerPosition(positions, _unitController.transform.position);
        SetDestination(nearestPlayerPosition);
    }
    public void SetDestination(Vector3 destination)
    {
        Destination = destination;
    }

    void CalcPathToDestination()
    {
        NavMesh.CalculatePath(transform.position, Destination, 1, _path);
    }

    void SetMoveTarget()
    {
        if (_path.corners.Length > 0) {
            var nextCorner = _path.corners[1];
            _moveTarget = new Vector3(nextCorner.x, 0, nextCorner.z);
        }
    }

    void CalculateAngle()
    {
        if (_moveTarget == null) return;

        var pos = _unitController.transform.position - _moveTarget;
        var angle = -(Mathf.Atan2(pos.z, pos.x) * Mathf.Rad2Deg) - 90;
        _unitController.angle = angle;
    }

    void CalculateMoveInput()
    {
        if (_moveTarget == null) {
            StopMoveInput();
            return;
        };
        
        float distance = GetPathDistance(_path);

        if (distance < 1f) {
            StopMoveInput();
            return;
        }

        Vector3 direction = _moveTarget - _unitController.transform.position;
        direction.y = 0f;
        direction.Normalize();
        _unitController.horizontalInput = direction.x;
        _unitController.verticalInput = direction.z;
    }

    void StopMoveInput()
    {
        _unitController.horizontalInput = 0;
        _unitController.verticalInput = 0;
    }

    private float GetPathDistance(NavMeshPath path)
    {
        if (path != null && path.corners.Length > 1)
        {
            float distance = 0f;
            for (int i = 1; i < path.corners.Length; i++)
            {
                distance += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            }
            return distance;
        }
        return 0f; // If the path is null or has no corners, return 0 distance.
    }

    private Vector3 GetNearestPlayerPosition(Vector3[] playerPositions, Vector3 referencePosition)
    {
        if (playerPositions == null || playerPositions.Length == 0)
        {
            Debug.LogWarning("Player positions array is null or empty.");
            return Vector3.zero;
        }

        Vector3 nearestPlayerPosition = playerPositions[0];
        float nearestDistanceSqr = Vector3.SqrMagnitude(nearestPlayerPosition - referencePosition);

        for (int i = 1; i < playerPositions.Length; i++)
        {
            float distanceSqr = Vector3.SqrMagnitude(playerPositions[i] - referencePosition);
            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearestPlayerPosition = playerPositions[i];
            }
        }

        return nearestPlayerPosition;
    }
}
