public static class NetworkInventorySync
{
    public static void NotifyLocalInventoryChanged()
    {
        if (!PlayerEquipment.ShouldSyncToServer())
            return;

        PlayerEquipment.Local?.RequestSyncInventory();
    }
}
