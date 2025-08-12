using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Mirror; // For NetworkServer.active check
using MyGame.Events;

public class AiZombieController : MonoBehaviour
{
    [Header("References")] public UnitController _unitController;

    [Header("Runtime Debug")] public Vector3 Destination; // current target destination (player position)
    public Vector3 _moveTarget; // next path corner we move towards (flattened Y)
    public bool _hasMoveTarget;
    public UnitController _targetPlayer;

    [Header("Tuning")] [SerializeField] private float retargetInterval = 0.5f; // seconds between checking nearest player
    [SerializeField] private float pathRecalcInterval = 0.25f; // seconds between navmesh path recalcs
    [SerializeField] private float stopDistance = 1.1f; // distance along path at which we stop moving
    [SerializeField] private bool aggroOnDamage = true; // switch target to attacker on damage

    private readonly List<UnitController> _playerUnits = new();
    private NavMeshPath _path;
    private float _nextRetargetTime;
    private float _nextPathCalcTime;

    void Awake()
    {
        _unitController = GetComponent<UnitController>();
    }

    void OnEnable()
    {
        // Subscribe to game-wide events to maintain player list & aggro
        EventManager.Instance.Subscribe<MyPlayerUnitSpawnedEvent>(OnPlayerSpawned);
        EventManager.Instance.Subscribe<UnitDiedEvent>(OnUnitDied);
        EventManager.Instance.Subscribe<UnitDamagedEvent>(OnUnitDamaged);
    }

    void OnDisable()
    {
        if (EventManager.Instance == null) return; // during shutdown
        EventManager.Instance.Unsubscribe<MyPlayerUnitSpawnedEvent>(OnPlayerSpawned);
        EventManager.Instance.Unsubscribe<UnitDiedEvent>(OnUnitDied);
        EventManager.Instance.Unsubscribe<UnitDamagedEvent>(OnUnitDamaged);
    }

    void Start()
    {
        _path = new NavMeshPath();
        SeedExistingPlayers(); // In case players spawned before this zombie
        _nextRetargetTime = Time.time + Random.Range(0f, retargetInterval); // small stagger
        _nextPathCalcTime = Time.time + Random.Range(0f, pathRecalcInterval);
    }

    // We keep Update very light; all heavy ops gated by timers & server check
    void Update()
    {
        if (!NetworkServer.active) return; // Only the server drives AI state
        if (_unitController.IsDead) return;

        TryRetarget();
        TryRecalculatePath();
        UpdateMoveTarget();
        ApplyMovementInputs();
        TryAttack();
    }

    #region Event Handlers
    void OnPlayerSpawned(MyPlayerUnitSpawnedEvent e)
    {
        // Only track units on server; client spawns are irrelevant for AI decisions here
        if (!NetworkServer.active) return;
        if (e.PlayerCharacter != null && !e.PlayerCharacter.IsDead && !_playerUnits.Contains(e.PlayerCharacter))
        {
            _playerUnits.Add(e.PlayerCharacter);
        }
    }

    void OnUnitDied(UnitDiedEvent e)
    {
        if (!NetworkServer.active) return;
        if (e.Unit != null && e.Unit.unitType == UnitType.Player)
        {
            _playerUnits.Remove(e.Unit);
            if (_targetPlayer == e.Unit)
            {
                _targetPlayer = null;
            }
        }
    }

    void OnUnitDamaged(UnitDamagedEvent e)
    {
        if (!NetworkServer.active) return;
        if (!aggroOnDamage) return;
        if (e.Unit == _unitController && e.Attacker != null && e.Attacker.unitType == UnitType.Player && !e.Attacker.IsDead)
        {
            // Switch target to attacker immediately for responsiveness
            _targetPlayer = e.Attacker;
            Destination = _targetPlayer.transform.position;
            _nextRetargetTime = Time.time + retargetInterval; // delay next retarget to stick briefly
            _nextPathCalcTime = Time.time; // force path recalculation next frame
        }
    }
    #endregion

