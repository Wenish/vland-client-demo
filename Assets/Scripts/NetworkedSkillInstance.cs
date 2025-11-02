using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Mirror;
using UnityEngine;
using UnityEngine.VFX;

public class NetworkedSkillInstance : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnSkillNameChanged))]
    public string skillName;

    [SyncVar]
    public double lastCastTime = -Mathf.Infinity;

    public SkillData skillData;

    [SyncVar, SerializeField]
    private UnitController unit;
    public UnitController Caster => unit;
    private SkillDatabase skillDatabase;

    [SerializeField]
    public readonly List<(UnitMediator target, Buff buff)> appliedBuffs = new();

    public void Initialize(string name, UnitController unitRef)
    {
        skillName = name;
        unit = unitRef;
        skillDatabase = DatabaseManager.Instance.skillDatabase;
        ResolveSkillData();

        EnsureReactiveRunner();
    }

    public void ResolveSkillData()
    {
        if (skillDatabase == null)
        {
            skillDatabase = DatabaseManager.Instance.skillDatabase;
        }

        skillData = skillDatabase.GetSkillByName(skillName);
    }

    public void OnSkillNameChanged(string oldName, string newName)
    {
        if (skillDatabase == null)
        {
            skillDatabase = DatabaseManager.Instance.skillDatabase;
        }

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
    }

    private Coroutine _runningCastCoroutine;
    private CastContext _runningCastContext;

    [Server]
    public void Cast(Vector3? aimPoint)
    {
        if (IsOnCooldown || skillData == null) return;

        // If the unit is busy (casting/attacking/etc), only block if the skill doesn't allow activation while busy
        if (unit.unitActionState.IsActive && !skillData.canActivateWhileBusy) return;

        if (_runningCastCoroutine != null)
        {
            StopCoroutine(_runningCastCoroutine);
        }
        _runningCastContext = new CastContext(unit, this)
        {
            aimPoint = aimPoint,
            aimRotation = aimPoint.HasValue ? Quaternion.LookRotation(aimPoint.Value - unit.transform.position) : null
        };
        _runningCastCoroutine = StartCoroutine(skillData.ExecuteCastCoroutine(_runningCastContext));
    }    

    [Server]
    public void CancelCast()
    {
        _runningCastContext?.Cancel();
        _runningCastContext = null;
        if (_runningCastCoroutine != null)
        {
            StopCoroutine(_runningCastCoroutine);
            _runningCastCoroutine = null;
        }
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

        MeshVFXSpawner.Spawn(mesh, mat, localPos, localRot, duration, materialPropertyBlock, attachToTarget ? target : null);
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
        Destroy(vfxInstance, duration);
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
    public void Rpc_PlaySound(string soundName, Vector3 position, float pitchOffset, bool attachToTarget, uint targetNetId)
    {
        if (string.IsNullOrEmpty(soundName)) return;

        Transform parent = null;
        if (attachToTarget && targetNetId != 0)
        {
            if (TryGetNetworkIdentity(targetNetId, out var identity))
                parent = identity.transform;
        }

        SoundManager.Instance.PlaySound(soundName, position, parent, pitchOffset);
    }

    [Server]
    public void Cleanup()
    {
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
        var stopwatch = Stopwatch.StartNew();

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
