using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class AreaZoneController : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnAreaZoneDataChanged))]
    public string areaZoneName;
    public AreaZoneData areaZoneData { get; private set; }
    public UnitController caster;
    public event Action<AreaZoneController> OnTick;
    public event Action<AreaZoneController> OnAreaZoneDestoryed;

    // Local-only visual spawned from AreaZoneData.prefabVisual (both server and clients)
    private GameObject _visualInstance;
    // Server-only lifetime/tick routine
    private Coroutine _zoneRoutine;

    [Server]
    public void SetAreaZoneName(string name)
    {
        areaZoneName = name;
        // Apply data and (re)start the zone timeline on the server
        SetAreaZoneData(name);
        RestartServerTimeline();
    }

    public void OnAreaZoneDataChanged(string oldName, string newName)
    {
        if (isServer) return;
        // Clients update visuals when the name (and therefore the data) changes
        SetAreaZoneData(newName);
    }

    private void SetAreaZoneData(string areaZoneName)
    {
        areaZoneData = DatabaseManager.Instance.areaZoneDatabase.GetAreaZoneByName(areaZoneName);
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        // Destroy any existing visual first
        if (_visualInstance != null)
        {
            Destroy(_visualInstance);
            _visualInstance = null;
        }

        // Spawn the new visual as a child if configured
        if (areaZoneData != null && areaZoneData.prefabVisual != null)
        {
            _visualInstance = Instantiate(areaZoneData.prefabVisual, transform);
            _visualInstance.name = $"{areaZoneData.areaZoneName}_Visual";
        }
    }

    [Server]
    private void RestartServerTimeline()
    {
        if (_zoneRoutine != null)
        {
            StopCoroutine(_zoneRoutine);
            _zoneRoutine = null;
        }

        if (areaZoneData == null)
            return;

        _zoneRoutine = StartCoroutine(RunZoneTimeline());
    }

    // SERVER: Drives ticking and lifetime based on AreaZoneData
    [Server]
    private IEnumerator RunZoneTimeline()
    {
        float duration = Mathf.Max(0f, areaZoneData.duration);
        int ticks = Mathf.Max(0, areaZoneData.tickCount);
        var mode = areaZoneData.tickMode;

        // Build the tick schedule (seconds from start)
        List<float> tickTimes = BuildTickSchedule(duration, ticks, mode);

        // Frame-driven scheduler that catches up missed ticks after hitches
        float startTime = Time.time;
        int index = 0;
        const float epsilon = 0.0001f; // handle float imprecision

        while (true)
        {
            float elapsed = Time.time - startTime;

            // Fire all ticks that should have occurred up to now (catch-up)
            while (index < tickTimes.Count && tickTimes[index] <= elapsed + epsilon)
            {
                ServerTick();
                index++;
            }

            // End when duration elapsed; flush any remaining ticks before destroy
            if (elapsed >= duration - epsilon)
            {
                while (index < tickTimes.Count)
                {
                    ServerTick();
                    index++;
                }
                break;
            }

            // Wait until next frame to re-evaluate; ensures we don't miss ticks on long frames
            yield return null;
        }

        DestroySelf();
    }

    // Create evenly distributed tick times according to the selected mode
    private static List<float> BuildTickSchedule(float duration, int ticks, AreaZoneData.TickMode mode)
    {
        var times = new List<float>();

        if (ticks <= 0 || duration <= 0f)
        {
            // No ticking or zero-duration: no scheduled ticks
            return times;
        }

        switch (mode)
        {
            case AreaZoneData.TickMode.EvenlySpacedEndAligned:
                // Ticks occur at duration/ticks, 2*duration/ticks, ..., duration
                for (int i = 1; i <= ticks; i++)
                {
                    float t = duration * i / ticks;
                    times.Add(t);
                }
                break;

            case AreaZoneData.TickMode.IncludeStartAndEnd:
                // Include both start (0) and end (duration) among 'ticks' count
                if (ticks == 1)
                {
                    // Single tick at start
                    times.Add(0f);
                }
                else
                {
                    for (int i = 0; i < ticks; i++)
                    {
                        float denom = Mathf.Max(1, ticks - 1);
                        float t = duration * i / denom; // 0 ... duration
                        times.Add(t);
                    }
                }
                break;
        }

        return times;
    }

    [Server]
    public void DestroySelf()
    {
        OnAreaZoneDestoryed?.Invoke(this);
        NetworkServer.Destroy(gameObject);
    }

    [Server]
    private void ServerTick()
    {
        OnTick?.Invoke(this);
    }

    [ServerCallback]
    private void OnDisable()
    {
        if (_zoneRoutine != null)
        {
            StopCoroutine(_zoneRoutine);
            _zoneRoutine = null;
        }
    }
}
