using System.Collections.Generic;
using Mirror;
using UnityEngine;
using System.Linq;
using ShadowInfection.DI;
using ShadowInfection.Items;

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

        if (GameServices.PlayerUnits == null)
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
        int humanPlayers = GameServices.PlayerUnits.GetHumanPlayerCount();
        int currentBots = GameServices.PlayerUnits.GetBotPlayerCount();
        int targetBots = Mathf.Clamp(desiredTotalPlayers - humanPlayers, 0, maxBots);

        if (currentBots < targetBots)
        {
            int toSpawn = targetBots - currentBots;
            for (int i = 0; i < toSpawn; i++)
            {
                var spawnedUnit = GameServices.PlayerUnits.SpawnBotPlayerUnit(botUnitName);
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

        var databases = GameServices.Databases;
        var skillDb = databases?.Skills;
        var items = databases?.Items;

        if (skillDb == null || items == null)
        {
            return;
        }

        var validItems = new List<ItemDefinition>();
        foreach (var item in items.All)
        {
            if (item == null || item.weaponData == null || item.weaponData.npcOnly)
                continue;
            if (!ItemRules.CanEquipToSlot(item, ItemSlot.MainHand, item.weaponData.weaponType, null))
                continue;
            validItems.Add(item);
        }

        ItemDefinition mainItem = null;
        if (validItems.Count > 0)
            mainItem = validItems[Random.Range(0, validItems.Count)];

        var offItemId = string.Empty;
        if (mainItem != null && ItemRules.IsDualWieldWeapon(mainItem.weaponData.weaponType))
            offItemId = mainItem.itemId;

        unitController.EquipHeldItems(
            mainItem != null ? mainItem.itemId : string.Empty,
            offItemId);

        var weaponType = unitController.currentWeapon != null
            ? (WeaponType?)unitController.currentWeapon.weaponType
            : WeaponType.Unarmed;

        var passiveSkills = new List<SkillData>();
        var normalSkills = new List<SkillData>();
        var ultimateSkills = new List<SkillData>();

        foreach (var skill in skillDb.allSkills)
        {
            if (skill == null || skill.npcOnly || !skill.CanBeUsedWithWeapon(weaponType))
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

        GameServices.PlayerUnits.GetBotConnectionIds(_botConnectionIdsBuffer);
        if (_botConnectionIdsBuffer.Count == 0)
        {
            return;
        }

        int removed = 0;

        for (int i = 0; i < _botConnectionIdsBuffer.Count && removed < count; i++)
        {
            int connectionId = _botConnectionIdsBuffer[i];
            var unit = GameServices.PlayerUnits.GetPlayerUnit(connectionId);
            if (unit == null)
            {
                continue;
            }

            var unitController = unit.GetComponent<UnitController>();
            if (unitController != null && !unitController.IsDead)
            {
                continue;
            }

            if (GameServices.PlayerUnits.DespawnBotPlayerUnit(connectionId))
            {
                removed++;
            }
        }

        for (int i = 0; i < _botConnectionIdsBuffer.Count && removed < count; i++)
        {
            int connectionId = _botConnectionIdsBuffer[i];
            if (GameServices.PlayerUnits.DespawnBotPlayerUnit(connectionId))
            {
                removed++;
            }
        }
    }
}
