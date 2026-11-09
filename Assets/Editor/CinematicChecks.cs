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
public static class CinematicChecks
{
    static CinematicChecks()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists("Tools/request-cinematic-check") || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete("Tools/request-cinematic-check"); Run();
        };
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("CinematicCheck", false))
            {
                Application.runInBackground = true;
                if (!Application.isBatchMode) EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Show();
                new GameObject("Cinematic checks").AddComponent<CinematicCheckRunner>();
            }
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool("CinematicRestore", false))
            {
                SessionState.SetBool("CinematicRestore", false);
                EditorApplication.delayCall += () =>
                {
                    EditorSceneManager.OpenScene(SessionState.GetString("CinematicCheckScene", "Assets/Scenes/Nivel1.unity"));
                    if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetInt("CinematicExit", 1));
                };
            }
        };
    }
    [MenuItem("Tools/Auki/Comprobar cinematicas sin video ni build")]
    public static void Run()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveOpenScenes()) return;
        SessionState.SetString("CinematicCheckScene", Application.isBatchMode ? "Assets/Scenes/Nivel1.unity" : SceneManager.GetActiveScene().path);
        EditorSceneManager.OpenScene("Assets/Scenes/Nivel1.unity");
        SessionState.SetBool("CinematicCheck", true);
        EditorApplication.isPaused = false; EditorApplication.isPlaying = true;
    }
    public static void ConfigureAndRun() { CinematicSetup.Run(); Run(); }
}

public class CinematicCheckRunner : MonoBehaviour
{
    private string report = "Cinematicas: pruebas en Play sin video asignado\n";
    private bool finished;
    private float deadline;
    private void Awake()
    {
        DontDestroyOnLoad(gameObject); deadline = Time.realtimeSinceStartup + 60;
        Application.logMessageReceived += OnLog;
    }
    private void OnDestroy() { Application.logMessageReceived -= OnLog; }
    private void OnLog(string message, string stack, LogType type)
    { if (type == LogType.Exception || type == LogType.Error) Finish(false, message); }
    private void Update() { if (!finished && Time.realtimeSinceStartup > deadline) Finish(false, "Timeout"); }
    private void Check(bool condition, string label)
    { if (!condition) { Finish(false, label); throw new Exception(label); } report += "PASS: " + label + "\n"; }
    private void Finish(bool success, string label)
    {
        if (finished) return; finished = true;
        File.WriteAllText("Logs/CinematicChecks.txt", report + (success ? "SUCCESS: " : "FAIL: ") + label);
        SessionState.SetBool("CinematicCheck", false); SessionState.SetBool("CinematicRestore", true);
        SessionState.SetInt("CinematicExit", success ? 0 : 1);
        EditorApplication.isPlaying = false;
    }
    private IEnumerator WaitForEnd()
    { while (CinematicSequence.IsPlaying && !finished) yield return null; yield return null; }
    private IEnumerator Start()
    {
        yield return null;
        Check(CinematicSequence.IsPlaying && Time.timeScale == 0, "Inicio de Nivel1 bloquea juego y abre cinematica");
        var runner = FindFirstObjectByType<CinematicSequence>();
        var text = runner.GetComponentInChildren<Text>(true);
        while (!text.gameObject.activeInHierarchy && !finished) yield return null;
        float start = Time.realtimeSinceStartup;
        Check(text.text == CinematicSequence.MissingClipMessage, "Texto exacto cuando falta video");
        var position = GameObject.FindGameObjectWithTag("Player").transform.position;
        bool frozen = true;
        while (CinematicSequence.IsPlaying && !finished)
        {
            frozen &= Vector3.Distance(position, GameObject.FindGameObjectWithTag("Player").transform.position) < .01f;
            yield return null;
        }
        Check(frozen, "Jugador inmovil durante cinematica");
        yield return null;
        Check(Time.timeScale == 1, "Inicio termina y devuelve control (timeScale=" + Time.timeScale + ")");
        start = Time.realtimeSinceStartup;
        SceneFlow.NextLevel(); SceneFlow.NextLevel();
        Check(FindObjectsByType<CinematicSequence>(FindObjectsSortMode.None).Length == 1, "Salida duplicada no crea otra transicion");
        runner = FindFirstObjectByType<CinematicSequence>();
        Check(runner.GetComponent<CanvasGroup>().alpha < 1, "Salida comienza con fundido gradual");
        while (runner.GetComponent<CanvasGroup>().alpha < 1 && !finished) yield return null;
        Check(runner.GetComponent<CanvasGroup>().alpha == 1, "Fundido alcanza negro");
        yield return WaitForEnd();
        Check(Time.realtimeSinceStartup - start >= 4.1f, "Aviso de tres segundos mas fundidos de salida y entrada");
        Check(SceneManager.GetActiveScene().name == "Nivel 2" && Time.timeScale == 1, "Fin de Nivel1 llega a Nivel 2");
        foreach (string destination in new[] { "Nivel 4", SceneFlow.MainMenu })
        {
            SceneFlow.NextLevel();
            runner = FindFirstObjectByType<CinematicSequence>();
            while (runner.GetComponent<CanvasGroup>().alpha < 1 && !finished) yield return null;
            runner.Skip();
            yield return WaitForEnd();
            Check(SceneManager.GetActiveScene().name == destination && Time.timeScale == 1, "Saltar continua hacia " + destination);
        }
        Finish(true, "Inicio, aviso, fundido, salto y destinos correctos. Video real pendiente de un clip del usuario.");
    }
}
