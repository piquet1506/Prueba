using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class GameSmokeChecks
{
    static GameSmokeChecks()
    {
        EditorApplication.update += CheckRequest;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("AukiSmoke", false))
                new GameObject("Smoke checks").AddComponent<GameSmokeRunner>();
        };
    }
    private static void CheckRequest()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!File.Exists("Tools/request-smoke")) return;
        File.Delete("Tools/request-smoke");
        if (!EditorSceneManager.SaveOpenScenes()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/Menu Principal.unity");
        SessionState.SetBool("AukiSmoke", true);
        EditorApplication.isPlaying = true;
    }
}

public class GameSmokeRunner : MonoBehaviour
{
    private string report = "Play mode checks\n";
    private void Awake() { DontDestroyOnLoad(gameObject); }
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            File.WriteAllText("Logs/PlayValidation.txt", report + "FAIL: " + message);
            SessionState.SetBool("AukiSmoke", false);
            EditorApplication.isPlaying = false;
            throw new Exception(message);
        }
        report += "PASS: " + message + "\n";
    }
    private IEnumerator WaitScene(string name)
    {
        float deadline = Time.realtimeSinceStartup + 20;
        while (SceneManager.GetActiveScene().name != name && Time.realtimeSinceStartup < deadline) yield return null;
        Assert(SceneManager.GetActiveScene().name == name, "Load " + name);
        yield return new WaitForSecondsRealtime(0.3f);
    }
    private IEnumerator Start()
    {
        yield return null;
        var play = FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.name == "Jugar");
        play.onClick.Invoke();
        yield return WaitScene("Nivel1");
        for (int i = 0; i < SceneFlow.Levels.Length; i++)
        {
            var player = FindFirstObjectByType<PlayerControlerC>();
            var ui = FindFirstObjectByType<GameManagerUI>();
            Assert(player != null && ui != null && Camera.main != null, "Player, camera and UI in " + SceneFlow.Levels[i]);
            Assert(player.GetComponent<SpriteRenderer>().sprite != null, "Player animation has a sprite");
            Assert(Physics2D.Raycast(player.transform.position, Vector2.down, player.longitudRaycast + 0.3f, player.capaSuelo).collider != null, "Ground detection");
            ui.PauseGame(); Assert(Time.timeScale == 0 && ui.pausePanel.activeSelf, "Pause");
            ui.ResumeGame(); Assert(Time.timeScale == 1 && !ui.pausePanel.activeSelf, "Resume");
            player.Atacar(); yield return new WaitForSecondsRealtime(0.1f);
            var coin = FindFirstObjectByType<Moneda>();
            Assert(coin != null, "Coin exists");
            coin.SendMessage("OnTriggerEnter2D", player.GetComponent<Collider2D>());
            Assert(ui.coinText.text == "Monedas: 1", "Coin updates HUD");
            player.Morir(); Assert(ui.gameOverPanel.activeSelf && Time.timeScale == 0, "Death shows game over");
            ui.ResumeGame(); Assert(Time.timeScale == 0, "Game over cannot resume");
            ui.RestartLevel(); yield return null; yield return new WaitForSecondsRealtime(0.3f);
            Assert(Time.timeScale == 1 && !FindFirstObjectByType<GameManagerUI>().gameOverPanel.activeSelf, "Restart resets game");
            var exit = FindFirstObjectByType<LevelExit>();
            Assert(exit != null, "Level exit exists");
            exit.SendMessage("OnTriggerEnter2D", FindFirstObjectByType<PlayerControlerC>().GetComponent<Collider2D>());
            yield return WaitScene(i + 1 < SceneFlow.Levels.Length ? SceneFlow.Levels[i + 1] : SceneFlow.MainMenu);
        }
        var audio = FindObjectsByType<SoundManager>(FindObjectsSortMode.None).First(s => s != SoundManager.Instance);
        float previous = SoundManager.Instance.musicAudioSource.volume;
        audio.SetMusicVolume(0.25f);
        Assert(Mathf.Approximately(SoundManager.Instance.musicAudioSource.volume, 0.25f), "Menu audio controls work after returning");
        audio.SetMusicVolume(previous);
        File.WriteAllText("Logs/PlayValidation.txt", report + "SUCCESS\n");
        SessionState.SetBool("AukiSmoke", false);
        EditorApplication.isPlaying = false;
    }
}
