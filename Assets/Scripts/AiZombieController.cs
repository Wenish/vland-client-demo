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
    private UnitController _targetPlayer;

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
        CalcShouldAttack();
    }

    void GetAllPlayerControllers()
    {
        _playerControllers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
    }
    void CalcNearestPlayer()
    {
        var playerUnits = _playerControllers
            .Where(x => x.Unit.GetComponent<UnitController>())
            .Where(x => !x.Unit.GetComponent<UnitController>().IsDead)
            .Select(x => x.Unit.GetComponent<UnitController>())
            .ToArray();
        if (playerUnits.Length == 0) return;
        
        var nearestPlayer = GetNearestPlayerPosition(playerUnits, _unitController.transform.position);
        SetDestination(nearestPlayer.transform.position);
        _targetPlayer = nearestPlayer;
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

        if (distance < 1.1f) {
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

    void CalcShouldAttack()
    {
        if (_targetPlayer && _targetPlayer.IsDead) return;

        var distance = Vector3.Distance(_unitController.transform.position, Destination);
        if (distance < _unitController.currentWeapon.attackRange) {
            _unitController.Attack();
        }
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

    private UnitController GetNearestPlayerPosition(UnitController[] playerUnits, Vector3 referencePosition)
    {
        if (playerUnits == null || playerUnits.Length == 0)
        {
            Debug.LogWarning("Player positions array is null or empty.");
        }

        UnitController nearestUnit = playerUnits[0];
        float nearestDistanceSqr = Vector3.SqrMagnitude(nearestUnit.transform.position - referencePosition);

        for (int i = 1; i < playerUnits.Length; i++)
        {
            float distanceSqr = Vector3.SqrMagnitude(playerUnits[i].transform.position - referencePosition);
            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearestUnit = playerUnits[i];
            }
        }

        return nearestUnit;
    }
}
