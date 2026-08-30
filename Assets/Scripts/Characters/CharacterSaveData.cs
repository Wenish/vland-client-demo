using System;
using System.Collections.Generic;
using ShadowInfection.Items;

[Serializable]
public class CharacterSaveData
{
    public string Id;
    public string Name;
    public CharacterGender Gender;

    public string WeaponId;
    public string PassiveId;
    public string Normal1Id;
    public string Normal2Id;
    public string Normal3Id;
    public string UltimateId;

    // Future meta stubs (unused in v1)
    public List<string> UnlockedSkillIds = new List<string>();
    public List<string> ArmorSlotIds = new List<string>();

    public List<InventoryEntry> InventoryEquipment = new List<InventoryEntry>();
    public List<ItemStack> InventoryStacks = new List<ItemStack>();

    public LocalLoadout ToLoadout()
    {
        return new LocalLoadout
        {
            UnitName = Name,
            WeaponId = WeaponId ?? string.Empty,
            PassiveId = PassiveId ?? string.Empty,
            Normal1Id = Normal1Id ?? string.Empty,
            Normal2Id = Normal2Id ?? string.Empty,
            Normal3Id = Normal3Id ?? string.Empty,
            UltimateId = UltimateId ?? string.Empty
        };
    }

    public void ApplyLoadout(LocalLoadout loadout)
    {
        if (loadout == null)
            return;

        WeaponId = loadout.WeaponId ?? string.Empty;
        PassiveId = loadout.PassiveId ?? string.Empty;
        Normal1Id = loadout.Normal1Id ?? string.Empty;
        Normal2Id = loadout.Normal2Id ?? string.Empty;
        Normal3Id = loadout.Normal3Id ?? string.Empty;
        UltimateId = loadout.UltimateId ?? string.Empty;
    }

    public static CharacterSaveData CreateNew(string name, CharacterGender gender, LocalLoadout initialLoadout = null)
    {
        var character = new CharacterSaveData
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name ?? string.Empty,
            Gender = gender,
            UnlockedSkillIds = new List<string>(),
            ArmorSlotIds = new List<string>(),
            InventoryEquipment = new List<InventoryEntry>(),
            InventoryStacks = new List<ItemStack>()
        };

        if (initialLoadout != null)
            character.ApplyLoadout(initialLoadout);
        else
            character.ApplyLoadout(LocalLoadout.CreateBeginnerDefault(character.Name));

        return character;
    }
}
