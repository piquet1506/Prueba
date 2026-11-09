using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneFlow
{
    public const string MainMenu = "Menu Principal";
    public static readonly string[] Levels = { "Nivel1", "Nivel 2", "Nivel 4" };
    public static string PendingLevel { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { PendingLevel = null; Time.timeScale = 1f; }

    public static void Load(string scene)
    {
        if (string.IsNullOrWhiteSpace(scene) || !Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogError("Escena no incluida en Build Settings: " + scene);
            return;
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene(scene);
    }

    public static void LoadLevel(string scene)
    {
        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogError("Nivel no disponible: " + scene);
            return;
        }
        PendingLevel = scene;
        Load("PantallaCarga");
    }

    public static string ConsumeLevel(string fallback)
    {
        string level = string.IsNullOrEmpty(PendingLevel) ? fallback : PendingLevel;
        PendingLevel = null;
        return level;
    }

    public static void NextLevel()
    {
        if (CinematicSequence.IsPlaying) return;
        int index = System.Array.IndexOf(Levels, SceneManager.GetActiveScene().name);
        string destination = index >= 0 && index + 1 < Levels.Length ? Levels[index + 1] : MainMenu;
        var cinematics = Object.FindFirstObjectByType<LevelCinematics>();
        if (cinematics != null) cinematics.FinishLevel(destination);
        else CinematicSequence.Begin(null, destination, .6f, false);
    }
}
