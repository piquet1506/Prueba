using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TreeParallaxRepair
{
    [InitializeOnLoadMethod]
    private static void Schedule()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists("Tools/request-tree-parallax") || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete("Tools/request-tree-parallax");
            Run();
        };
    }

    [MenuItem("Tools/Auki/Fijar arboles al escenario")]
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
                var components = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ParallaxBackground>(true)).ToArray();
                foreach (var parallax in components)
                {
                    if (!parallax.name.StartsWith("Arboles_fondo")) continue;
                    report.AppendLine(level + ": arbol fijo: " + parallax.name);
                    Object.DestroyImmediate(parallax);
                }
                var remaining = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ParallaxBackground>(true)).ToArray();
                foreach (var parallax in remaining)
                    report.AppendLine(level + ": parallax conservado: " + parallax.name);
                foreach (var tree in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Where(t => t.name.StartsWith("Arboles_fondo")))
                    if (tree.GetComponentInParent<ParallaxBackground>() != null)
                        throw new System.Exception("El arbol sigue bajo un objeto con parallax: " + tree.name);
                if (!remaining.Any(p => p.name == "fondo 1")) throw new System.Exception("Falta el fondo con parallax en " + level);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/TreeParallaxRepair.txt", report + "OK: arboles sin parallax; fondo conservado en los tres niveles.\n");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }
}
