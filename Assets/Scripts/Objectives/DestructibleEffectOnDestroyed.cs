using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(DestructibleObjective))]
public class DestructibleEffectOnDestroyed : NetworkBehaviour
{
    public enum TeamFilterMode : byte
    {
        ResolvedTeamAllies = 0,
        NonResolvedTeamEnemies = 1,
        AnyTeam = 2,
    }

    public enum OriginMode : byte
    {
        ObjectivePosition = 0,
        KillerPosition = 1,
    }

    public enum CasterMode : byte
    {
        KillerOrResolvedTeamMember = 0,
        ObjectiveUnit = 1,
    }

    [Header("Effect")]
    [SerializeField] private SkillEffectChainData effectChain;

    [Header("Target Query")]
    [SerializeField, Min(0.1f)] private float searchRadius = 8f;
    [SerializeField] private LayerMask unitLayer = ~0;
    [SerializeField] private TeamFilterMode teamFilter = TeamFilterMode.ResolvedTeamAllies;
    [SerializeField] private int maxTargets = 0;

    [Header("Resolution")]
    [SerializeField] private bool requireResolvedTeam = true;
    [SerializeField] private OriginMode originMode = OriginMode.ObjectivePosition;
    [SerializeField] private CasterMode casterMode = CasterMode.KillerOrResolvedTeamMember;

    [Header("Runtime Skill Host")]
    [SerializeField] private NetworkedSkillInstance effectSkillHost;
    [SerializeField] private string skillInstancePrefabName = "SkillInstance";

    private DestructibleObjective _objective;
    private readonly Collider[] _overlapBuffer = new Collider[256];

    private void Awake()
    {
        _objective = GetComponent<DestructibleObjective>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (_objective == null)
        {
            _objective = GetComponent<DestructibleObjective>();
        }

        if (_objective != null)
        {
            _objective.OnDestroyedServer += OnObjectiveDestroyed;
        }
    }

    public override void OnStopServer()
    {
        if (_objective != null)
        {
            _objective.OnDestroyedServer -= OnObjectiveDestroyed;
        }

        base.OnStopServer();
    }

    [Server]
    private void OnObjectiveDestroyed(DestructibleObjective objective, UnitController killer, int resolvedTeamId)
    {
        if (effectChain == null)
        {
            return;
        }

        if (requireResolvedTeam && resolvedTeamId < 0)
        {
            return;
        }

        Vector3 origin = ResolveOrigin(killer);

        UnitController caster = ResolveCaster(killer, resolvedTeamId);
        if (caster == null)
        {
            Debug.LogWarning($"[{nameof(DestructibleEffectOnDestroyed)}] Could not resolve caster on {name}.", this);
            return;
        }

        if (!TryEnsureSkillHost(caster, out NetworkedSkillInstance host))
        {
            Debug.LogWarning($"[{nameof(DestructibleEffectOnDestroyed)}] Missing skill host, effect aborted on {name}.", this);
            return;
        }

        List<UnitController> targets = CollectTargets(origin, resolvedTeamId);
        if (targets.Count == 0)
        {
            return;
        }

        StartCoroutine(ExecuteEffectChain(host, caster, origin, targets));
    }

    [Server]
    private Vector3 ResolveOrigin(UnitController killer)
    {
        if (originMode == OriginMode.KillerPosition && killer != null)
        {
            return killer.transform.position;
        }

        return transform.position;
    }

    [Server]
    private UnitController ResolveCaster(UnitController killer, int resolvedTeamId)
    {
        if (casterMode == CasterMode.ObjectiveUnit)
        {
            return _objective != null ? _objective.ObjectiveUnit : null;
        }

        if (killer != null && !killer.IsDead)
        {
            if (resolvedTeamId < 0 || killer.team == resolvedTeamId)
            {
                return killer;
            }
        }

        if (resolvedTeamId < 0)
        {
            return _objective != null ? _objective.ObjectiveUnit : null;
        }

        var allUnits = FindObjectsByType<UnitController>();
        for (int i = 0; i < allUnits.Length; i++)
        {
            var unit = allUnits[i];
            if (unit == null) continue;
            if (unit.IsDead) continue;
            if (unit.team != resolvedTeamId) continue;
            return unit;
        }

        return null;
    }

    [Server]
    private List<UnitController> CollectTargets(Vector3 origin, int resolvedTeamId)
    {
        var targets = new List<UnitController>();
        var seen = new HashSet<UnitController>();

        int hitCount = Physics.OverlapSphereNonAlloc(
            origin,
            searchRadius,
            _overlapBuffer,
            unitLayer,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < hitCount; i++)
        {
            var hit = _overlapBuffer[i];
            if (hit == null) continue;

            var unit = hit.GetComponentInParent<UnitController>();
            if (unit == null) continue;
            if (unit.IsDead) continue;
            if (!PassesTeamFilter(unit, resolvedTeamId)) continue;
            if (!seen.Add(unit)) continue;

            targets.Add(unit);
        }

        if (maxTargets > 0 && targets.Count > maxTargets)
        {
            targets.RemoveRange(maxTargets, targets.Count - maxTargets);
        }

        return targets;
    }

    private bool PassesTeamFilter(UnitController unit, int resolvedTeamId)
    {
        switch (teamFilter)
        {
            case TeamFilterMode.ResolvedTeamAllies:
                return resolvedTeamId >= 0 && unit.team == resolvedTeamId;

            case TeamFilterMode.NonResolvedTeamEnemies:
                return resolvedTeamId >= 0 && unit.team >= 0 && unit.team != resolvedTeamId;

            case TeamFilterMode.AnyTeam:
                return true;

            default:
                return false;
        }
    }

    [Server]
    private IEnumerator ExecuteEffectChain(NetworkedSkillInstance host, UnitController caster, Vector3 origin, List<UnitController> targets)
    {
        var context = new CastContext(caster, host)
        {
            aimPoint = origin,
            aimRotation = Quaternion.LookRotation(Vector3.forward)
        };

        yield return effectChain.ExecuteCoroutine(context, targets);
    }

    [Server]
    private bool TryEnsureSkillHost(UnitController caster, out NetworkedSkillInstance host)
    {
        host = effectSkillHost;
        if (host != null)
        {
            return true;
        }

        if (NetworkManager.singleton == null || NetworkManager.singleton.spawnPrefabs == null)
        {
            return false;
        }

        GameObject skillPrefab = null;
        for (int i = 0; i < NetworkManager.singleton.spawnPrefabs.Count; i++)
        {
            var prefab = NetworkManager.singleton.spawnPrefabs[i];
            if (prefab != null && prefab.name == skillInstancePrefabName)
            {
                skillPrefab = prefab;
                break;
            }
        }

        if (skillPrefab == null)
        {
            return false;
        }

        var skillObject = Instantiate(skillPrefab, transform.position, Quaternion.identity, transform);
        host = skillObject.GetComponent<NetworkedSkillInstance>();
        if (host == null)
        {
            Destroy(skillObject);
            return false;
        }

        host.Initialize(string.Empty, caster);
        NetworkServer.Spawn(skillObject);
        effectSkillHost = host;
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.1f, 0.8f, 1f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, searchRadius);
    }
}