using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "CastleSiegeMapConfig", menuName = "Game/CastleSiege/MapConfig")]
public class CastleSiegeMapConfig : ScriptableObject
{
    [Serializable]
    public struct SpawnPointData
    {
        public Vector3 Position;
        public Vector3 RotationEuler;

        public Quaternion Rotation => Quaternion.Euler(RotationEuler);
    }

    [Serializable]
    public class TeamConfig
    {
        [Min(0)] public int TeamId;
        public UnitData LordUnit;
        public SpawnPointData LordSpawn;
        public List<SpawnPointData> PlayerSpawnPoints = new List<SpawnPointData>();
    }

    [Header("Team Setup")]
    [Min(2)] public int TeamCount = 2;
    public List<TeamConfig> Teams = new List<TeamConfig>();

    [Header("Match Flow")]
    [Min(0f)] public float WarmupSeconds = 15f;
    [Min(0f)] public float StartCountdownSeconds = 5f;

    [Header("Respawn Scaling")]
    [Min(0f)] public float BaseRespawnSeconds = 10f;
    [Min(0f)] public float ExtraRespawnPerMinute = 1f;
    [Min(0f)] public float MinRespawnSeconds = 5f;
    [Min(0f)] public float MaxRespawnSeconds = 30f;

    [Header("Spawn Offset")]
    [Min(0f)] public float SpawnOffsetRadiusStart = 0.75f;
    [Min(0f)] public float SpawnOffsetRadiusStep = 0.5f;
    [Min(1)] public int SpawnOffsetMaxAttempts = 12;
    public bool RequireWalkable = false;

    public bool Validate(out string errorMessage)
    {
        var errors = new List<string>();

        if (TeamCount < 2)
        {
            errors.Add("TeamCount must be >= 2.");
        }

        if (Teams == null)
        {
            errors.Add("Teams list is null.");
        }
        else
        {
            if (Teams.Count != TeamCount)
            {
                errors.Add($"Teams.Count ({Teams.Count}) must match TeamCount ({TeamCount}).");
            }

            var ids = Teams.Select(team => team.TeamId).ToList();
            if (ids.Distinct().Count() != ids.Count)
            {
                errors.Add("TeamId values must be unique.");
            }

            for (int i = 0; i < Teams.Count; i++)
            {
                var team = Teams[i];
                if (team.TeamId < 0 || team.TeamId >= TeamCount)
                {
                    errors.Add($"Team index {i} has invalid TeamId {team.TeamId}. Must be in [0..{TeamCount - 1}].");
                }

                if (team.LordUnit == null)
                {
                    errors.Add($"Team {team.TeamId} has no LordUnit assigned.");
                }

                if (team.PlayerSpawnPoints == null || team.PlayerSpawnPoints.Count < 1)
                {
                    errors.Add($"Team {team.TeamId} must have at least one player spawn point.");
                }
            }
        }

        if (MinRespawnSeconds > MaxRespawnSeconds)
        {
            errors.Add("MinRespawnSeconds must be <= MaxRespawnSeconds.");
        }

        errorMessage = string.Join(" ", errors);
        return errors.Count == 0;
    }

    private void OnValidate()
    {
        if (!Validate(out string errorMessage))
        {
            Debug.LogWarning($"[CastleSiegeMapConfig] Validation issues in '{name}': {errorMessage}", this);
        }
    }
}