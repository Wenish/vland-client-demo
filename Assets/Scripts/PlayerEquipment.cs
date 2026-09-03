using Mirror;
using MyGame.Events;
using R3;
using ShadowInfection.DI;
using ShadowInfection.Items;
using UnityEngine;

/// <summary>
/// Syncs character bag + equipped slots to the server during a match.
/// Client roster remains in PlayerPrefs via CharacterManager (survives disconnect).
/// </summary>
public class PlayerEquipment : NetworkBehaviour
{
    public static PlayerEquipment Local { get; private set; }

    private PlayerInput playerInput;
    private Coroutine deferredSyncCoroutine;
    private DisposableBag serverSubscriptions;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        GameMessages.Subscribe<PlayerUnitSpawnedEvent>(ref serverSubscriptions, OnPlayerUnitSpawned);
    }

    public override void OnStartLocalPlayer()
    {
        Local = this;
        RestartDeferredSync();
    }

    public override void OnStopLocalPlayer()
    {
        if (Local == this)
            Local = null;

        StopDeferredSync();
    }

    private void OnDestroy()
    {
        if (Local == this)
            Local = null;

        StopDeferredSync();
        serverSubscriptions.Dispose();
        serverSubscriptions = new DisposableBag();
    }

    public static bool ShouldSyncToServer()
    {
        return NetworkClient.active && NetworkClient.isConnected;
    }

    public void RequestSyncInventory()
    {
        if (!isLocalPlayer)
            return;

        var snapshot = BuildLocalSnapshot();
        if (string.IsNullOrWhiteSpace(snapshot.characterId))
            return;

        CmdSyncCharacterInventory(snapshot);
    }

    private void RestartDeferredSync()
    {
        if (!isLocalPlayer)
            return;

        StopDeferredSync();
        deferredSyncCoroutine = StartCoroutine(DeferredSyncWhenUnitReady());
    }

    private void StopDeferredSync()
    {
        if (deferredSyncCoroutine == null)
            return;

        StopCoroutine(deferredSyncCoroutine);
        deferredSyncCoroutine = null;
    }

    private System.Collections.IEnumerator DeferredSyncWhenUnitReady()
    {
        while (isLocalPlayer)
        {
            EnsurePlayerInput();
            if (playerInput != null && playerInput.myUnit != null)
                break;

            yield return null;
        }

        if (!isLocalPlayer)
        {
            deferredSyncCoroutine = null;
            yield break;
        }

        RequestSyncInventory();
        deferredSyncCoroutine = null;
    }

    private void EnsurePlayerInput()
    {
        if (playerInput != null)
            return;

        playerInput = GetComponent<PlayerInput>();
    }

    [Server]
    private void OnPlayerUnitSpawned(PlayerUnitSpawnedEvent e)
    {
        if (connectionToClient == null || e == null || e.ConnectionId != connectionToClient.connectionId)
            return;

        ApplyEquippedHandsToUnit(e.Unit != null ? e.Unit.GetComponent<UnitController>() : null);
    }

    private static CharacterInventorySnapshot BuildLocalSnapshot()
    {
        var active = GameServices.Characters?.GetActive();
        return CharacterInventorySnapshot.From(active);
    }

    private static IItemCatalog ResolveCatalog()
    {
        var items = GameServices.Databases?.Items;
        return items != null ? new DatabaseItemCatalog(items) : null;
    }

    [Command]
    private void CmdSyncCharacterInventory(CharacterInventorySnapshot snapshot)
    {
        if (connectionToClient == null)
            return;

        var catalog = ResolveCatalog();
        if (!CharacterInventoryValidator.TryValidate(snapshot, catalog, out var error))
        {
            var serverSnapshot = ServerPlayerInventories.TryGet(connectionToClient.connectionId, out var existing)
                ? CharacterInventorySnapshot.From(existing)
                : default;
            TargetRejectInventory(connectionToClient, serverSnapshot, error ?? "Invalid inventory.");
            return;
        }

        ServerPlayerInventories.SetFromSnapshot(connectionToClient.connectionId, snapshot);
        ApplyEquippedHandsToUnit();
    }

    [Server]
    private void ApplyEquippedHandsToUnit(UnitController unit = null)
    {
        if (connectionToClient == null)
            return;

        if (unit == null)
            unit = ResolveControlledUnit();

        ServerApplyHeldItems(connectionToClient.connectionId, unit);
    }

    [Server]
    private UnitController ResolveControlledUnit()
    {
        EnsurePlayerInput();
        var fromInput = playerInput != null ? playerInput.myUnit : null;
        if (fromInput != null)
            return fromInput.GetComponent<UnitController>();

        var spawned = GameServices.PlayerUnits != null
            ? GameServices.PlayerUnits.GetPlayerUnit(connectionToClient.connectionId)
            : null;
        return spawned != null ? spawned.GetComponent<UnitController>() : null;
    }

    [Server]
    public static void ServerApplyHeldItems(int connectionId, UnitController unit)
    {
        if (unit == null)
            return;

        if (!ServerPlayerInventories.TryGet(connectionId, out var inventory))
            return;

        var catalog = ResolveCatalog();
        if (catalog == null)
            return;

        var mainId = string.Empty;
        if (CharacterInventory.TryGetEquipped(inventory, ItemSlot.MainHand, out var mainEntry)
            && !string.IsNullOrWhiteSpace(mainEntry.itemId)
            && catalog.TryGet(mainEntry.itemId, out var mainDefinition)
            && mainDefinition.weaponData != null)
        {
            mainId = mainEntry.itemId;
        }

        var offId = string.Empty;
        if (CharacterInventory.TryGetEquipped(inventory, ItemSlot.OffHand, out var offEntry)
            && !string.IsNullOrWhiteSpace(offEntry.itemId)
            && catalog.TryGet(offEntry.itemId, out var offDefinition)
            && offDefinition.weaponData != null)
        {
            offId = offEntry.itemId;
        }

        var previousType = unit.currentWeapon != null
            ? (WeaponType?)unit.currentWeapon.weaponType
            : null;

        unit.EquipHeldItems(mainId, offId);

        var nextType = unit.currentWeapon != null
            ? (WeaponType?)unit.currentWeapon.weaponType
            : WeaponType.Unarmed;

        if (previousType != nextType)
            unit.unitMediator?.Skills?.RemoveSkillsIncompatibleWithWeapon(nextType);
    }

    [TargetRpc]
    private void TargetRejectInventory(NetworkConnection target, CharacterInventorySnapshot snapshot, string error)
    {
        ApplySnapshotLocally(snapshot);
        if (!string.IsNullOrEmpty(error))
            Debug.LogWarning($"Inventory sync rejected: {error}");
    }

    private static void ApplySnapshotLocally(CharacterInventorySnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot.characterId))
            return;

        var characters = GameServices.Characters;
        var active = characters?.GetActive();
        if (active == null || active.Id != snapshot.characterId)
            return;

        CharacterInventorySnapshot.ApplyTo(active, snapshot);
        characters.PersistRoster();
    }
}
