using System;
using System.Collections.Generic;

[Serializable]
public class CharacterRosterSave
{
    public const int CurrentVersion = 2;

    public int Version;
    public string ActiveCharacterId = string.Empty;
    public List<CharacterSaveData> Characters = new List<CharacterSaveData>();
}
