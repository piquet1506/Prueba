using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GameplayMenuStructureRepair
{
    [InitializeOnLoadMethod]
    private static void Schedule()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists("Tools/request-menu-structure") || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete("Tools/request-menu-structure"); Run();
        };
    }
    [MenuItem("Tools/Auki/Integrar ajustes en Interfaz de juego")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveOpenScenes()) return;
        var setup = EditorSceneManager.GetSceneManagerSetup();
        var report = new StringBuilder();
        try
        {
            foreach (var level in SceneFlow.Levels)
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + level + ".unity");
                var ui = Object.FindFirstObjectByType<GameManagerUI>();
                var old = ui.settingsCanvas;
                if (old == null) throw new System.Exception("Falta controlador de ajustes: " + level);
                if (PrefabUtility.IsPartOfPrefabInstance(old))
                    PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(old.gameObject), PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                var target = ui.GetComponent<SettingsCanvasController>();
                if (target == null) target = ui.gameObject.AddComponent<SettingsCanvasController>();
                if (old != target) EditorUtility.CopySerialized(old, target);
                target.owner = ui; ui.settingsCanvas = target;
                foreach (var panel in new[] { target.optionsPanel, target.soundPanel, target.controlsPanel })
                {
                    if (panel == null) throw new System.Exception("Panel sin referencia: " + level);
                    if (panel.transform.parent != ui.transform) panel.transform.SetParent(ui.transform, false);
                    var rect = (RectTransform)panel.transform;
                    // The menu was authored at 1920x1080; the gameplay HUD uses 1280x720.
                    // Dragging between inactive canvases had also left zero scales and displaced anchors.
                    var scaler = ui.GetComponent<UnityEngine.UI.CanvasScaler>();
                    float scale = scaler != null ? scaler.referenceResolution.x / 1920f : 1f;
                    rect.localScale = Vector3.one * scale;
                    rect.localRotation = Quaternion.identity;
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                    rect.anchoredPosition3D = Vector3.zero;
                    panel.transform.SetAsLastSibling();
                }
                if (old != target)
                {
                    // Preserve all button actions while replacing their former controller target.
                    foreach (var component in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Component>(true)))
                    {
                        if (component == null || component == old) continue;
                        var serialized = new SerializedObject(component);
                        var property = serialized.GetIterator(); bool changed = false;
                        while (property.Next(true))
                            if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == old)
                            { property.objectReferenceValue = target; changed = true; }
                        if (changed) serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                    var obsolete = old.gameObject;
                    if (obsolete != ui.gameObject && obsolete.name == "AjustesEnJuego" && !ui.transform.IsChildOf(obsolete.transform)) Object.DestroyImmediate(obsolete);
                    else Object.DestroyImmediate(old);
                }
                target.Hide(); ui.gameObject.SetActive(true);
                if (ui.pausePanel != null) ui.pausePanel.SetActive(false);
                if (ui.gameOverPanel != null) ui.gameOverPanel.SetActive(false);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                report.AppendLine(level + ": ajustes en " + target.name + "; paneles vinculados; Canvas anterior retirado.");
            }
            File.WriteAllText("Logs/MenuStructureRepair.txt", report.ToString());
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }
}
