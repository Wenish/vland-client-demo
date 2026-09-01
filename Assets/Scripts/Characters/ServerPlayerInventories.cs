using System.Collections.Generic;
using ShadowInfection.Items;

/// <summary>
/// Server-side bag + equipped mirror per connection. Rebuilt from client snapshot on join.
/// </summary>
public static class ServerPlayerInventories
{
    private static readonly Dictionary<int, CharacterSaveData> ByConnectionId = new Dictionary<int, CharacterSaveData>(16);

    public static void SetFromSnapshot(int connectionId, CharacterInventorySnapshot snapshot)
    {
        if (connectionId < 0)
            return;

        var data = CharacterInventorySnapshot.ToCharacterData(snapshot);
        ByConnectionId[connectionId] = data;
    }

    public static bool TryGet(int connectionId, out CharacterSaveData inventory)
    {
        return ByConnectionId.TryGetValue(connectionId, out inventory) && inventory != null;
    }

    public static CharacterSaveData GetOrCreate(int connectionId, string characterId)
    {
        if (!ByConnectionId.TryGetValue(connectionId, out var inventory) || inventory == null)
        {
            inventory = new CharacterSaveData { Id = characterId ?? string.Empty };
            CharacterInventory.EnsureLists(inventory);
            ByConnectionId[connectionId] = inventory;
        }

        return inventory;
    }

    public static void Remove(int connectionId)
    {
        ByConnectionId.Remove(connectionId);
    }

    public static void Clear()
    {
        ByConnectionId.Clear();
    }
}
