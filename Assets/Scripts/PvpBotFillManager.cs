using System.Collections.Generic;
using Mirror;
using UnityEngine;
using System.Linq;

[DisallowMultipleComponent]
public class PvpBotFillManager : NetworkBehaviour
{
    [Header("Bot Fill")]
    [SerializeField] private bool enableBotFill = true;
    [SerializeField, Min(0)] private int desiredTotalPlayers = 6;
    [SerializeField, Min(0)] private int maxBots = 6;
    [SerializeField] private string botUnitName = "Player";
    [SerializeField, Min(0.2f)] private float rebalanceIntervalSeconds = 1.5f;
    [SerializeField] private bool cullBotsWhenHumansJoin = true;

    private readonly List<int> _botConnectionIdsBuffer = new List<int>(16);
    private float _nextReconcileAt;

    public override void OnStartServer()
    {
        base.OnStartServer();
        _nextReconcileAt = Time.time + 0.5f;
    }

    [Server]
    public void ServerConfigure(int targetPlayers, int maxBotPlayers, string unitName = "Player")
    {
        desiredTotalPlayers = Mathf.Max(0, targetPlayers);
        maxBots = Mathf.Max(0, maxBotPlayers);

        if (!string.IsNullOrWhiteSpace(unitName))
        {
            botUnitName = unitName;
        }
    }

    [ServerCallback]
    private void Update()
    {
        if (!enableBotFill)
        {
            return;
        }

        if (PlayerUnitsManager.Instance == null)
        {
            return;
        }

        if (Time.time < _nextReconcileAt)
        {
            return;
        }

        _nextReconcileAt = Time.time + rebalanceIntervalSeconds;
        ReconcileBotPopulation();
    }

    [Server]
    private void ReconcileBotPopulation()
    {
        int humanPlayers = PlayerUnitsManager.Instance.GetHumanPlayerCount();
        int currentBots = PlayerUnitsManager.Instance.GetBotPlayerCount();
        int targetBots = Mathf.Clamp(desiredTotalPlayers - humanPlayers, 0, maxBots);

        if (currentBots < targetBots)
        {
            int toSpawn = targetBots - currentBots;
            for (int i = 0; i < toSpawn; i++)
            {
                var spawnedUnit = PlayerUnitsManager.Instance.SpawnBotPlayerUnit(botUnitName);
                if (spawnedUnit == null)
                {
                    continue;
                }

                ApplyRandomLoadout(spawnedUnit);

                var botBrain = spawnedUnit.GetComponent<PvpBotBrain>();
                if (botBrain == null)
                {
                    botBrain = spawnedUnit.gameObject.AddComponent<PvpBotBrain>();
                }
            }

            return;
        }

        if (currentBots > targetBots && cullBotsWhenHumansJoin)
        {
            int toDespawn = currentBots - targetBots;
            RemoveBots(toDespawn);
        }
    }

    [Server]
    private void ApplyRandomLoadout(GameObject botUnit)
    {
        if (botUnit == null)
        {
            return;
        }

        var unitController = botUnit.GetComponent<UnitController>();
        var skillSystem = botUnit.GetComponent<SkillSystem>();

        if (unitController == null || skillSystem == null)
        {
            return;
        }

        var skillDb = DatabaseManager.Instance?.skillDatabase;
        var weaponDb = DatabaseManager.Instance?.weaponDatabase;

        if (skillDb == null || weaponDb == null)
        {
            return;
        }

        var validWeapons = new List<WeaponData>();
        foreach (var weapon in weaponDb.allWeapons)
        {
            if (weapon == null || weapon.npcOnly)
            {
                continue;
            }

            validWeapons.Add(weapon);
        }

        if (validWeapons.Count > 0)
        {
            var randomWeapon = validWeapons[Random.Range(0, validWeapons.Count)];
            unitController.EquipWeapon(randomWeapon.weaponName);
        }

        var passiveSkills = new List<SkillData>();
        var normalSkills = new List<SkillData>();
        var ultimateSkills = new List<SkillData>();

        foreach (var skill in skillDb.allSkills)
        {
            if (skill == null || skill.npcOnly)
            {
                continue;
            }

            switch (skill.skillType)
            {
                case SkillType.Passive:
                    passiveSkills.Add(skill);
                    break;
                case SkillType.Normal:
                    normalSkills.Add(skill);
                    break;
                case SkillType.Ultimate:
                    ultimateSkills.Add(skill);
                    break;
            }
        }

        var selectedPassive = (passiveSkills.Count > 0)
            ? passiveSkills[Random.Range(0, passiveSkills.Count)].skillName
            : string.Empty;

        var selectedNormals = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            selectedNormals.Add((normalSkills.Count > 0)
                ? normalSkills[Random.Range(0, normalSkills.Count)].skillName
                : string.Empty);
        }

        var selectedUltimate = (ultimateSkills.Count > 0)
            ? ultimateSkills[Random.Range(0, ultimateSkills.Count)].skillName
            : string.Empty;

        skillSystem.ReplaceLoadout(new[] { selectedPassive }, selectedNormals, new[] { selectedUltimate });
    }

    [Server]
    private void RemoveBots(int count)
    {
        if (count <= 0)
        {
            return;
        }

        PlayerUnitsManager.Instance.GetBotConnectionIds(_botConnectionIdsBuffer);
        if (_botConnectionIdsBuffer.Count == 0)
        {
            return;
        }

        int removed = 0;

        for (int i = 0; i < _botConnectionIdsBuffer.Count && removed < count; i++)
        {
            int connectionId = _botConnectionIdsBuffer[i];
            var unit = PlayerUnitsManager.Instance.GetPlayerUnit(connectionId);
            if (unit == null)
            {
                continue;
            }

            var unitController = unit.GetComponent<UnitController>();
            if (unitController != null && !unitController.IsDead)
            {
                continue;
            }

            if (PlayerUnitsManager.Instance.DespawnBotPlayerUnit(connectionId))
            {
                removed++;
            }
        }

        for (int i = 0; i < _botConnectionIdsBuffer.Count && removed < count; i++)
        {
            int connectionId = _botConnectionIdsBuffer[i];
            if (PlayerUnitsManager.Instance.DespawnBotPlayerUnit(connectionId))
            {
                removed++;
            }
        }
    }
}
