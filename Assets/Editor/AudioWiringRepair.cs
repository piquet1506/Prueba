using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public static class AudioWiringRepair
{
    [InitializeOnLoadMethod]
    private static void Schedule()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists("Tools/request-audio-wiring") || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete("Tools/request-audio-wiring"); Run();
        };
    }
    [MenuItem("Tools/Auki/Reparar solo conexiones de volumen")]
    public static void Run()
    {
        if (!EditorSceneManager.SaveOpenScenes()) return;
        var setup = EditorSceneManager.GetSceneManagerSetup();
        var report = new StringBuilder(); var tiles = new StringBuilder();
        try
        {
            foreach (var path in new[] { "Assets/Prefabs/Menus/Canvas.prefab", "Assets/Prefabs/AjustesEnJuego.prefab" })
            {
                if (!File.Exists(path)) continue;
                var root = PrefabUtility.LoadPrefabContents(path);
                Wire(root, report);
                PrefabUtility.SaveAsPrefabAsset(root, path); PrefabUtility.UnloadPrefabContents(root);
            }
            foreach (var path in Directory.GetFiles("Assets/Scenes", "*.unity"))
            {
                var scene = EditorSceneManager.OpenScene(path);
                report.AppendLine(path);
                foreach (var root in scene.GetRootGameObjects()) Wire(root, report);
                foreach (var tilemap in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
                {
                    var grid = tilemap.layoutGrid;
                    tiles.AppendLine(path + " :: " + tilemap.name + " layer=" + LayerMask.LayerToName(tilemap.gameObject.layer));
                    tiles.AppendLine("Grid=" + grid.name + " cell=" + grid.cellSize + " gridScale=" + grid.transform.lossyScale + " tilemapScale=" + tilemap.transform.lossyScale + " anchor=" + tilemap.tileAnchor);
                    tiles.AppendLine("Components=" + string.Join(", ", tilemap.GetComponents<Component>().Select(c => c.GetType().Name)));
                    var used = new TileBase[tilemap.GetUsedTilesCount()]; tilemap.GetUsedTilesNonAlloc(used);
                    foreach (var tile in used.OfType<Tile>())
                    {
                        var sprite = tile.sprite;
                        tiles.AppendLine(AssetDatabase.GetAssetPath(tile) + " sprite=" + AssetDatabase.GetAssetPath(sprite) + " rect=" + sprite.rect + " ppu=" + sprite.pixelsPerUnit + " collider=" + tile.colliderType);
                    }
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            File.WriteAllText("Logs/AudioWiring.txt", report.ToString());
            File.WriteAllText("Logs/TilemapInspection.txt", tiles.ToString());
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }
    private static void Wire(GameObject root, StringBuilder report)
    {
        foreach (var slider in root.GetComponentsInChildren<Slider>(true))
        {
            var names = Enumerable.Range(0, slider.onValueChanged.GetPersistentEventCount()).Select(slider.onValueChanged.GetPersistentMethodName).ToArray();
            var settings = slider.GetComponentInParent<SettingsCanvasController>(true);
            bool music = names.Contains("SetMusicVolume") || slider.name == "Musica" || (settings != null && settings.musicSlider == slider);
            bool effects = names.Contains("SetSfxVolume") || slider.name == "Botones" || (settings != null && settings.sfxSlider == slider);
            if (!music && !effects) continue;
            var binding = slider.GetComponent<VolumeSlider>(); if (binding == null) binding = slider.gameObject.AddComponent<VolumeSlider>();
            binding.channel = music ? VolumeSlider.AudioChannel.Music : VolumeSlider.AudioChannel.Effects;
            for (int i = slider.onValueChanged.GetPersistentEventCount() - 1; i >= 0; i--)
                if (new[] { "SetMusicVolume", "SetSfxVolume" }.Contains(slider.onValueChanged.GetPersistentMethodName(i)))
                    UnityEditor.Events.UnityEventTools.RemovePersistentListener(slider.onValueChanged, i);
            slider.minValue = 0; slider.maxValue = 1; slider.wholeNumbers = false;
            // The full track accepts pointer input; decorations cannot intercept it.
            foreach (var graphic in slider.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            var hit = slider.GetComponent<Image>(); if (hit == null) { hit = slider.gameObject.AddComponent<Image>(); hit.color = Color.clear; }
            hit.raycastTarget = true;
            EditorUtility.SetDirty(slider); EditorUtility.SetDirty(binding);
            if (PrefabUtility.IsPartOfPrefabInstance(slider)) { PrefabUtility.RecordPrefabInstancePropertyModifications(slider); PrefabUtility.RecordPrefabInstancePropertyModifications(binding); }
            report.AppendLine(slider.name + " -> " + binding.channel + " (VolumeSlider)");
        }
    }
}
