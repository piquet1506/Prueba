using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class SharedSettingsSetup
{
    private const string PrefabPath = "Assets/Prefabs/AjustesEnJuego.prefab";
    [InitializeOnLoadMethod]
    private static void Schedule()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists("Tools/request-shared-settings") || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete("Tools/request-shared-settings"); Run();
        };
    }
    // Map by sibling indices: the menu contains duplicate panel names.
    private static GameObject Map(GameObject source, Transform original, Transform clone)
    {
        if (source == null) return null;
        var indices = new System.Collections.Generic.Stack<int>();
        var node = source.transform;
        while (node != original)
        {
            if (node == null) throw new Exception("Panel fuera del Canvas");
            indices.Push(node.GetSiblingIndex()); node = node.parent;
        }
        while (indices.Count > 0) clone = clone.GetChild(indices.Pop());
        return clone.gameObject;
    }
    [MenuItem("Tools/Auki/Copiar ajustes actuales del menu al juego")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveOpenScenes()) return;
        var setup = EditorSceneManager.GetSceneManagerSetup();
        var report = new StringBuilder();
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Menu Principal.unity");
            var menu = Object.FindFirstObjectByType<MainMenuController>();
            var serialized = new SerializedObject(menu);
            Func<string, GameObject> panel = key => (GameObject)serialized.FindProperty(key).objectReferenceValue;
            var canvas = panel("optionsPanel").GetComponentInParent<Canvas>();
            var clone = Object.Instantiate(canvas.gameObject); clone.name = "AjustesEnJuego";
            if (PrefabUtility.IsPartOfPrefabInstance(clone)) PrefabUtility.UnpackPrefabInstance(clone, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var controller = clone.AddComponent<SettingsCanvasController>();
            controller.optionsPanel = Map(panel("optionsPanel"), canvas.transform, clone.transform);
            controller.soundPanel = Map(panel("soundPanel"), canvas.transform, clone.transform);
            controller.controlsPanel = Map(panel("controlsPanel"), canvas.transform, clone.transform);
            var mainPanel = Map(panel("mainMenuPanel"), canvas.transform, clone.transform);
            if (mainPanel != null) Object.DestroyImmediate(mainPanel);
            foreach (var old in clone.GetComponentsInChildren<MainMenuController>(true)) Object.DestroyImmediate(old);
            foreach (var button in clone.GetComponentsInChildren<Button>(true))
            {
                for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                {
                    string method = button.onClick.GetPersistentMethodName(i);
                    UnityAction action;
                    switch (method)
                    {
                        case "OpenSettings": action = controller.ShowOptions; break;
                        case "OpenSound": action = controller.ShowSound; break;
                        case "OpenControls": action = controller.ShowControls; break;
                        case "CloseSound": case "CloseControls": action = controller.ShowOptions; break;
                        case "CloseSettings": case "ReturnToMainMenu": action = controller.Close; break;
                        case "PlayClick": action = controller.PlayClick; break;
                        default: throw new Exception("Revisar evento de ajustes: " + button.name + "/" + method);
                    }
                    UnityEventTools.RemovePersistentListener(button.onClick, i);
                    UnityEventTools.AddPersistentListener(button.onClick, action);
                    report.AppendLine(button.name + ": " + method + " -> " + action.Method.Name);
                }
            }
            foreach (var binding in clone.GetComponentsInChildren<VolumeSlider>(true))
            {
                if (binding.channel == VolumeSlider.AudioChannel.Music) controller.musicSlider = binding.GetComponent<Slider>();
                else controller.sfxSlider = binding.GetComponent<Slider>();
            }
            if (controller.musicSlider == null || controller.sfxSlider == null) throw new Exception("Faltan sliders");
            clone.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            clone.GetComponent<Canvas>().sortingOrder = 50;
            controller.ShowOptions(); clone.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(clone, PrefabPath);
            Object.DestroyImmediate(clone);
            foreach (string level in SceneFlow.Levels)
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + level + ".unity");
                var ui = Object.FindFirstObjectByType<GameManagerUI>();
                foreach (var old in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SettingsCanvasController>(true)).ToArray())
                {
                    if (old.gameObject == ui.gameObject)
                    {
                        foreach (var existingPanel in new[] { old.optionsPanel, old.soundPanel, old.controlsPanel })
                            if (existingPanel != null && existingPanel != ui.gameObject) Object.DestroyImmediate(existingPanel);
                        Object.DestroyImmediate(old);
                    }
                    else Object.DestroyImmediate(old.gameObject);
                }
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
                var settings = instance.GetComponent<SettingsCanvasController>();
                settings.owner = ui; ui.settingsCanvas = settings;
                PrefabUtility.RecordPrefabInstancePropertyModifications(settings);
                if (PrefabUtility.IsPartOfPrefabInstance(ui)) PrefabUtility.RecordPrefabInstancePropertyModifications(ui);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                report.AppendLine(level + ": prefab actual del menu instalado y enlazado a pausa");
            }
            File.WriteAllText("Logs/SharedSettingsSetup.txt", report + "OK\n");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        GameplayMenuStructureRepair.Run();
    }
}