    #region Targeting & Path
    void SeedExistingPlayers()
    {
        // One-off initial fetch to cover players already present
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var pc in players)
        {
            if (pc && pc.Unit)
            {
                var uc = pc.Unit.GetComponent<UnitController>();
                if (uc && uc.unitType == UnitType.Player && !uc.IsDead && !_playerUnits.Contains(uc))
                {
                    _playerUnits.Add(uc);
                }
            }
        }
    }

    void TryRetarget()
    {
        if (Time.time < _nextRetargetTime) return;
        _nextRetargetTime = Time.time + retargetInterval;

        // Clean dead entries
        for (int i = _playerUnits.Count - 1; i >= 0; i--)
        {
            if (_playerUnits[i] == null || _playerUnits[i].IsDead)
                _playerUnits.RemoveAt(i);
        }
        if (_playerUnits.Count == 0)
        {
            _targetPlayer = null;
            StopMoveInput();
            return;
        }

        // If current target still valid, keep it (sticky targeting) unless far away & another is closer
        UnitController best = _targetPlayer != null && !_targetPlayer.IsDead ? _targetPlayer : _playerUnits[0];
        float bestDistSqr = Vector3.SqrMagnitude(best.transform.position - _unitController.transform.position);
        foreach (var candidate in _playerUnits)
        {
            if (candidate == null || candidate.IsDead) continue;
            float distSqr = Vector3.SqrMagnitude(candidate.transform.position - _unitController.transform.position);
            if (distSqr < bestDistSqr * 0.95f) // small hysteresis
            {
                bestDistSqr = distSqr;
                best = candidate;
            }
        }
        _targetPlayer = best;
        Destination = _targetPlayer.transform.position;
    }

    void TryRecalculatePath()
    {
        if (Time.time < _nextPathCalcTime) return;
        _nextPathCalcTime = Time.time + pathRecalcInterval;
        if (_targetPlayer == null) return;
        Destination = _targetPlayer.transform.position; // refresh moving target position
        NavMesh.CalculatePath(transform.position, Destination, 1, _path);
    }

    void UpdateMoveTarget()
    {
        _hasMoveTarget = false;
        if (_path == null || _path.corners == null || _path.corners.Length == 0) return;
        // If we have at least 2 corners, use the next corner after current position; else use destination itself.
        Vector3 nextCorner;
        if (_path.corners.Length >= 2)
        {
            nextCorner = _path.corners[1];
        }
        else
        {
            nextCorner = _path.corners[0];
        }
        _moveTarget = new Vector3(nextCorner.x, 0f, nextCorner.z);
        _hasMoveTarget = true;
    }
    #endregion

    #region Movement & Combat
    void ApplyMovementInputs()
    {
        if (!_hasMoveTarget)
        {
            StopMoveInput();
            return;
        }
        float remainingPath = GetPathDistance(_path);
        if (remainingPath < stopDistance)
        {
            StopMoveInput();
            return;
        }
        Vector3 direction = _moveTarget - _unitController.transform.position;
        direction.y = 0f;
        direction.Normalize();
        _unitController.horizontalInput = direction.x;
        _unitController.verticalInput = direction.z;

        // Set facing angle towards movement target (could also face player directly if preferred)
        Vector3 pos = _unitController.transform.position - _moveTarget;
        var angle = -(Mathf.Atan2(pos.z, pos.x) * Mathf.Rad2Deg) - 90f;
        _unitController.angle = angle;
    }

    void StopMoveInput()
    {
        _unitController.horizontalInput = 0f;
        _unitController.verticalInput = 0f;
    }

    void TryAttack()
    {
        if (_targetPlayer == null || _targetPlayer.IsDead) return;
        if (_unitController.currentWeapon == null) return;
        float dist = Vector3.Distance(_unitController.transform.position, _targetPlayer.transform.position);
        if (dist <= _unitController.currentWeapon.attackRange)
        {
            _unitController.Attack();
        }
    }
    #endregion

    #region Helpers
    private float GetPathDistance(NavMeshPath path)
    {
        if (path == null || path.corners == null || path.corners.Length < 2) return 0f;
        float distance = 0f;
        for (int i = 1; i < path.corners.Length; i++)
        {
            distance += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }
        return distance;
    }
    #endregion
}
