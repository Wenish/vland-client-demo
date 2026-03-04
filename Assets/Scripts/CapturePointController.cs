using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class CapturePoint : NetworkBehaviour
{
    [SyncVar]
    public int controllingTeam = -1; // -1 for neutral, 0 for team A, 1 for team B

    [SyncVar]
    public float captureProgress = 0f; // 0 to 100

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
    private int neutralCaptureTeam = -1;

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
        if (dominantTeam < 0)
        {
            return;
        }

        float tickCaptureDelta = captureRate * captureTickInterval;

        if (controllingTeam < 0)
        {
            if (neutralCaptureTeam < 0)
            {
                neutralCaptureTeam = dominantTeam;
            }

            if (neutralCaptureTeam == dominantTeam)
            {
                captureProgress = Mathf.Clamp(captureProgress + tickCaptureDelta, 0f, 100f);
                if (captureProgress >= 100f)
                {
                    controllingTeam = neutralCaptureTeam;
                    neutralCaptureTeam = -1;
                    captureProgress = 100f;
                }
            }
            else
            {
                captureProgress = Mathf.Clamp(captureProgress - tickCaptureDelta, 0f, 100f);
                if (captureProgress <= 0f)
                {
                    captureProgress = 0f;
                    neutralCaptureTeam = dominantTeam;
                }
            }

            return;
        }

        if (controllingTeam == dominantTeam)
        {
            neutralCaptureTeam = -1;
            captureProgress = Mathf.Clamp(captureProgress + tickCaptureDelta, 0f, 100f);
            return;
        }

        captureProgress = Mathf.Clamp(captureProgress - tickCaptureDelta, 0f, 100f);
        if (captureProgress <= 0f)
        {
            controllingTeam = -1;
            captureProgress = 0f;
            neutralCaptureTeam = dominantTeam;
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