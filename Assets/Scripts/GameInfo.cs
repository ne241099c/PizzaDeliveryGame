/// <summary>The game's name and version, defined once. The title screen reads these at runtime; the editor menu
/// Tools/Game Info/Sync copies them to the build settings (productName, bundleVersion) and README.md.
/// The name is provisional (a parody of the travelling salesman problem): change it here and run the sync.</summary>
public static class GameInfo
{
    /// <summary>Shown in the window title, the build and README.</summary>
    public const string DisplayName="Traveling Pizza Salesman";
    /// <summary>The logo: a small first line and the large words below it.</summary>
    public const string LogoSmall="Traveling";
    public static readonly string[] LogoLarge={"Pizza","Salesman"};
    /// <summary>The reading in katakana, shown small under the logo.</summary>
    public const string Kana="トラベリング・ピザセールスマン";
    public const string Version="0.1.0";
    /// <summary>Internal id for the save folder. Never change it: Unity's data folder follows productName, so the saves
    /// would be lost when the display name changes. CampaignSaveManager keeps saves under this name instead.</summary>
    public const string SaveFolder="PizzaDeliveryGame";
    public static string VersionLabel=>"v"+Version;
}
