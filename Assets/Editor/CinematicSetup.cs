using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CinematicSetup
{
    [InitializeOnLoadMethod]
    private static void Schedule()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists("Tools/request-cinematic-setup") || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete("Tools/request-cinematic-setup"); Run();
        };
    }
    [MenuItem("Tools/Auki/Configurar cinematicas y restaurar parallax")]
    public static void Run()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveOpenScenes()) return;
        var setup = EditorSceneManager.GetSceneManagerSetup(); var report = new StringBuilder();
        try
        {
            foreach (string level in SceneFlow.Levels)
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + level + ".unity");
                var config = Object.FindFirstObjectByType<LevelCinematics>();
                if (config == null) config = new GameObject("Cinematicas del nivel").AddComponent<LevelCinematics>();
                config.playOnStart = level == SceneFlow.Levels[0];
                foreach (var sprite in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SpriteRenderer>(true)))
                {
                    bool background = sprite.name == "fondo 1";
                    if (!background && !sprite.name.StartsWith("Arboles_fondo")) continue;
                    var parallax = sprite.GetComponent<ParallaxBackground>();
                    if (parallax == null) parallax = sprite.gameObject.AddComponent<ParallaxBackground>();
                    parallax.enabled = true;
                    parallax.horizontalFollow = background ? .8f : .35f;
                    parallax.verticalFollow = background ? .85f : .3f;
                    parallax.repeatHorizontally = background;
                    report.AppendLine(level + ": " + sprite.name + " parallax=" + parallax.horizontalFollow + "/" + parallax.verticalFollow);
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                report.AppendLine(level + ": inicio=" + config.playOnStart + "; cinematicas listas para asignar.");
            }
            File.WriteAllText("Logs/CinematicSetup.txt", report.ToString());
        }
        finally { if (!Application.isBatchMode) EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }
}
