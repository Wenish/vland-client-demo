namespace ShadowInfection.Input
{
    public enum PlayerActionGroup
    {
        Movement = 0,
        Combat = 1,
        Modifiers = 2,
        World = 3,
        Camera = 4,
        Interface = 5
    }

    public enum PlayerActionId
    {
        None = 0,
        MoveForward = 1,
        MoveBackward = 2,
        MoveLeft = 3,
        MoveRight = 4,
        Attack = 5,
        Skill1 = 6,
        Skill2 = 7,
        Skill3 = 8,
        Ultimate = 9,
        CancelCast = 10,
        Interrupt = 11,
        Ping = 12,
        SelfTargetModifier = 13,
        CastModifier = 14,
        Interact = 15,
        CameraFollow = 16,
        CameraFixed = 17,
        SpectatePrevious = 18,
        SpectateNext = 19,
        Loadout = 20,
        Leaderboard = 21,
        VendorTabs = 22,
        Menu = 23,
        SelectTarget = 24,
        Inventory = 25
    }

    public enum InputBindingSlot
    {
        Primary = 0,
        Secondary = 1,
        Gamepad = 2
    }
}
