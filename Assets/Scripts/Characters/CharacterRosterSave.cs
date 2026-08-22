using System;
using System.Collections.Generic;

[Serializable]
public class CharacterRosterSave
{
    public string ActiveCharacterId = string.Empty;
    public List<CharacterSaveData> Characters = new List<CharacterSaveData>();
}
