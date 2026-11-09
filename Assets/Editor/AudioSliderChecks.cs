using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class AudioSliderChecks
{
    static AudioSliderChecks()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists("Tools/request-audio-check") || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete("Tools/request-audio-check"); Run();
        };
        EditorApplication.playModeStateChanged += state =>
        {
            if (SessionState.GetBool("AudioCheck", false)) File.WriteAllText("Logs/AudioCheckState.txt", state.ToString());
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("AudioCheck", false))
            {
                Application.runInBackground = true;
                EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Show();
                new GameObject("Audio checks").AddComponent<AudioSliderRunner>();
            }
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool("AudioCheckRestore", false))
            {
                SessionState.SetBool("AudioCheckRestore", false);
                EditorApplication.delayCall += () => EditorSceneManager.OpenScene(SessionState.GetString("AudioCheckScene", "Assets/Scenes/Nivel1.unity"));
            }
        };
    }
    [MenuItem("Tools/Auki/Comprobar sliders en Play sin build")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveOpenScenes()) return;
        SessionState.SetString("AudioCheckScene", SceneManager.GetActiveScene().path);
        EditorSceneManager.OpenScene("Assets/Scenes/Menu Principal.unity");
        SessionState.SetBool("AudioCheck", true);
        EditorApplication.isPaused = false;
        EditorApplication.isPlaying = true;
    }
}

