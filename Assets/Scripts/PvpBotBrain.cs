using Mirror;
using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(UnitController))]
public class PvpBotBrain : MonoBehaviour
{
    [Header("Thinking")]
    [SerializeField, Min(0.05f)] private float thinkIntervalSeconds = 0.15f;
    [SerializeField, Min(0f)] private float retargetIntervalSeconds = 0.5f;
    [SerializeField, Min(0.1f)] private float eqsQueryIntervalSeconds = 0.5f;

    [Header("Line of Sight")]
    [SerializeField] private float losCheckDistance = 50f;
    [SerializeField] private int losLayerMask = -1;
    [SerializeField] private bool debugDrawLineOfSight = false;

    [Header("Movement - Melee")]
    [SerializeField, Min(0.1f)] private float meleeChaseDistance = 5f;
    [SerializeField, Min(0.1f)] private float meleeRetreatDistance = 1.5f;
    [SerializeField, Range(0f, 1f)] private float meleeStrafeStrength = 0.7f;

    [Header("Movement - Ranged")]
    [SerializeField, Min(0.1f)] private float rangedOptimalDistance = 8f;
    [SerializeField, Min(0.1f)] private float rangedMaxDistance = 12f;
    [SerializeField, Min(0.1f)] private float rangedMinDistance = 4f;
    [SerializeField, Range(0f, 1f)] private float rangedStrafeStrength = 0.85f;

    [Header("Combat")]
    [SerializeField, Min(0.1f)] private float fallbackAttackRange = 2.5f;
    [SerializeField, Min(0.1f)] private float skillCastRange = 10f;
    [SerializeField, Min(0f)] private float skillAttemptIntervalMin = 0.35f;
    [SerializeField, Min(0f)] private float skillAttemptIntervalMax = 0.9f;
    [SerializeField, Range(0f, 1f)] private float normalSkillChance = 0.6f;
    [SerializeField, Range(0f, 1f)] private float ultimateSkillChance = 0.35f;

    private UnitController _unit;
    private SkillSystem _skills;
    private WeaponController _weaponController;
    private UnitController _currentTarget;
    private float _currentThreatScore;
    private Vector3 _eqsDesiredPosition;

    private float _nextThinkAt;
    private float _nextRetargetAt;
    private float _nextNormalSkillAt;
    private float _nextUltimateSkillAt;
    private float _nextEqsQueryAt;
    private float _strafeDirection = 1f;
    private bool _initialized;

    private const float ThreatReEvalInterval = 0.3f;
    private float _lastThreatEval;
    private const float LowHealthThreshold = 0.4f;
    private const float HighThreatThreshold = 15f;

    private void Awake()
    {
        TryInitialize();
    }

    private void OnEnable()
    {
        TryInitialize();
    }

    [Server]
    private void TryInitialize()
    {
        if (_initialized)
        {
            return;
        }

        if (!NetworkServer.active)
        {
            return;
        }

        _unit = GetComponent<UnitController>();
        _skills = GetComponent<SkillSystem>();
        _weaponController = GetComponent<WeaponController>();

        if (_unit == null)
        {
            return;
        }

        _nextThinkAt = Time.time + Random.Range(0f, thinkIntervalSeconds);
        _nextRetargetAt = Time.time + Random.Range(0f, retargetIntervalSeconds);
        _nextNormalSkillAt = Time.time + Random.Range(0.1f, 0.5f);
        _nextUltimateSkillAt = Time.time + Random.Range(0.3f, 0.9f);
        _nextEqsQueryAt = Time.time + Random.Range(0f, eqsQueryIntervalSeconds);
        _eqsDesiredPosition = _unit.transform.position;
        _initialized = true;
    }

    [ServerCallback]
    private void Update()
    {
        if (!_initialized)
        {
            TryInitialize();
        }

        if (_unit == null)
        {
            return;
        }

        if (_unit.IsDead)
        {
            StopMovement();
            return;
        }

        if (!CanActInCurrentMode())
        {
            StopMovement();
            return;
        }

        // Query EQS for optimal positioning
        if (Time.time >= _nextEqsQueryAt)
        {
            _nextEqsQueryAt = Time.time + eqsQueryIntervalSeconds;
            QueryEnvironment();
        }

        if (Time.time < _nextThinkAt)
        {
            return;
        }

        _nextThinkAt = Time.time + thinkIntervalSeconds;

        if (Time.time >= _nextRetargetAt || !IsTargetValid(_currentTarget))
        {
            _nextRetargetAt = Time.time + retargetIntervalSeconds;
            _currentTarget = AcquireTarget();
        }

        if (!IsTargetValid(_currentTarget))
        {
            StopMovement();
            return;
        }

        DriveMovementAndFacing(_currentTarget);
        DriveCombat(_currentTarget);
    }

