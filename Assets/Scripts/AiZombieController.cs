using UnityEngine;
using UnityEngine.AI;

public class AiZombieController : MonoBehaviour
{
    // Start is called before the first frame update
    public NavMeshAgent _navMeshAgent;
    public UnitController _unitController;
    Vector3 Destination;
    [SerializeField]
    Vector3? _moveTarget;

    void Awake()
    {
        _unitController = gameObject.GetComponent<UnitController>();
        _navMeshAgent = gameObject.AddComponent<NavMeshAgent>();
        _navMeshAgent.isStopped = true; 
        _navMeshAgent.autoBraking = false;
        _navMeshAgent.angularSpeed = 0;
        _navMeshAgent.speed = 0;
        _navMeshAgent.acceleration = 0;
    }
    void Start()
    {
        _unitController.OnDied += HandleOnDied;
        _unitController.OnDied += HandleOnRevive;
        SetDestination(Vector3.zero);
        Destination = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if(_unitController.IsDead) return;
        SetMoveTarget();
        CalculateAngle();
        CalculateMoveInput();
    }
    void HandleOnDied()
    {
        _navMeshAgent.enabled = false;
    }
    void HandleOnRevive()
    {
        _navMeshAgent.enabled = true;
    }
    public void SetDestination(Vector3 destination)
    {
        _navMeshAgent.SetDestination(destination);
    }

    void SetMoveTarget()
    {
        if (_navMeshAgent.hasPath && _navMeshAgent.path.corners.Length > 0) {
            var nextCorner = _navMeshAgent.path.corners[1];
            _moveTarget = new Vector3(nextCorner.x, 0, nextCorner.z);
        } else {
            _moveTarget = null;
        }
    }

    void CalculateAngle()
    {
        if (_moveTarget == null) return;

        Vector3 pos = (Vector3)(_unitController.transform.position - _moveTarget);
        var angle = -(Mathf.Atan2(pos.z, pos.x) * Mathf.Rad2Deg) - 90;
        _unitController.angle = angle;
    }

    void CalculateMoveInput()
    {
        if (_moveTarget == null) {
            StopMoveInput();
            return;
        };
        
        float distance = Vector3.Distance(_unitController.transform.position, _navMeshAgent.destination);

        if (distance < 1f) {
            StopMoveInput();
            return;
        }

        Vector3 direction = (Vector3)(_moveTarget - _unitController.transform.position);
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
}
