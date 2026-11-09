using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SettingsVisualPolish
{
    [InitializeOnLoadMethod]
    private static void CheckRequest()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists("Tools/request-polish") || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete("Tools/request-polish"); Run();
        };
    }
    [MenuItem("Tools/Auki/Actualizar etiquetas de ajustes")]
    public static void Run()
    {
        if (!EditorSceneManager.SaveOpenScenes()) return;
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/AjustesEnJuego.prefab");
            var settings = root.GetComponent<SettingsCanvasController>();
            settings.sfxSlider = StyleSliders(settings.musicSlider, settings.sfxSlider);
            Relabel(root);
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/AjustesEnJuego.prefab");
            PrefabUtility.UnloadPrefabContents(root);
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Ajustes.unity");
            var audio = Object.FindFirstObjectByType<SoundManager>();
            audio.sfxSlider = StyleSliders(audio.musicSlider, audio.sfxSlider);
            Relabel(Object.FindFirstObjectByType<Canvas>().gameObject);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            foreach (string level in SceneFlow.Levels)
            {
                scene = EditorSceneManager.OpenScene("Assets/Scenes/" + level + ".unity");
                var hud = Object.FindObjectsByType<HealthBar>(FindObjectsSortMode.None).First(b => !b.enemyBar);
                if (hud.valueText.GetComponent<Outline>() == null)
                {
                    var outline = hud.valueText.gameObject.AddComponent<Outline>();
                    outline.effectColor = Color.black; outline.effectDistance = new Vector2(1, -1);
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            File.WriteAllText("Logs/SettingsVisualCheck.txt", "Sliders etiquetados y estilizados; botones Controles y Volver; HUD con contraste.\n");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }
    private static Slider StyleSliders(Slider music, Slider sfx)
    {
        if (sfx.transform.Find("Etiqueta volumen") == null)
        {
            var rect = (RectTransform)sfx.transform;
            var clone = Object.Instantiate(music.gameObject, sfx.transform.parent).GetComponent<Slider>();
            clone.name = sfx.name;
            var cloneRect = (RectTransform)clone.transform;
            cloneRect.anchorMin = rect.anchorMin; cloneRect.anchorMax = rect.anchorMax;
            cloneRect.pivot = rect.pivot; cloneRect.anchoredPosition = rect.anchoredPosition;
            cloneRect.sizeDelta = rect.sizeDelta; cloneRect.localScale = rect.localScale;
            clone.onValueChanged = sfx.onValueChanged;
            clone.value = sfx.value;
            Object.DestroyImmediate(sfx.gameObject); sfx = clone;
        }
        VolumeLabel(music, "Música"); VolumeLabel(sfx, "Efectos");
        return sfx;
    }
    private static void VolumeLabel(Slider slider, string text)
    {
        var label = slider.transform.Find("Etiqueta volumen");
        if (label == null) label = new GameObject("Etiqueta volumen", typeof(RectTransform), typeof(Text)).transform;
        label.SetParent(slider.transform, false);
        var rect = (RectTransform)label; rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, 28); rect.sizeDelta = new Vector2(500, 44);
        var field = label.GetComponent<Text>(); field.text = text; field.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        field.fontSize = 30; field.alignment = TextAnchor.MiddleCenter; field.raycastTarget = false;
    }
    private static void Relabel(GameObject root)
    {
        foreach (var button in root.GetComponentsInChildren<Button>(true))
        {
            string text = button.name == "Controles" ? "Controles" :
                (button.name == "Volver" || button.name == "Button") ? "Volver" : null;
            if (text == null) continue;
            var label = button.transform.Find("Etiqueta accion");
            if (label == null)
            {
                label = new GameObject("Etiqueta accion", typeof(RectTransform), typeof(Image)).transform;
                label.SetParent(button.transform, false);
                label.GetComponent<Image>().color = new Color(0.31f, 0.24f, 0.21f, 1);
                label.GetComponent<Image>().raycastTarget = false;
                var r = (RectTransform)label; r.anchorMin = new Vector2(0.24f, 0.22f); r.anchorMax = new Vector2(0.9f, 0.78f); r.offsetMin = r.offsetMax = Vector2.zero;
                var field = new GameObject("Texto", typeof(RectTransform), typeof(Text)).GetComponent<Text>(); field.transform.SetParent(label, false);
                field.rectTransform.anchorMin = Vector2.zero; field.rectTransform.anchorMax = Vector2.one; field.rectTransform.offsetMin = field.rectTransform.offsetMax = Vector2.zero;
                field.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); field.fontSize = 30; field.alignment = TextAnchor.MiddleCenter; field.raycastTarget = false;
            }
            label.GetComponentInChildren<Text>().text = text;
        }
    }
}
