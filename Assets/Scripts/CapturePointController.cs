using UnityEngine;
using Mirror;
using System;
using System.Collections.Generic;

public class CapturePointController : NetworkBehaviour
{
    [SyncVar(hook = nameof(HookOnControllingTeamChanged))]
    public int controllingTeam = -1; // -1 for neutral, 0 for team A, 1 for team B etc.

    public event Action<(int oldTeam, int newTeam)> OnControllingTeamChanged = delegate { };

    [SyncVar(hook = nameof(HookOnCaptureProgressChanged))]
    public float captureProgress = 0f; // 0 to 100

    public event Action<float> OnCaptureProgressChanged = delegate { };

    [SyncVar(hook = nameof(HookOnContenderTeamChanged))]
    public int contenderTeam = -1;

    public event Action<(int oldTeam, int newTeam)> OnContenderTeamChanged = delegate { };

    [Header("Capture Settings")]
    [Tooltip("Capture progress changed per second while a team is actively capturing.")]
    [Min(0.01f)]
    public float captureRate = 10f;

    [Tooltip("Radius used to count nearby units for capture.")]
    [Min(0.1f)]
    public float captureRadius = 5f;

    [Tooltip("How often capture logic is evaluated on the server.")]
    [Min(0.1f)]
    [SerializeField] private float captureTickInterval = 1f;

    [Tooltip("Physics layer mask used when querying units in the capture radius.")]
    [SerializeField] private LayerMask unitLayerMask = ~0;

    [Tooltip("Initial size of the non-alloc overlap buffer. It grows automatically if needed.")]
    [Min(8)]
    [SerializeField] private int initialOverlapBufferSize = 64;

    private Collider[] overlapBuffer;
    private readonly Dictionary<int, UnitController> unitByColliderId = new Dictionary<int, UnitController>(128);
    private readonly HashSet<UnitController> uniqueUnitsInTick = new HashSet<UnitController>();
    private readonly Dictionary<int, int> teamCounts = new Dictionary<int, int>(8);

    private void HookOnControllingTeamChanged(int oldTeam, int newTeam)
    {
        if (isServer) return;

        OnControllingTeamChanged((oldTeam, newTeam));
    }

    private void HookOnCaptureProgressChanged(float oldProgress, float newProgress)
    {
        if (isServer) return;

        OnCaptureProgressChanged(newProgress);
    }

    private void HookOnContenderTeamChanged(int oldTeam, int newTeam)
    {
        if (isServer) return;

        OnContenderTeamChanged((oldTeam, newTeam));
    }

    [Server]
    private void SetControllingTeam(int newTeam)
    {
        if (controllingTeam == newTeam)
        {
            return;
        }

        int oldTeam = controllingTeam;
        controllingTeam = newTeam;
        OnControllingTeamChanged((oldTeam, newTeam));
    }

    [Server]
    private void SetCaptureProgress(float newProgress)
    {
        float clampedProgress = Mathf.Clamp(newProgress, 0f, 100f);
        if (Mathf.Approximately(captureProgress, clampedProgress))
        {
            return;
        }

        captureProgress = clampedProgress;
        OnCaptureProgressChanged(clampedProgress);
    }

    [Server]
    private void SetContenderTeam(int newTeam)
    {
        if (contenderTeam == newTeam)
        {
            return;
        }

        int oldTeam = contenderTeam;
        contenderTeam = newTeam;
        OnContenderTeamChanged((oldTeam, newTeam));
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        int bufferSize = Mathf.Max(8, initialOverlapBufferSize);
        overlapBuffer = new Collider[bufferSize];
        InvokeRepeating(nameof(ServerCaptureTick), captureTickInterval, captureTickInterval);
    }

    public override void OnStopServer()
    {
        CancelInvoke(nameof(ServerCaptureTick));
        base.OnStopServer();
    }

    [Server]
    private void ServerCaptureTick()
    {
        int dominantTeam = FindDominantTeam();
        float tickCaptureDelta = captureRate * captureTickInterval;

        if (contenderTeam < 0)
        {
            if (dominantTeam < 0 || dominantTeam == controllingTeam)
            {
                return;
            }

            SetContenderTeam(dominantTeam);
            SetCaptureProgress(0f);
        }

        if (dominantTeam == contenderTeam)
        {
            SetCaptureProgress(captureProgress + tickCaptureDelta);

            if (captureProgress >= 100f)
            {
                SetControllingTeam(contenderTeam);
                SetContenderTeam(-1);
                SetCaptureProgress(100f);
            }

            return;
        }

        SetCaptureProgress(captureProgress - tickCaptureDelta);
        if (captureProgress <= 0f)
        {
            SetCaptureProgress(0f);

            if (dominantTeam < 0 || dominantTeam == controllingTeam)
            {
                SetContenderTeam(-1);
                return;
            }

            SetContenderTeam(dominantTeam);
        }
    }

    [Server]
    private int FindDominantTeam()
    {
        uniqueUnitsInTick.Clear();
        teamCounts.Clear();

        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            captureRadius,
            overlapBuffer,
            unitLayerMask,
            QueryTriggerInteraction.Collide);

        while (hitCount >= overlapBuffer.Length)
        {
            overlapBuffer = new Collider[overlapBuffer.Length * 2];
            hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                captureRadius,
                overlapBuffer,
                unitLayerMask,
                QueryTriggerInteraction.Collide);
        }

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = overlapBuffer[i];
            if (hit == null)
            {
                continue;
            }

            UnitController unit = ResolveUnit(hit);
            if (unit == null || unit.IsDead)
            {
                continue;
            }

            if (!uniqueUnitsInTick.Add(unit))
            {
                continue;
            }

            int teamId = unit.team;
            if (teamId < 0)
            {
                continue;
            }

            if (teamCounts.TryGetValue(teamId, out int teamCount))
            {
                teamCounts[teamId] = teamCount + 1;
            }
            else
            {
                teamCounts[teamId] = 1;
            }
        }

        int dominantTeam = -1;
        int highestCount = 0;
        bool hasTie = false;

        foreach (KeyValuePair<int, int> pair in teamCounts)
        {
            if (pair.Value > highestCount)
            {
                highestCount = pair.Value;
                dominantTeam = pair.Key;
                hasTie = false;
            }
            else if (pair.Value == highestCount)
            {
                hasTie = true;
            }
        }

        if (highestCount <= 0 || hasTie)
        {
            return -1;
        }

        return dominantTeam;
    }

    [Server]
    private UnitController ResolveUnit(Collider hit)
    {
        int colliderId = hit.GetInstanceID();

        if (unitByColliderId.TryGetValue(colliderId, out UnitController cachedUnit))
        {
            if (cachedUnit != null)
            {
                return cachedUnit;
            }

            unitByColliderId.Remove(colliderId);
        }

        UnitController resolvedUnit = hit.GetComponentInParent<UnitController>();
        if (resolvedUnit != null)
        {
            unitByColliderId[colliderId] = resolvedUnit;
        }

        return resolvedUnit;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, captureRadius);
    }
#endif
}