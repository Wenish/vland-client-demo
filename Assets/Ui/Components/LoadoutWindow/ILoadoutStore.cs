namespace ShadowInfection.UI.LoadoutWindow
{
    internal interface ILoadoutStore
    {
        LocalLoadout Get();
        void Set(LocalLoadout loadout);
    }
}