    [Server]
    private bool CanActInCurrentMode()
    {
        if (SkirmishGameManager.Instance != null)
        {
            return SkirmishGameManager.Instance.CurrentRoundState == SkirmishGameManager.RoundState.InRound;
        }

        if (CastleSiegeManager.Instance != null)
        {
            return CastleSiegeManager.Instance.IsInGame;
        }

        return true;
    }

    [Server]
    private UnitController AcquireTarget()
    {
        if (_unit == null)
        {
            return null;
        }

        if (CastleSiegeManager.Instance != null && CastleSiegeManager.Instance.IsInGame)
        {
            var enemyLord = CastleSiegeManager.Instance.ServerGetAliveEnemyLordForTeam(_unit.team, _unit.transform.position);
            if (IsTargetValid(enemyLord) && HasLineOfSight(_unit, enemyLord))
            {
                return enemyLord;
            }
        }

        if (PlayerUnitsManager.Instance == null)
        {
            return null;
        }

        UnitController bestTarget = null;
        float bestThreatScore = float.MinValue;

        for (int i = 0; i < PlayerUnitsManager.Instance.playerUnits.Count; i++)
        {
            var playerUnit = PlayerUnitsManager.Instance.playerUnits[i];
            if (playerUnit.Unit == null)
            {
                continue;
            }

            var candidate = playerUnit.Unit.GetComponent<UnitController>();
            if (!IsTargetValid(candidate))
            {
                continue;
            }

            if (candidate.team == _unit.team)
            {
                continue;
            }

            // Only target if bot has line of sight
            if (!HasLineOfSight(_unit, candidate))
            {
                continue;
            }

            float threatScore = CalculateThreatScore(candidate);
            if (threatScore > bestThreatScore)
            {
                bestThreatScore = threatScore;
                bestTarget = candidate;
            }
        }

        _currentThreatScore = bestThreatScore;
        return bestTarget;
    }

    [Server]
    private float CalculateThreatScore(UnitController candidate)
    {
        if (candidate == null || _unit == null)
        {
            return float.MinValue;
        }

        float distance = Vector3.Distance(_unit.transform.position, candidate.transform.position);
        float proximityScore = Mathf.Max(0f, 20f - distance);
        float healthPercent = (float)candidate.health / Mathf.Max(1, candidate.maxHealth);
        float weakTargetBonus = (healthPercent < LowHealthThreshold) ? 10f : 0f;
        float myHealthPercent = (float)_unit.health / Mathf.Max(1, _unit.maxHealth);
        float defensiveBonus = (myHealthPercent < LowHealthThreshold) ? 8f : 0f;

        float threatScore = proximityScore + weakTargetBonus + defensiveBonus + Random.Range(-2f, 2f);
        return threatScore;
    }

