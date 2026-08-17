/// <summary>
/// Project-database URLs for SVG icons imported as Texture2D (fileID 2800000).
/// Use in UXML/USS via DesignTokens.uss variables, or in C# with StyleBackground/Background.
/// </summary>
public static class UiIconPaths
{
    private const string Base = "project://database/Assets/Art/Ui/Icons";

    public static string Icon(string name, string guid) =>
        $"{Base}/{name}.svg?fileID=2800000&guid={guid}&type=3#{name}";

    public const string Play = "project://database/Assets/Art/Ui/Icons/icon-play.svg?fileID=2800000&guid=e1a1b2c3d4e5f60718293a4b5c6d7ea2&type=3#icon-play";
    public const string Settings = "project://database/Assets/Art/Ui/Icons/icon-settings.svg?fileID=2800000&guid=e1a1b2c3d4e5f60718293a4b5c6d7ea3&type=3#icon-settings";
    public const string Credits = "project://database/Assets/Art/Ui/Icons/icon-credits.svg?fileID=2800000&guid=e1a1b2c3d4e5f60718293a4b5c6d7ea4&type=3#icon-credits";
    public const string Quit = "project://database/Assets/Art/Ui/Icons/icon-quit.svg?fileID=2800000&guid=e1a1b2c3d4e5f60718293a4b5c6d7ea5&type=3#icon-quit";
    public const string Host = "project://database/Assets/Art/Ui/Icons/icon-host.svg?fileID=2800000&guid=e1a1b2c3d4e5f60718293a4b5c6d7ea6&type=3#icon-host";
    public const string Join = "project://database/Assets/Art/Ui/Icons/icon-join.svg?fileID=2800000&guid=e1a1b2c3d4e5f60718293a4b5c6d7ea7&type=3#icon-join";
    public const string Back = "project://database/Assets/Art/Ui/Icons/icon-back.svg?fileID=2800000&guid=e1a1b2c3d4e5f60718293a4b5c6d7ea8&type=3#icon-back";
    public const string Shield = "project://database/Assets/Art/Ui/Icons/icon-shield.svg?fileID=2800000&guid=e1a1b2c3d4e5f60718293a4b5c6d7ea9&type=3#icon-shield";
    public const string Health = "project://database/Assets/Art/Ui/Icons/icon-health.svg?fileID=2800000&guid=e1a1b2c3d4e5f60718293a4b5c6d7eaa&type=3#icon-health";
    public const string Coins = "project://database/Assets/Art/Ui/Icons/icon-coins.svg?fileID=2800000&guid=e1a1b2c3d4e5f60718293a4b5c6d7eab&type=3#icon-coins";
    public const string Skull = "project://database/Assets/Art/Ui/Icons/icon-skull.svg?fileID=2800000&guid=e1a1b2c3d4e5f60718293a4b5c6d7eac&type=3#icon-skull";
    public const string Sword = "project://database/Assets/Art/Ui/Icons/icon-sword.svg?fileID=2800000&guid=e1a1b2c3d4e5f60718293a4b5c6d7ead&type=3#icon-sword";
    public const string Clock = "project://database/Assets/Art/Ui/Icons/icon-clock.svg?fileID=2800000&guid=e1a1b2c3d4e5f60718293a4b5c6d7eae&type=3#icon-clock";
    public const string MouseLeft = "project://database/Assets/Art/Ui/Icons/icon-mouse-left.svg?fileID=2800000&guid=e1a1b2c3d4e5f60718293a4b5c6d7eaf&type=3#icon-mouse-left";
    public const string ChartUp = "project://database/Assets/Art/Ui/Icons/icon-chart-up.svg?fileID=2800000&guid=e1a1b2c3d4e5f60718293a4b5c6d7eb2&type=3#icon-chart-up";
    public const string Trophy = "project://database/Assets/Art/Ui/Icons/icon-trophy.svg?fileID=2800000&guid=e1a1b2c3d4e5f60718293a4b5c6d7eb3&type=3#icon-trophy";
}
