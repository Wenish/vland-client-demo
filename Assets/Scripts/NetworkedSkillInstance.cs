using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using MyGame.Events;
using ShadowInfection.DI;
using UnityEngine;
using UnityEngine.VFX;

public class NetworkedSkillInstance : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnSkillNameChanged))]
    public string skillName;

    [SyncVar]
    public double lastCastTime = -Mathf.Infinity;

    [SyncVar]
    private bool _isRecastWindowOpen;

    [SyncVar]
    private double _recastWindowEndTime = -Mathf.Infinity;

    public SkillData skillData;
    public bool IsRecastWindowOpen => _isRecastWindowOpen;
    public float RecastWindowRemaining
    {
        get
        {
            if (!_isRecastWindowOpen)
                return 0f;

            var remaining = (float)(_recastWindowEndTime - NetworkTime.time);
            return Mathf.Max(0f, remaining);
        }
    }

    [SyncVar, SerializeField]
    private UnitController unit;
    public UnitController Caster => unit;
    private SkillDatabase skillDatabase;
    public event Action<NetworkedSkillInstance> OnCleanup;

    [SerializeField]
    public readonly List<(UnitMediator target, Buff buff)> appliedBuffs = new();

    private readonly List<GameObject> _spawnedVfxInstances = new();
    private int _nextIndicatorSessionId = 1;
    private readonly HashSet<int> _activeIndicatorSessions = new();

    public void Initialize(string name, UnitController unitRef)
    {
        skillName = name;
        unit = unitRef;
        skillDatabase = GameServices.Databases?.Skills;
        ResolveSkillData();

        EnsureReactiveRunner();
    }

    public void ResolveSkillData()
    {
        if (skillDatabase == null)
        {
            skillDatabase = GameServices.Databases?.Skills;
        }

        if (skillDatabase == null)
            return;

        skillData = skillDatabase.GetSkillByName(skillName);
    }

    public void OnSkillNameChanged(string oldName, string newName)
    {
        if (skillDatabase == null)
        {
            skillDatabase = GameServices.Databases?.Skills;
        }

        if (skillDatabase == null)
            return;

        skillData = skillDatabase.GetSkillByName(newName);
        EnsureReactiveRunner();
    }

    public bool IsOnCooldown
        => skillData != null && skillData.cooldown > 0f && NetworkTime.time < lastCastTime + skillData.cooldown;

    public float CooldownRemaining
    {
        get
        {
            if (skillData == null || skillData.cooldown <= 0f)
                return 0f;

            // remaining = (lastCast + cd) - now
            var remaining = (float)((lastCastTime + skillData.cooldown) - NetworkTime.time);
            return Mathf.Max(0f, remaining);
        }
    }

    public float CooldownProgress
    {
        get
        {
            if (skillData == null || skillData.cooldown <= 0f)
                return 0f;

            // keep returning 0..100 like before
            return Mathf.Clamp01(CooldownRemaining / skillData.cooldown) * 100f;
        }
    }

    private Coroutine _runningInitCoroutine;
    private CastContext _runningInitContext;

    [Server]
    public void TriggerInit()
    {
        if (skillData == null) return;
        if (_runningInitCoroutine != null)
        {
            StopCoroutine(_runningInitCoroutine);
        }
        _runningInitContext = new CastContext(unit, this);
        _runningInitCoroutine = StartCoroutine(skillData.ExecuteInitCoroutine(_runningInitContext));
        // Reactive triggers: subscribe at init so passives/normal skills can react
        EnsureReactiveRunner();
    }

    public void CancelInit()
    {
        _runningInitContext?.Cancel();
        _runningInitContext = null;
        if (_runningInitCoroutine != null)
        {
            StopCoroutine(_runningInitCoroutine);
            _runningInitCoroutine = null;
        }
        Rpc_CleanupSpawnedVfx();
    }

    private Coroutine _runningCastCoroutine;
    private CastContext _runningCastContext;
    private int _lastCastStartFrame = -1;

    [Server]
    public SkillCastResult Cast(Vector3? aimPoint)
    {
        if (unit == null || unit.IsDead || unit.IsKnockedUp)
            return SkillCastResult.Rejected;

        // If a cast is already running, signal it instead of restarting
        if (_runningCastCoroutine != null && _runningCastContext != null && !_runningCastContext.IsCancelled)
        {
            Debug.Log($"[Cast] Signaling running cast for {skillName} (busy={unit.unitActionState.IsActive})");
            _runningCastContext.SignalTrigger();
            return SkillCastResult.SignaledRunningCast;
        }

        if (skillData == null)
            return SkillCastResult.Rejected;
        if (IsOnCooldown)
            return SkillCastResult.OnCooldown;

        var currentWeaponType = unit.currentWeapon != null
            ? (WeaponType?)unit.currentWeapon.weaponType
            : null;
        if (!skillData.CanBeUsedWithWeapon(currentWeaponType))
        {
            return SkillCastResult.Rejected;
        }

        // Skills yield to other skills, but auto-attack should not block a player ability.
        if (unit.unitActionState.IsActive && !skillData.canActivateWhileBusy)
        {
            if (unit.unitActionState.state.type == UnitActionState.ActionType.Attacking)
            {
                unit.InterruptAction();
            }
            else
            {
                return SkillCastResult.Rejected;
            }
        }

        // Prevent duplicate admission within the same server frame (e.g. very fast spam / duplicate command packets).
        if (_lastCastStartFrame == Time.frameCount)
            return SkillCastResult.Rejected;

        if (_runningCastCoroutine != null)
        {
            StopCoroutine(_runningCastCoroutine);
        }
        _lastCastStartFrame = Time.frameCount;

        Vector3? clampedAim = aimPoint;
        if (clampedAim.HasValue && skillData != null)
        {
            clampedAim = SkillAimUtil.ClampAimPoint(unit, clampedAim.Value, skillData);
        }

        Quaternion? aimRotation = clampedAim.HasValue
            ? SkillAimUtil.GetAimRotation(unit, clampedAim.Value)
            : null;

        // Finish facing before cast/channel can lock turn speed mid-lerp, and so
        // LockOnConfirm indicators match unit facing even when turnSpeed stays at 100%.
        var indicator = SkillAimPreviewUtil.Resolve(this);
        bool shouldSnapFacing = SkillAimUtil.ShouldSnapFacingToCastAim(
            unit,
            new Vector2(unit.horizontalInput, unit.verticalInput),
            indicator);

        if (shouldSnapFacing)
        {
            if (clampedAim.HasValue)
                unit.SnapFacingToAimPoint(clampedAim.Value);
            else if (aimRotation.HasValue)
                unit.SnapFacingToAimRotation(aimRotation.Value);
        }

        _runningCastContext = new CastContext(unit, this)
        {
            aimPoint = clampedAim,
            aimRotation = aimRotation
        };
        _runningCastCoroutine = StartCoroutine(CastCoroutineWrapper(_runningCastContext));
        return SkillCastResult.Started;
    }

    [Server]
    public void SetRecastWindowOpen(bool isOpen)
    {
        _isRecastWindowOpen = isOpen;
        if (!isOpen)
        {
            _recastWindowEndTime = -Mathf.Infinity;
        }
    }

    [Server]
    public void SetRecastWindow(float durationSeconds)
    {
        _isRecastWindowOpen = durationSeconds > 0f;
        _recastWindowEndTime = _isRecastWindowOpen
            ? NetworkTime.time + durationSeconds
            : -Mathf.Infinity;
    }

    private IEnumerator CastCoroutineWrapper(CastContext ctx)
    {
        try
        {
            yield return skillData.ExecuteCastCoroutine(ctx);
        }
        finally
        {
            _runningCastCoroutine = null;
            _runningCastContext = null;
            ServerHideAllSkillIndicators();
        }
    }

    [Server]
    public void CancelCast()
    {
        _isRecastWindowOpen = false;
        _recastWindowEndTime = -Mathf.Infinity;
        _runningCastContext?.Cancel();
        _runningCastContext = null;
        if (_runningCastCoroutine != null)
        {
            StopCoroutine(_runningCastCoroutine);
            _runningCastCoroutine = null;
        }
        Rpc_CleanupSpawnedVfx();
        ServerHideAllSkillIndicators();
    }

    [Server]
    public int ServerShowSkillIndicator(
        SkillIndicatorDisplayParams display,
        Vector3 aimPoint,
        UnitController followTarget = null)
    {
        // One active cast indicator at a time — prevents Phase 1 flashing when Phase 2 shows.
        ServerHideAllSkillIndicators();

        int sessionId = _nextIndicatorSessionId++;
        if (_nextIndicatorSessionId <= 0)
            _nextIndicatorSessionId = 1;

        _activeIndicatorSessions.Add(sessionId);

        var conn = GetOwnerConnection();
        if (conn != null)
        {
            uint followNetId = followTarget != null ? followTarget.netId : 0u;
            TargetShowSkillIndicator(conn, sessionId, display, aimPoint, followNetId);
        }

        return sessionId;
    }

    [Server]
    public void ServerHideSkillIndicator(int sessionId)
    {
        if (!_activeIndicatorSessions.Remove(sessionId))
            return;

        var conn = GetOwnerConnection();
        if (conn != null)
            TargetHideSkillIndicator(conn, sessionId);
    }

    [Server]
    public void ServerHideAllSkillIndicators()
    {
        _activeIndicatorSessions.Clear();

        var conn = GetOwnerConnection();
        if (conn != null)
            TargetHideAllSkillIndicators(conn);
    }

    [Server]
    private NetworkConnectionToClient GetOwnerConnection()
    {
        if (unit == null)
            return null;

        var playerUnits = GameServices.PlayerUnits;
        if (playerUnits == null)
            return null;

        GameObject unitGo = unit.gameObject;
        for (int i = 0; i < playerUnits.playerUnits.Count; i++)
        {
            var entry = playerUnits.playerUnits[i];
            if (entry.Unit != unitGo)
                continue;

            if (entry.ConnectionId < 0)
                return null;

            return NetworkServer.connections.TryGetValue(entry.ConnectionId, out var conn)
                ? conn
                : null;
        }

        return null;
    }

    [TargetRpc]
    private void TargetShowSkillIndicator(
        NetworkConnectionToClient conn,
        int sessionId,
        SkillIndicatorDisplayParams display,
        Vector3 aimPoint,
        uint followTargetNetId)
    {
        UnitController followTarget = null;
        if (followTargetNetId != 0
            && NetworkClient.spawned.TryGetValue(followTargetNetId, out var identity)
            && identity != null)
        {
            followTarget = identity.GetComponent<UnitController>();
        }

        GameMessages.Publish(
            new SkillIndicatorShowEvent(sessionId, unit, display, aimPoint, followTarget, this));
    }

    [TargetRpc]
    private void TargetHideSkillIndicator(NetworkConnectionToClient conn, int sessionId)
    {
        GameMessages.Publish(new SkillIndicatorHideEvent(sessionId));
    }

    [TargetRpc]
    private void TargetHideAllSkillIndicators(NetworkConnectionToClient conn)
    {
        GameMessages.Publish(new SkillIndicatorHideAllEvent());
    }

    [Server]
    public void ServerUpdateRunningCastAim(Vector3 aimPoint)
    {
        if (_runningCastContext == null || _runningCastContext.IsCancelled)
            return;

        if (skillData == null || unit == null)
            return;

        if (!_runningCastContext.updatesAimDuringCast)
            return;

        Vector3 clamped = SkillAimUtil.ClampAimPoint(unit, aimPoint, skillData);
        _runningCastContext.aimPoint = clamped;
        _runningCastContext.aimRotation = SkillAimUtil.GetAimRotation(unit, clamped);

        // Keep facing on the live aim without NetworkTransform teleports (those fire every frame).
        unit.angle = SkillAimUtil.GetFacingAngleYaw(unit.transform.position, clamped);
        float turnSpeed = unit.unitMediator != null
            ? unit.unitMediator.Stats.GetStat(StatType.TurnSpeed)
            : 1f;
        if (turnSpeed <= 0.05f)
            unit.SnapFacingToAimPoint(clamped);
    }

    /// <summary>
    /// Active cast aim used to keep unit facing aligned with LockOnConfirm indicators.
    /// </summary>
    [Server]
    public bool TryGetRunningCastAim(out Vector3 aimPoint, out Quaternion aimRotation, out bool updatesAimDuringCast)
    {
        aimPoint = default;
        aimRotation = default;
        updatesAimDuringCast = false;

        if (_runningCastContext == null || _runningCastContext.IsCancelled)
            return false;

        updatesAimDuringCast = _runningCastContext.updatesAimDuringCast;

        if (_runningCastContext.aimPoint.HasValue)
        {
            aimPoint = _runningCastContext.aimPoint.Value;
            aimRotation = _runningCastContext.aimRotation
                ?? SkillAimUtil.GetAimRotation(unit, aimPoint);
            return true;
        }

        if (_runningCastContext.aimRotation.HasValue)
        {
            aimRotation = _runningCastContext.aimRotation.Value;
            aimPoint = unit != null
                ? unit.transform.position + (aimRotation * Vector3.forward)
                : aimRotation * Vector3.forward;
            return true;
        }

        return false;
    }

    [Server]
    public void ManageBuff(UnitMediator mediator, Buff buff, bool apply)
    {
        if (apply)
        {
            mediator.AddBuff(buff);
            appliedBuffs.Add((mediator, buff));

            // Subscribe to OnRemoved only once
            buff.OnRemoved += () => RemoveManagedBuff(mediator, buff);
        }
        else
        {
            mediator.Buffs.RemoveBuff(buff);

            // Unsubscribe from the OnRemoved event
            buff.OnRemoved -= () => RemoveManagedBuff(mediator, buff);
            appliedBuffs.Remove((mediator, buff));
        }
    }

    // Separate method for clean removal logic
    [Server]
    private void RemoveManagedBuff(UnitMediator mediator, Buff buff)
    {
        appliedBuffs.Remove((mediator, buff));
        buff.OnRemoved -= () => RemoveManagedBuff(mediator, buff);
    }

    private void TrackVfxInstance(GameObject instance)
    {
        if (instance == null) return;
        _spawnedVfxInstances.Add(instance);
    }

    private void CleanupLocalVfx()
    {
        for (int i = _spawnedVfxInstances.Count - 1; i >= 0; i--)
        {
            var go = _spawnedVfxInstances[i];
            if (go == null)
            {
                _spawnedVfxInstances.RemoveAt(i);
                continue;
            }

            var visualEffect = go.GetComponent<VisualEffect>();
            if (visualEffect != null)
            {
                visualEffect.Stop();
            }

            Destroy(go);
            _spawnedVfxInstances.RemoveAt(i);
        }
    }

    [ClientRpc(includeOwner = true)]
    public void Rpc_SpawnAreaVFX(
        Vector3 origin,
        Vector3 direction,
        float range,
        float width,
        string materialName,
        float duration,
        Transform target,
        AreaVFXShape shape,
        Vector2 offset,
        bool attachToTarget)
    {
        Mesh mesh = shape switch
        {
            AreaVFXShape.Rectangle => MeshFactory.BuildRectangle(range, width),
            AreaVFXShape.Circle => MeshFactory.BuildCircle(radius: range, segments: Mathf.Clamp(Mathf.CeilToInt(range * 16), 16, 256)),
            AreaVFXShape.Cone => MeshFactory.BuildCone(radius: range, angleDegrees: width),
            _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null)
        };

        Material mat = Resources.Load<Material>("Materials/VFX/" + materialName);
        // Offset in the direction of 'direction' (forward) and its right vector
        Vector3 forward = direction.normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        Vector3 offsetPosition = forward * offset.y + right * offset.x;

        Vector3 worldPos = origin + offsetPosition;
        if (shape == AreaVFXShape.Rectangle)
        {
            // For rectangle, center it at origin + forward * (range/2)
            worldPos += direction.normalized * (range * 0.5f);
        }
        Quaternion worldRot = Quaternion.LookRotation(direction.normalized, Vector3.up * 0.01f);

        Vector3 localPos = worldPos;
        Quaternion localRot = worldRot;

        if (attachToTarget && target != null)
        {
            // Convert world to local
            localPos = target.InverseTransformPoint(worldPos);
            localRot = Quaternion.Inverse(target.rotation) * worldRot;
        }

        var materialPropertyBlock = new MaterialPropertyBlock();
        materialPropertyBlock.SetFloat("_Duration", duration);
        materialPropertyBlock.SetFloat("_SpawnTime", Time.time);
        switch (shape)
        {
            case AreaVFXShape.Rectangle:
                materialPropertyBlock.SetFloat("_Width", width);
                materialPropertyBlock.SetFloat("_Length", range);

                break;
            case AreaVFXShape.Circle:
                materialPropertyBlock.SetFloat("_Radius", range);
                break;
            case AreaVFXShape.Cone:
                materialPropertyBlock.SetFloat("_Radius", range);
                materialPropertyBlock.SetFloat("_AngleDegrees", width);
                break;
        }

        var vfxInstance = MeshVFXSpawner.Spawn(mesh, mat, localPos, localRot, duration, materialPropertyBlock, attachToTarget ? target : null);
        TrackVfxInstance(vfxInstance);
    }

    [ClientRpc(includeOwner = true)]
    public void Rpc_SpawnVFXGraphPrefab(
        Vector3 position,
        Quaternion rotation,
        float duration,
        float lifetime,
        bool attachToTarget,
        uint targetNetId,
        string prefabName)
    {
        Transform parent = null;
        if (attachToTarget && targetNetId != 0)
        {
            if (TryGetNetworkIdentity(targetNetId, out var identity))
                parent = identity.transform;
        }

        // Load the prefab from Resources by name
        var prefab = Resources.Load<GameObject>("Vfx/" + prefabName);
        if (prefab == null)
        {
            UnityEngine.Debug.LogWarning($"VFX Prefab '{prefabName}' not found in Resources!");
            return;
        }

        var vfxInstance = Instantiate(prefab, position, rotation, parent);

        var visualEffect = vfxInstance.GetComponent<VisualEffect>();
        if (visualEffect != null)
        {
            if (visualEffect.HasFloat("Duration"))
            {
                visualEffect.SetFloat("Duration", duration);
            }
            if (visualEffect.HasFloat("Lifetime"))
            {
                visualEffect.SetFloat("Lifetime", lifetime);
            }
        }
        TrackVfxInstance(vfxInstance);
        Destroy(vfxInstance, duration + lifetime);
    }

    [ClientRpc(includeOwner = true)]
    public void Rpc_CleanupSpawnedVfx()
    {
        CleanupLocalVfx();
    }

    private static bool TryGetNetworkIdentity(uint netId, out NetworkIdentity identity)
    {
        identity = null;

        // server-side dictonary
        if (NetworkServer.active && NetworkServer.spawned.TryGetValue(netId, out identity))
        {
            return true;
        }

        // client-side dictonary
        if (NetworkClient.active && NetworkClient.spawned.TryGetValue(netId, out identity))
        {
            return true;
        }

        return false;
    }

    [ClientRpc(includeOwner = true)]
    public void Rpc_PlaySound(string soundId, Vector3 position, bool attachToTarget, uint targetNetId)
    {
        if (string.IsNullOrEmpty(soundId)) return;
        if (!ShadowInfection.DI.GameLifetimeScope.TryResolve<ShadowInfection.Audio.ISfxPlayer>(out var sfx))
            return;

        if (attachToTarget && targetNetId != 0 && TryGetNetworkIdentity(targetNetId, out var identity))
            sfx.PlayAttached(soundId, identity.transform);
        else
            sfx.Play(soundId, position);
    }

    [ClientRpc(includeOwner = true)]
    public void Rpc_PlayUnitAnimation(
        uint targetNetId,
        bool useTrigger,
        string triggerName,
        string stateName,
        int layer,
        float transitionDuration,
        float normalizedTime,
        float speedMultiplier,
        bool resetTriggerBeforeSet)
    {
        if (targetNetId == 0) return;
        if (!TryGetNetworkIdentity(targetNetId, out var identity) || identity == null) return;

        var unit = identity.GetComponent<UnitController>();
        if (unit == null) return;

        var animator = ResolveAnimator(unit);
        if (animator == null) return;

        if (speedMultiplier > 0f)
        {
            animator.speed = speedMultiplier;
        }

        if (useTrigger)
        {
            if (string.IsNullOrWhiteSpace(triggerName)) return;
            if (resetTriggerBeforeSet)
            {
                animator.ResetTrigger(triggerName);
            }

            animator.SetTrigger(triggerName);
            return;
        }

        if (string.IsNullOrWhiteSpace(stateName)) return;
        animator.CrossFadeInFixedTime(stateName, Mathf.Max(0f, transitionDuration), Mathf.Max(0, layer), Mathf.Clamp01(normalizedTime));
    }

    private static Animator ResolveAnimator(UnitController unit)
    {
        if (unit == null) return null;

        if (unit.modelInstance != null)
        {
            var modelAnimator = unit.modelInstance.GetComponentInChildren<Animator>(true);
            if (modelAnimator != null) return modelAnimator;
        }

        return unit.GetComponentInChildren<Animator>(true);
    }

    [Server]
    public void Cleanup()
    {
        OnCleanup?.Invoke(this);
        Rpc_CleanupSpawnedVfx();
        var buffsToRemove = new List<(UnitMediator target, Buff buff)>(appliedBuffs);

        foreach (var (target, buff) in buffsToRemove)
        {
            ManageBuff(target, buff, false);
        }

        appliedBuffs.Clear();
    }

    private ReactiveTriggerRunner _reactiveRunner;
    private void EnsureReactiveRunner()
    {
        if (skillData == null) return;
        if (_reactiveRunner == null)
        {
            _reactiveRunner = GetComponent<ReactiveTriggerRunner>();
        }
        _reactiveRunner.Initialize(this, skillData.reactiveTriggers);
    }

    private void OnDestroy()
    {
        CleanupLocalVfx();
    }

    [ContextMenu("Benchmark Cast 10,000x (Coroutine)")]
    [Server]
    public void BenchmarkCast()
    {
        // turn your void into a coroutine
        StartCoroutine(BenchmarkCastCoroutine());
    }

    private IEnumerator BenchmarkCastCoroutine()
    {
        if (skillData == null)
            yield break;

        int total = 10000;
        int finished = 0;

        // Stopwatch can’t start/stop inside the main thread yield,
        // so we start it here; we’ll Stop() it after all coroutines.
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < total; i++)
        {
            var ctx = new CastContext(unit, this);
            // launch each wrapped cast
            StartCoroutine(WrapAndCount(ctx, () => finished++));
        }

        // wait until every single one has fired its callback
        yield return new WaitUntil(() => finished >= total);

        stopwatch.Stop();
        UnityEngine.Debug.Log($"TriggerCast {total:N0}×: {stopwatch.ElapsedMilliseconds} ms");
    }

    // helper that runs the real cast, then fires the onDone callback
    private IEnumerator WrapAndCount(CastContext ctx, Action onDone)
    {
        // run your original coroutine to completion
        yield return skillData.ExecuteCastCoroutine(ctx);
        // notify
        onDone();
    }

    [Server]
    internal void OnCastCounted()
    {
        // Start cooldown when the first eligible effect executes
        lastCastTime = NetworkTime.time;
    }
}
