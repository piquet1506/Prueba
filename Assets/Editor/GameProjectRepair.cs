using System;
using System.IO;
using System.Linq;

using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public static class GameProjectRepair
{
    private const string Request = "Tools/request-repair";
    [InitializeOnLoadMethod]
    private static void Schedule() { EditorApplication.delayCall += RunRequested; }
    private static void RunRequested()
    {
        if (!File.Exists(Request)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        { EditorApplication.delayCall += RunRequested; return; }
        File.Delete(Request);
        Repair();
    }

    [MenuItem("Tools/Auki/Repair and validate scenes")]
    public static void Repair()
    {
        // Preserve the user's current unsaved scene work before editing other scenes.
        if (!EditorSceneManager.SaveOpenScenes()) throw new Exception("Could not save open scenes.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            if (AssetDatabase.LoadAssetAtPath<TMPro.TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset") == null)
                AssetDatabase.ImportPackage("Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage", false);
            string[] names = { SceneFlow.MainMenu, "PantallaCarga", "Nivel1", "Nivel 2", "Nivel 4", "Ajustes" };
            EditorBuildSettings.scenes = names.Select(n => new EditorBuildSettingsScene("Assets/Scenes/" + n + ".unity", true)).ToArray();
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            tags.FindProperty("layers").GetArrayElementAtIndex(6).stringValue = "Suelo";
            tags.ApplyModifiedProperties();
            RepairAnimation();
            foreach (string level in SceneFlow.Levels)
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + level + ".unity");
                RepairLevel();
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            string report = "Scene repair completed.\n";
            foreach (var entry in EditorBuildSettings.scenes)
            {
                var scene = EditorSceneManager.OpenScene(entry.path);
                int missing = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                    .Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
                if (missing != 0) throw new Exception(entry.path + ": " + missing + " missing scripts");
                foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                        if (button.onClick.GetPersistentTarget(i) == null) throw new Exception("Disconnected button: " + button.name);
                report += entry.path + ": no missing scripts or null button targets\n";
            }
            File.WriteAllText("Logs/SceneValidation.txt", report);
            Debug.Log(report);
        }
        catch (Exception e) { File.WriteAllText("Logs/SceneValidation.txt", e.ToString()); Debug.LogException(e); }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }

    private static void RepairAnimation()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/animations/New Animator Controller.controller");
        if (!controller.parameters.Any(p => p.name == "Atacar")) controller.AddParameter("Atacar", AnimatorControllerParameterType.Trigger);
        var machine = controller.layers[0].stateMachine;
        var attack = machine.states.First(s => s.state.name == "Atacar").state;
        var idle = machine.states.First(s => s.state.name == "Idle").state;
        if (!machine.anyStateTransitions.Any(t => t.destinationState == attack))
        {
            var transition = machine.AddAnyStateTransition(attack);
            transition.AddCondition(AnimatorConditionMode.If, 0, "Atacar");
            transition.duration = 0;
            transition.canTransitionToSelf = false;
        }
        if (attack.transitions.Length == 0)
        {
            var transition = attack.AddTransition(idle);
            transition.hasExitTime = true; transition.exitTime = 1; transition.duration = 0;
        }
        EditorUtility.SetDirty(controller);
    }

    private static void RepairLevel()
    {
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerControlerC>();
        if (player == null) throw new Exception("Player missing");
        player.animator = player.GetComponent<Animator>();
        player.capaSuelo = 1 << 6;
        player.GetComponent<SpriteRenderer>().sortingOrder = 10;
        var tiles = UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        foreach (var tile in tiles)
        {
            tile.gameObject.layer = 6;
            if (tile.GetComponent<TilemapCollider2D>() == null) tile.gameObject.AddComponent<TilemapCollider2D>();
            tile.CompressBounds();
        }
        var ground = tiles.OrderByDescending(t => t.GetUsedTilesCount()).First();
        var bounds = ground.localBounds;
        float bottom = ground.transform.TransformPoint(bounds.min).y;
        player.limiteCaida = bottom - 8f;
        foreach (var enemy in UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
        { enemy.player = player.transform; enemy.gameObject.layer = 0; enemy.GetComponent<SpriteRenderer>().sortingOrder = 10; }
        foreach (var coin in UnityEngine.Object.FindObjectsByType<Moneda>(FindObjectsSortMode.None))
            coin.GetComponent<SpriteRenderer>().sortingOrder = 11;
        var camera = Camera.main;
        if (camera == null) throw new Exception("Main camera missing");
        camera.orthographic = true;
        camera.orthographicSize = 5;
        if (camera.GetComponent<Unity.Cinemachine.CinemachineBrain>() == null) camera.gameObject.AddComponent<Unity.Cinemachine.CinemachineBrain>();
        var virtualCamera = UnityEngine.Object.FindFirstObjectByType<Unity.Cinemachine.CinemachineVirtualCamera>();
        if (virtualCamera == null) virtualCamera = new GameObject("Gameplay Camera").AddComponent<Unity.Cinemachine.CinemachineVirtualCamera>();
        virtualCamera.Follow = player.transform;
        virtualCamera.m_Lens.OrthographicSize = 5;
        virtualCamera.m_Lens.NearClipPlane = 0.1f;
        virtualCamera.transform.position = player.transform.position + Vector3.back * 10;
        if (virtualCamera.GetCinemachineComponent<Unity.Cinemachine.CinemachineComponentBase>() == null)
            virtualCamera.AddCinemachineComponent<Unity.Cinemachine.CinemachineFramingTransposer>();
        if (UnityEngine.Object.FindFirstObjectByType<LevelExit>() == null)
        {
            // Place the exit on the rightmost occupied ground column, not outside the map.
            Vector3Int cell = ground.cellBounds.min;
            bool found = false;
            for (int x = ground.cellBounds.xMax - 1; x >= ground.cellBounds.xMin && !found; x--)
                for (int y = ground.cellBounds.yMax - 1; y >= ground.cellBounds.yMin; y--)
                    if (ground.HasTile(new Vector3Int(x, y, 0))) { cell = new Vector3Int(x, y, 0); found = true; break; }
            var exit = new GameObject("Salida al siguiente nivel");
            exit.transform.position = ground.GetCellCenterWorld(cell) + Vector3.up * 1.5f;
            var trigger = exit.AddComponent<BoxCollider2D>(); trigger.isTrigger = true; trigger.size = new Vector2(1.5f, 4f);
            exit.AddComponent<LevelExit>();
            var label = new GameObject("Salida").AddComponent<TextMesh>();
            label.transform.SetParent(exit.transform, false); label.transform.localPosition = Vector3.up * 2;
            label.text = "SALIDA"; label.characterSize = 0.15f; label.fontSize = 48; label.anchor = TextAnchor.MiddleCenter;
        }
        if (UnityEngine.Object.FindFirstObjectByType<GameManagerUI>() == null) CreateUI();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private static Text Label(Transform parent, string text, Vector2 position, Vector2 size)
    {
        var go = new GameObject(text, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
        var label = go.GetComponent<Text>(); label.text = text; label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 24; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
        var rect = go.GetComponent<RectTransform>(); rect.sizeDelta = size; rect.anchoredPosition = position;
        return label;
    }
    private static void Button(Transform parent, string text, Vector2 position, UnityAction action)
    {
        var go = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(260, 52); rect.anchoredPosition = position;
        go.GetComponent<Image>().color = new Color(0.18f, 0.3f, 0.23f, 1);
        UnityEventTools.AddPersistentListener(go.GetComponent<Button>().onClick, action);
        go.AddComponent<ButtonSound>();
        Label(go.transform, text, Vector2.zero, rect.sizeDelta);
    }
    private static GameObject Panel(Transform parent, string title)
    {
        var go = new GameObject(title, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(520, 390);
        go.GetComponent<Image>().color = new Color(0.06f, 0.1f, 0.08f, 0.97f);
        Label(go.transform, title, new Vector2(0, 135), new Vector2(450, 60));
        return go;
    }
    private static void CreateUI()
    {
        var canvas = new GameObject("Interfaz de juego", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720);
        var ui = canvas.AddComponent<GameManagerUI>();
        ui.coinText = Label(canvas.transform, "Monedas: 0", new Vector2(-490, 320), new Vector2(260, 45));
        Label(canvas.transform, "A/D o flechas: mover   Espacio: saltar   J: atacar   Esc: pausa", new Vector2(0, -325), new Vector2(1150, 45));
        ui.pausePanel = Panel(canvas.transform, "Pausa");
        Button(ui.pausePanel.transform, "Continuar", new Vector2(0, 55), ui.ResumeGame);
        Button(ui.pausePanel.transform, "Reiniciar", new Vector2(0, -15), ui.RestartLevel);
        Button(ui.pausePanel.transform, "Menu principal", new Vector2(0, -85), ui.ReturnToMenu);
        ui.gameOverPanel = Panel(canvas.transform, "Fin de la partida");
        Button(ui.gameOverPanel.transform, "Reintentar", new Vector2(0, 30), ui.RestartLevel);
        Button(ui.gameOverPanel.transform, "Menu principal", new Vector2(0, -45), ui.ReturnToMenu);
        ui.pausePanel.SetActive(false); ui.gameOverPanel.SetActive(false);
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    [MenuItem("Tools/Auki/Build Windows")]
    public static void Build()
    {
        var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            "Build/Auki.exe", BuildTarget.StandaloneWindows64, BuildOptions.Development);
        File.WriteAllText("Logs/BuildValidation.txt", report.summary.result + " errors=" + report.summary.totalErrors);
    }
}