    [Server]
    private bool IsTargetValid(UnitController target)
    {
        if (target == null)
        {
            return false;
        }

        if (!target.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (target.IsDead)
        {
            return false;
        }

        if (_unit != null && target == _unit)
        {
            return false;
        }

        return true;
    }

    [Server]
    private void DriveMovementAndFacing(UnitController target)
    {
        Vector3 toTarget = target.transform.position - _unit.transform.position;
        toTarget.y = 0f;

        float distance = toTarget.magnitude;
        Vector3 desiredDirection = Vector3.zero;

        bool isRangedWeapon = IsRangedWeapon();

        if (isRangedWeapon)
        {
            // Ranged bot behavior - maintain optimal distance
            if (distance > rangedMaxDistance)
            {
                desiredDirection = toTarget.normalized;
            }
            else if (distance < rangedMinDistance)
            {
                desiredDirection = -toTarget.normalized;
            }
            else if (distance < rangedOptimalDistance)
            {
                desiredDirection = -toTarget.normalized * 0.5f;
            }
            else if (distance > rangedOptimalDistance)
            {
                desiredDirection = toTarget.normalized * 0.5f;
            }
            else
            {
                // At optimal range, strafe
                Vector3 lateral = Vector3.Cross(Vector3.up, toTarget.normalized) * _strafeDirection;
                desiredDirection = (toTarget.normalized * 0.2f) + (lateral * rangedStrafeStrength);

                if (Random.value < 0.03f)
                {
                    _strafeDirection *= -1f;
                }
            }
        }
        else
        {
            // Melee bot behavior - aggressive close range
            if (distance > meleeChaseDistance)
            {
                desiredDirection = toTarget.normalized;
            }
            else if (distance < meleeRetreatDistance)
            {
                desiredDirection = -toTarget.normalized;
            }
            else
            {
                Vector3 lateral = Vector3.Cross(Vector3.up, toTarget.normalized) * _strafeDirection;
                desiredDirection = (toTarget.normalized * 0.35f) + (lateral * meleeStrafeStrength);

                if (Random.value < 0.04f)
                {
                    _strafeDirection *= -1f;
                }
            }
        }

        desiredDirection = Vector3.ClampMagnitude(desiredDirection, 1f);

        _unit.horizontalInput = desiredDirection.x;
        _unit.verticalInput = desiredDirection.z;

        Vector3 facingPos = _unit.transform.position - target.transform.position;
        float angle = -(Mathf.Atan2(facingPos.z, facingPos.x) * Mathf.Rad2Deg) - 90f;
        _unit.angle = angle;
    }

    [Server]
    private void DriveCombat(UnitController target)
    {
        float distance = Vector3.Distance(_unit.transform.position, target.transform.position);
        float weaponRange = fallbackAttackRange;

        if (_unit.currentWeapon != null)
        {
            weaponRange = Mathf.Max(0.1f, _unit.currentWeapon.attackRange);
        }

        if (distance <= weaponRange * 1.1f)
        {
            _unit.Attack();
        }

        if (_skills == null)
        {
            return;
        }

        if (distance > skillCastRange)
        {
            return;
        }

        Vector3 aimPoint = target.transform.position;

        if (!_unit.unitActionState.IsActive)
        {
            if (Time.time >= _nextUltimateSkillAt && _skills.ultimateSkills.Count > 0)
            {
                _nextUltimateSkillAt = Time.time + Random.Range(skillAttemptIntervalMin, skillAttemptIntervalMax);

                if (Random.value <= ultimateSkillChance || _currentThreatScore > HighThreatThreshold)
                {
                    _skills.CastSkill(SkillSlotType.Ultimate, 0, aimPoint);
                    return;
                }
            }

            if (Time.time >= _nextNormalSkillAt && _skills.normalSkills.Count > 0)
            {
                _nextNormalSkillAt = Time.time + Random.Range(skillAttemptIntervalMin, skillAttemptIntervalMax);

                if (Random.value <= normalSkillChance)
                {
                    int index = Random.Range(0, _skills.normalSkills.Count);
                    _skills.CastSkill(SkillSlotType.Normal, index, aimPoint);
                }
            }
        }
    }

    [Server]
    private void StopMovement()
    {
        if (_unit == null)
        {
            return;
        }

        _unit.horizontalInput = 0f;
        _unit.verticalInput = 0f;
    }

    [Server]
    private bool IsRangedWeapon()
    {
        if (_unit == null || _unit.currentWeapon == null)
        {
            return false;
        }

        WeaponType weaponType = _unit.currentWeapon.weaponType;
        return weaponType == WeaponType.Bow || weaponType == WeaponType.Gun || weaponType == WeaponType.Pistols;
    }

    [Server]
    private bool HasLineOfSight(UnitController from, UnitController to)
    {
        if (from == null || to == null)
        {
            return false;
        }

        Vector3 fromPos = from.transform.position + Vector3.up * 0.5f;
        Vector3 toPos = to.GetComponent<Collider>() != null 
            ? to.GetComponent<Collider>().bounds.center 
            : to.transform.position + Vector3.up * 0.5f;

        Vector3 direction = (toPos - fromPos).normalized;
        float distance = Vector3.Distance(fromPos, toPos);

        if (distance > losCheckDistance)
        {
            return false;
        }

        int raycastMask = losLayerMask == -1 ? LayerMask.GetMask("Default") : losLayerMask;

        if (Physics.Raycast(fromPos, direction, out RaycastHit hit, distance, raycastMask, QueryTriggerInteraction.Ignore))
        {
            UnitController hitUnit = hit.collider.GetComponent<UnitController>();
            bool hasLOS = (hitUnit == to);

            if (debugDrawLineOfSight)
            {
                Debug.DrawLine(fromPos, toPos, hasLOS ? Color.green : Color.red, 0.1f);
            }

            return hasLOS;
        }

        if (debugDrawLineOfSight)
        {
            Debug.DrawLine(fromPos, toPos, Color.green, 0.1f);
        }

        return true;
    }

    [Server]
    private void QueryEnvironment()
    {
        if (_unit == null || !IsTargetValid(_currentTarget))
        {
            return;
        }

        bool isRanged = IsRangedWeapon();
        float queryRadius = isRanged ? rangedMaxDistance * 1.5f : meleeChaseDistance * 1.5f;
        int sampleCount = 8;

        Vector3 targetDir = (_currentTarget.transform.position - _unit.transform.position).normalized;
        Vector3 bestPosition = _unit.transform.position;
        float bestScore = float.MinValue;

        // Query a circle of positions around the bot
        for (int i = 0; i < sampleCount; i++)
        {
            float angle = (i / (float)sampleCount) * 360f * Mathf.Deg2Rad;
            float offsetX = Mathf.Cos(angle) * queryRadius;
            float offsetZ = Mathf.Sin(angle) * queryRadius;
            
            Vector3 candidatePos = _unit.transform.position + new Vector3(offsetX, 0f, offsetZ);

            // Clamp to ground
            if (Physics.Raycast(candidatePos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
            {
                candidatePos = hit.point;
            }

            float score = EvaluatePosition(candidatePos, _currentTarget, isRanged);
            if (score > bestScore)
            {
                bestScore = score;
                bestPosition = candidatePos;
            }
        }

        _eqsDesiredPosition = bestPosition;

        if (debugDrawLineOfSight)
        {
            Debug.DrawLine(_unit.transform.position, _eqsDesiredPosition, Color.cyan, 0.1f);
        }
    }

    [Server]
    private float EvaluatePosition(Vector3 position, UnitController target, bool isRanged)
    {
        if (target == null || _unit == null)
        {
            return float.MinValue;
        }

        float score = 0f;

        // Distance to target scoring
        float distToTarget = Vector3.Distance(position, target.transform.position);
        
        if (isRanged)
        {
            // Ranged: prefer optimal distance
            float optimalDistScore = 20f - Mathf.Abs(distToTarget - rangedOptimalDistance);
            score += Mathf.Max(0f, optimalDistScore) * 2f;
        }
        else
        {
            // Melee: prefer closer distances
            float closenessScore = Mathf.Max(0f, meleeChaseDistance - distToTarget);
            score += closenessScore * 1.5f;
        }

        // Line of sight bonus
        if (HasLineOfSight(_unit, target))
        {
            score += 10f;
        }

        // Prefer positions that don't expose bot to other enemies
        float exposureBonus = 0f;
        int enemyVisCount = 0;

        if (PlayerUnitsManager.Instance != null)
        {
            for (int i = 0; i < PlayerUnitsManager.Instance.playerUnits.Count; i++)
            {
                var playerUnit = PlayerUnitsManager.Instance.playerUnits[i];
                if (playerUnit.Unit == null)
                {
                    continue;
                }

                var candidate = playerUnit.Unit.GetComponent<UnitController>();
                if (candidate == null || candidate == _unit || candidate == target)
                {
                    continue;
                }

                if (candidate.team == _unit.team)
                {
                    continue;
                }

                if (candidate.IsDead)
                {
                    continue;
                }

                // Check distance to other enemies
                float distToOther = Vector3.Distance(position, candidate.transform.position);
                if (distToOther < 15f)
                {
                    enemyVisCount++;
                }
            }
        }

        // Penalty for positions exposed to multiple enemies
        exposureBonus = -(enemyVisCount * 3f);
        score += exposureBonus;

        // Random jitter to prevent predictable movement
        score += Random.Range(-1f, 1f);

        return score;
    }
}
