using UnityEngine.SceneManagement;

/// <summary>How the Game scene should start when it is loaded from the title.</summary>
public enum LaunchMode
{
    /// <summary>The Game scene was opened directly (editor, tests): continue a usable save, otherwise start a new game.</summary>
    Direct,
    NewGame,
    Continue
}

/// <summary>The two scenes (Title, Game) and the hand-off between them. The title chooses a LaunchMode;
/// the Game scene reads it once when it starts.</summary>
public static class GameScenes
{
    public const string Title="Title";
    public const string Game="Game";
    private static LaunchMode pending=LaunchMode.Direct;

    public static void LoadGame(LaunchMode mode){pending=mode;SceneManager.LoadScene(Game);}
    public static void LoadTitle(){pending=LaunchMode.Direct;SceneManager.LoadScene(Title);}
    /// <summary>The requested mode, then Direct for any later start (a scene reload or the editor).</summary>
    public static LaunchMode Consume(){var mode=pending;pending=LaunchMode.Direct;return mode;}
}
