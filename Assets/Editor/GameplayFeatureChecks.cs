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
public static class GameplayFeatureChecks
{
    static GameplayFeatureChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("AukiFeatures", false))
                new GameObject("Feature checks").AddComponent<GameplayFeatureRunner>();
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool("AukiFeaturesDone", false))
            {
                SessionState.SetBool("AukiFeaturesDone", false);
                EditorApplication.delayCall += () => { if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetInt("AukiFeaturesExit", 1)); };
            }
        };
    }
    [MenuItem("Tools/Auki/Comprobar funciones en Play sin build")]
    public static void Run()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveOpenScenes()) throw new Exception("No se pudieron guardar las escenas.");
        EditorSceneManager.OpenScene("Assets/Scenes/Menu Principal.unity");
        SessionState.SetBool("AukiFeatures", true); SessionState.SetBool("AukiFeaturesDone", false);
        EditorApplication.isPlaying = true;
    }
}

public class GameplayFeatureRunner : MonoBehaviour
{
    private string report = "Pruebas de funciones en Play (sin build)\n";
    private bool finished;
    private float deadline;
    private float oldMusic, oldSfx;
    private void Awake()
    {
        DontDestroyOnLoad(gameObject); deadline = Time.realtimeSinceStartup + 150;
        oldMusic = PlayerPrefs.GetFloat("MusicVolume", 0.8f); oldSfx = PlayerPrefs.GetFloat("SfxVolume", 0.8f);
        Application.logMessageReceived += OnLog;
    }
    private void OnDestroy() { Application.logMessageReceived -= OnLog; }
    private void OnLog(string message, string trace, LogType type)
    { if (!finished && (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)) Finish(false, message + "\n" + trace); }
    private void Update() { if (!finished && Time.realtimeSinceStartup > deadline) Finish(false, "Tiempo de espera agotado"); }
    private void Check(bool condition, string text)
    { if (!condition) { Finish(false, text); throw new Exception(text); } report += "PASS: " + text + "\n"; }
    private void Finish(bool success, string text)
    {
        if (finished) return; finished = true;
        PlayerPrefs.SetFloat("MusicVolume", oldMusic); PlayerPrefs.SetFloat("SfxVolume", oldSfx); PlayerPrefs.Save();
        File.WriteAllText("Logs/FeaturePlayChecks.txt", report + (success ? "SUCCESS: " : "FAIL: ") + text + "\n");
        SessionState.SetBool("AukiFeatures", false); SessionState.SetBool("AukiFeaturesDone", true);
        SessionState.SetInt("AukiFeaturesExit", success ? 0 : 1); EditorApplication.isPlaying = false;
    }
    private IEnumerator Loaded(string name)
    {
        float until = Time.realtimeSinceStartup + 20;
        while (SceneManager.GetActiveScene().name != name && Time.realtimeSinceStartup < until) yield return null;
        Check(SceneManager.GetActiveScene().name == name, "Escena " + name);
        yield return new WaitForSecondsRealtime(0.4f);
    }
    private IEnumerator Start()
    {
        yield return null;
        FindFirstObjectByType<MainMenuController>().OpenSettings(); yield return Loaded("Ajustes");
        var localAudio = FindObjectsByType<SoundManager>(FindObjectsSortMode.None).First(a => a.gameObject.scene.name == "Ajustes");
        localAudio.musicSlider.value = 0;
        Check(Mathf.Approximately(SoundManager.Instance.musicAudioSource.volume, 0), "Slider musica del menu silencia la fuente persistente");
        localAudio.musicSlider.value = 0.35f;
        Check(Mathf.Approximately(SoundManager.Instance.musicAudioSource.volume, 0.35f), "Slider musica del menu restaura volumen");
        FindFirstObjectByType<MainMenuController>().ReturnToMainMenu(); yield return Loaded(SceneFlow.MainMenu);
        FindFirstObjectByType<MainMenuController>().GoToLoadingScreen("PantallaCarga"); yield return Loaded("Nivel1");
        foreach (string level in SceneFlow.Levels)
        {
            if (SceneManager.GetActiveScene().name != level) { SceneFlow.LoadLevel(level); yield return Loaded(level); }
            var player = FindFirstObjectByType<PlayerControlerC>(); var health = player.GetComponent<Health>();
            var ui = FindFirstObjectByType<GameManagerUI>();
            var hud = FindObjectsByType<HealthBar>(FindObjectsSortMode.None).First(b => !b.enemyBar);
            Check(hud.target == health && hud.frames.All(s => s != null), level + ": HUD enlazado y sprites completos");
            Check(health.Current == 100 && health.Damage(15) && health.Current == 85, "Dano gradual del jugador");
            Check(!health.Damage(15) && health.Current == 85, "Proteccion contra dano repetido en el mismo instante");
            yield return new WaitForSecondsRealtime(0.3f);
            Check(hud.valueText.text == "85 / 100" && hud.picture.sprite != hud.frames[0], "HUD animado refleja la vida");
            Check(player.Grounded, "Jugador apoyado en el suelo compuesto");
            player.TryJump(); Check(player.GetComponent<Rigidbody2D>().linearVelocity.y > 0, "Salto desde el suelo");
            ui.OpenSettings(); Check(Time.timeScale == 0 && ui.settingsCanvas.gameObject.activeSelf, "Ajustes disponibles desde pausa");
            var settings = ui.settingsCanvas; Check(settings.owner == ui, "Referencia de retorno de ajustes guardada");
            settings.ShowSound(); settings.musicSlider.value = 0;
            Check(SoundManager.Instance.musicAudioSource.volume == 0, "Slider de juego silencia musica");
            settings.musicSlider.value = 0.6f; settings.sfxSlider.value = 0.3f;
            Check(Mathf.Approximately(SoundManager.Instance.musicAudioSource.volume, 0.6f) && Mathf.Approximately(SoundManager.Instance.sfxAudioSource.volume, 0.3f), "Musica y efectos independientes");
            settings.Close(); Check(ui.pausePanel.activeSelf && Time.timeScale == 0, "Volver de ajustes conserva pausa");
            ui.ResumeGame(); Check(Time.timeScale == 1, "Continuar reanuda");
            var dialogue = FindFirstObjectByType<DialogueUI>();
            Check(!DialogueUI.IsOpen && !dialogue.panel.activeSelf, "Dialogo oculto al iniciar el nivel");
            var npc = FindObjectsByType<NpcDialogue>(FindObjectsSortMode.None).First();
            player.transform.position = npc.transform.position + Vector3.left * 0.8f;
            player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero; Physics2D.SyncTransforms();
            yield return null;
            Check(!DialogueUI.IsOpen, "Acercarse a NPC no abre dialogo automaticamente");
            dialogue.Begin(npc); Check(DialogueUI.IsOpen && dialogue.panel.activeSelf && dialogue.dialogueText.text == npc.lines[0], "Interaccion explicita abre el dialogo");
            Check(!health.Damage(10), "Dialogo impide dano durante la conversacion");
            dialogue.Advance(); Check(dialogue.dialogueText.text == npc.lines[1], "Avance de linea");
            dialogue.Close(); Check(!DialogueUI.IsOpen && !dialogue.panel.activeSelf, "Cerrar oculta el dialogo");
            player.transform.position = npc.transform.position + Vector3.right * 20; Physics2D.SyncTransforms();
            dialogue.Begin(npc); Check(!DialogueUI.IsOpen, "NPC lejano no puede iniciar dialogo");
            // Controlled geometry exercises the same movement path as keyboard input.
            player.enabled = false;
            player.transform.position = new Vector3(2000, 100, 0); player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            var wall = new GameObject("Pared de prueba"); wall.layer = 6; wall.transform.position = new Vector3(2000.85f, 100, 0);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(1, 8); Physics2D.SyncTransforms();
            player.SetMovementInput(1, false, true, 0); player.SendMessage("FixedUpdate");
            Check(player.IsGripping, "Agarre de pared con Shift");
            player.SetMovementInput(1, false, true, 1); player.SendMessage("FixedUpdate");
            Check(player.GetComponent<Rigidbody2D>().linearVelocity.y > 0, "Trepar con direccion vertical");
            player.TryJump(); Check(player.GetComponent<Rigidbody2D>().linearVelocity.x < 0 && player.GetComponent<Rigidbody2D>().linearVelocity.y > 0, "Salto de pared se aleja del muro");
            Destroy(wall);
            player.SetMovementInput(0, true, false, 0);
            Check(player.IsSneaking && player.GetComponent<CapsuleCollider2D>().size.y < 0.95f, "Sigilo reduce el collider");
            var cover = new GameObject("Cobertura de prueba").AddComponent<CoverZone>(); player.EnterCover(cover);
            Check(player.IsHidden, "Sigilo dentro de cobertura oculta al jugador");
            player.LeaveCover(cover); Check(!player.IsHidden, "Salir de cobertura elimina ocultacion"); Destroy(cover);
            var enemy = FindFirstObjectByType<EnemyController>();
            if (enemy != null)
            {
                enemy.enabled = false; enemy.transform.position = player.transform.position + Vector3.right * 4;
                enemy.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero; Physics2D.SyncTransforms();
                Check(!enemy.CanDetectPlayer(), "Sigilo reduce distancia de deteccion");
                player.SetSneaking(false); Check(enemy.CanDetectPlayer(), "Jugador visible es detectado a la misma distancia");
                var bar = FindObjectsByType<HealthBar>(FindObjectsSortMode.None).First(b => b.target == enemy.GetComponent<Health>());
                yield return new WaitForSecondsRealtime(0.25f);
                Check(bar.visibility.alpha > 0.5f, "Barra enemiga aparece por cercania");
                enemy.transform.position = player.transform.position + Vector3.right * 30;
                yield return new WaitForSecondsRealtime(0.25f);
                Check(bar.visibility.alpha == 0, "Barra enemiga desaparece lejos de combate");
                enemy.ReceiveDamage(30); Check(enemy.GetComponent<Health>().Current == 30 && enemy.InCombat, "Enemigo recibe dano y entra en combate");
                yield return new WaitForSecondsRealtime(0.25f); Check(bar.visibility.alpha > 0.5f, "Combate mantiene visible la barra enemiga");
                enemy.ReceiveDamage(30); yield return null; Check(enemy == null, "Enemigo muere al agotar su vida");
            }
            var background = FindObjectsByType<ParallaxBackground>(FindObjectsSortMode.None).First(p => p.repeatHorizontally);
            Vector3 before = background.transform.position; Camera.main.transform.position += Vector3.right;
            background.SendMessage("LateUpdate"); Check(background.transform.position != before, "Fondo sigue movimiento de camara con paralaje");
            player.enabled = true; player.Morir();
            Check(health.IsDead && ui.gameOverPanel.activeSelf, "Muerte agota vida y abre fin de partida");
        }
        SceneFlow.Load(SceneFlow.MainMenu); yield return Loaded(SceneFlow.MainMenu);
        Check(FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Count(a => a.isPlaying && a.loop) == 1, "Solo una fuente de musica tras cambiar escenas");
        Finish(true, "Funciones verificadas en los tres niveles; no se genero ejecutable.");
    }
}
