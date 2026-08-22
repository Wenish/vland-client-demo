using System.Collections.Generic;

/// <summary>
/// Server-side character selection that survives Lobby → gameplay scene swaps
/// (RoomPlayer objects are replaced by the game Player prefab).
/// </summary>
public static class ServerCharacterSelections
{
    public struct Selection
    {
        public string CharacterId;
        public string CharacterName;
        public CharacterGender Gender;
        public string ModelName;

        public bool IsValid => !string.IsNullOrEmpty(CharacterId) && !string.IsNullOrEmpty(ModelName);
    }

    private static readonly Dictionary<int, Selection> ByConnectionId = new Dictionary<int, Selection>(16);

    public static void Set(int connectionId, Selection selection)
    {
        ByConnectionId[connectionId] = selection;
    }

    public static bool TryGet(int connectionId, out Selection selection)
    {
        return ByConnectionId.TryGetValue(connectionId, out selection) && selection.IsValid;
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
