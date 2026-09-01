using Mirror;
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

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
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

        if (deferredSyncCoroutine != null)
        {
            StopCoroutine(deferredSyncCoroutine);
            deferredSyncCoroutine = null;
        }
    }

    private void OnDestroy()
    {
        if (Local == this)
            Local = null;
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

        if (deferredSyncCoroutine != null)
            StopCoroutine(deferredSyncCoroutine);

        deferredSyncCoroutine = StartCoroutine(DeferredSyncWhenUnitReady());
    }

    private System.Collections.IEnumerator DeferredSyncWhenUnitReady()
    {
        while (isLocalPlayer)
        {
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
        ApplyEquippedMainHandToUnit();
    }

    private void ApplyEquippedMainHandToUnit()
    {
        var unit = playerInput != null ? playerInput.myUnit?.GetComponent<UnitController>() : null;
        if (unit == null || connectionToClient == null)
            return;

        if (!ServerPlayerInventories.TryGet(connectionToClient.connectionId, out var inventory))
            return;

        var catalog = ResolveCatalog();
        if (catalog == null)
            return;

        if (!CharacterInventory.TryGetEquipped(inventory, ItemSlot.MainHand, out var entry)
            || string.IsNullOrWhiteSpace(entry.itemId)
            || !catalog.TryGet(entry.itemId, out var definition)
            || definition.weaponData == null)
            return;

        unit.EquipWeapon(definition.weaponData.weaponName);
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