public class AudioSliderRunner : MonoBehaviour
{
    private string report = "Audio: pruebas en Play, sin build\n";
    private float music, effects, deadline;
    private bool finished;
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        music = PlayerPrefs.GetFloat("MusicVolume", .8f); effects = PlayerPrefs.GetFloat("SfxVolume", .8f);
        deadline = Time.realtimeSinceStartup + 90;
        Application.logMessageReceived += OnLog;
    }
    private void OnDestroy() { Application.logMessageReceived -= OnLog; }
    private void OnLog(string message, string trace, LogType type)
    { if (type == LogType.Exception || type == LogType.Error) Finish(false, message); }
    private void Update() { if (!finished && Time.realtimeSinceStartup > deadline) Finish(false, "Timeout"); }
    private void Check(bool value, string label)
    {
        if (!value) { Finish(false, label); throw new Exception(label); }
        report += "PASS: " + label + "\n";
    }
    private void Finish(bool success, string detail)
    {
        if (finished) return;
        finished = true;
        SoundManager.SetChannelVolume(true, music); SoundManager.SetChannelVolume(false, effects); PlayerPrefs.Save();
        File.WriteAllText("Logs/AudioSliderChecks.txt", report + (success ? "SUCCESS: " : "FAIL: ") + detail);
        SessionState.SetBool("AudioCheck", false); SessionState.SetBool("AudioCheckRestore", true);
        Time.timeScale = 1; EditorApplication.isPlaying = false;
    }
    private void Exercise(string context)
    {
        Canvas.ForceUpdateCanvases();
        var bindings = FindObjectsByType<VolumeSlider>(FindObjectsSortMode.None);
        Check(bindings.Length == 2, context + ": dos sliders activos");
        Check(SoundManager.Instance != null && SoundManager.Instance.musicAudioSource.isPlaying, context + ": musica persistente en reproduccion");
        foreach (var binding in bindings)
        {
            var slider = binding.GetComponent<Slider>();
            bool isMusic = binding.channel == VolumeSlider.AudioChannel.Music;
            var source = isMusic ? SoundManager.Instance.musicAudioSource : SoundManager.Instance.sfxAudioSource;
            var other = isMusic ? SoundManager.Instance.sfxAudioSource : SoundManager.Instance.musicAudioSource;
            float unchanged = other.volume;
            var rect = (RectTransform)slider.transform;
            var canvas = slider.GetComponentInParent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)) };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Slider>() == slider, context + ": puntero alcanza " + binding.channel);
            foreach (float value in new[] { 0f, .25f, 1f, .6f })
            {
                slider.value = value;
                Check(Mathf.Approximately(source.volume, value) && Mathf.Approximately(other.volume, unchanged), context + ": " + binding.channel + "=" + value + " independiente");
            }
        }
    }
    private IEnumerator Start()
    {
        yield return null;
        var menu = FindFirstObjectByType<MainMenuController>();
        menu.OpenSettings(); yield return null;
        ScreenCapture.CaptureScreenshot("Logs/MenuOptions.png"); yield return new WaitForSecondsRealtime(.2f);
        menu.OpenSound(); yield return null;
        Exercise("Menu"); menu.CloseSound(); menu.OpenSound(); yield return null;
        Check(FindObjectsByType<VolumeSlider>(FindObjectsSortMode.None).All(b => Mathf.Approximately(b.GetComponent<Slider>().value, .6f)), "Menu: valores conservados al reabrir");
        foreach (string level in SceneFlow.Levels)
        {
            SceneFlow.LoadLevel(level);
            while (SceneManager.GetActiveScene().name != level && !finished) yield return null;
            while (CinematicSequence.IsPlaying && !finished) yield return null;
            if (finished) yield break;
            yield return new WaitForSecondsRealtime(.6f);
            var ui = FindFirstObjectByType<GameManagerUI>();
            Check(ui.isActiveAndEnabled && Time.timeScale == 1 && !ui.settingsCanvas.IsOpen, level + ": inicio sin pausa y HUD activo");
            Check(ui.settingsCanvas.gameObject == ui.gameObject, level + ": controlador integrado en Interfaz de juego");
            ui.PauseGame();
            var open = ui.pausePanel.GetComponentsInChildren<Button>().Single(b => Enumerable.Range(0, b.onClick.GetPersistentEventCount()).Any(i => b.onClick.GetPersistentMethodName(i) == "OpenSettings"));
            open.onClick.Invoke(); yield return null;
            var settings = ui.settingsCanvas;
            Check(settings.optionsPanel.activeInHierarchy && !ui.pausePanel.activeSelf, level + ": boton Ajustes abre opciones");
            if (level == "Nivel1") { ScreenCapture.CaptureScreenshot("Logs/GameOptions.png"); yield return new WaitForSecondsRealtime(.2f); }
            ClickSettings(settings, "ShowControls"); yield return null;
            Check(settings.controlsPanel.activeInHierarchy, level + ": boton Controles abre panel");
            ClickSettings(settings, "ShowOptions"); yield return null;
            Check(settings.optionsPanel.activeInHierarchy, level + ": Volver de controles");
            ClickSettings(settings, "ShowSound"); yield return null;
            Check(Time.timeScale == 0, level + ": pausa activa");
            Exercise(level);
            ClickSettings(settings, "ShowOptions"); yield return null;
            ClickSettings(settings, "Close"); yield return null;
            Check(!settings.IsOpen && ui.pausePanel.activeInHierarchy && Time.timeScale == 0, level + ": Volver regresa a pausa");
            ui.settingsCanvas.Close(); ui.OpenSettings(); ui.settingsCanvas.ShowSound(); yield return null;
            Check(Mathf.Approximately(ui.settingsCanvas.musicSlider.value, .6f) && Mathf.Approximately(ui.settingsCanvas.sfxSlider.value, .6f), level + ": conserva volumen al reabrir");
            ui.ResumeGame();
            Check(Time.timeScale == 1 && ui.isActiveAndEnabled && !settings.IsOpen, level + ": reanuda sin apagar la interfaz");
        }
        SceneFlow.Load(SceneFlow.MainMenu); yield return new WaitForSecondsRealtime(.6f);
        menu = FindFirstObjectByType<MainMenuController>(); menu.OpenSettings(); menu.OpenSound(); yield return null;
        Exercise("Regreso al menu");
        Finish(true, "Menu y tres niveles verificados; preferencias restauradas.");
    }
    private void ClickSettings(SettingsCanvasController settings, string method)
    {
        var button = settings.GetComponentsInChildren<Button>().FirstOrDefault(b => Enumerable.Range(0, b.onClick.GetPersistentEventCount()).Any(i => b.onClick.GetPersistentMethodName(i) == method));
        Check(button != null, "Boton enlazado: " + method);
        button.onClick.Invoke();
    }
}
