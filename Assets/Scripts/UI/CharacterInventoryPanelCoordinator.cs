using ShadowInfection.UI.CharacterWindow;
using ShadowInfection.UI.InventoryWindow;

namespace ShadowInfection.UI
{
    internal sealed class CharacterInventoryPanelCoordinator
    {
        public InventoryWindowPresenter InventoryPresenter { get; private set; }
        public CharacterWindowPresenter CharacterPresenter { get; private set; }

        public void RegisterInventory(InventoryWindowPresenter presenter)
        {
            InventoryPresenter = presenter;
            CharacterPresenter?.LinkInventoryPresenter(presenter);
            InventoryPresenter?.LinkCharacterPresenter(CharacterPresenter);
        }

        public void RegisterCharacter(CharacterWindowPresenter presenter)
        {
            CharacterPresenter = presenter;
            CharacterPresenter.LinkInventoryPresenter(InventoryPresenter);
            InventoryPresenter?.LinkCharacterPresenter(presenter);
        }
    }
}
