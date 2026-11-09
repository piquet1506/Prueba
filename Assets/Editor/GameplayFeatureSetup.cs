using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public static class GameplayFeatureSetup
{
    private const string Hud = "Assets/DiseñosJuego/Hud/";
    private const string Anim = "Assets/DiseñosJuego/Animaciones/";
    private static Sprite[] playerFrames, enemyFrames;
    private static string report;

    [MenuItem("Tools/Auki/Integrar vida dialogos y ajustes")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Salir de Play antes de integrar.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveOpenScenes()) throw new Exception("No se pudieron guardar las escenas abiertas.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        report = "Integracion de funciones (sin generar ejecutable)\n";
        try
        {
            Directory.CreateDirectory("Assets/Prefabs"); Directory.CreateDirectory("Assets/animations/Gameplay");
            AssetDatabase.Refresh();
            playerFrames = Enumerable.Range(2, 9).Select(i => ImportSprite(Hud + "vidas" + i + ".png", new Rect(0, 130, 320, 130))).ToArray();
            enemyFrames = Enumerable.Range(1, 10).Select(i => ImportSprite(Hud + "villanos" + i + ".png", new Rect(10, 50, 430, 170))).ToArray();
            ConfigureAnimations();
            var settings = EditorSceneManager.OpenScene("Assets/Scenes/Ajustes.unity");
            FixRaycasts(UnityEngine.Object.FindFirstObjectByType<Canvas>());
            EditorSceneManager.SaveScene(settings);
            var audio = UnityEngine.Object.FindFirstObjectByType<SoundManager>();
            PrefabUtility.SaveAsPrefabAsset(audio.gameObject, "Assets/Prefabs/AudioDelJuego.prefab");
            BuildSettingsPrefab();
            foreach (string name in SceneFlow.Levels)
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
                ConfigureLevel(name);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/FeatureSetup.txt", report + "SUCCESS\n");
        }
        catch (Exception ex) { File.WriteAllText("Logs/FeatureSetup.txt", report + ex); Debug.LogException(ex); throw; }
        finally
        {
            if (Application.isBatchMode) EditorSceneManager.OpenScene("Assets/Scenes/Menu Principal.unity");
            else EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }

    private static Sprite ImportSprite(string path, Rect? crop = null)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null) throw new Exception("No se encontro " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.spritePixelsPerUnit = 32;
        if (crop.HasValue)
        {
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritesheet = new[] { new SpriteMetaData { name = Path.GetFileNameWithoutExtension(path), rect = crop.Value, alignment = 0, pivot = new Vector2(0.5f, 0.5f) } };
        }
        else importer.spriteImportMode = SpriteImportMode.Single;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First();
    }
    private static AnimationClip Clip(string filename, int width, int count, int fps, bool loop)
    {
        string path = Anim + filename + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = 32; importer.mipmapEnabled = false;
        importer.spritesheet = Enumerable.Range(0, count).Select(i => new SpriteMetaData
        { name = filename + "_" + i.ToString("D2"), rect = new Rect(i * width, 0, width, 32), alignment = 0, pivot = new Vector2(0.5f, 0.5f) }).ToArray();
        importer.SaveAndReimport();
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        string clipPath = "Assets/animations/Gameplay/" + filename + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, clipPath); }
        clip.frameRate = fps;
        var keys = sprites.Select((s, i) => new ObjectReferenceKeyframe { time = (float)i / fps, value = s }).ToArray();
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
        var options = AnimationUtility.GetAnimationClipSettings(clip); options.loopTime = loop; AnimationUtility.SetAnimationClipSettings(clip, options);
        EditorUtility.SetDirty(clip);
        return clip;
    }
    private static void ConfigureAnimations()
    {
        var sneak = Clip("sigilo-Sheet", 32, 22, 15, true);
        var hurt = Clip("Daño-Sheet", 32, 5, 14, false);
        var death = Clip("morir-Sheet", 47, 15, 12, false);
        string path = "Assets/animations/Gameplay/AukiGameplay.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller != null) return;
        controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
        foreach (string p in new[] { "EnSuelo", "Sigilo", "Agarrado" }) controller.AddParameter(p, AnimatorControllerParameterType.Bool);
        foreach (string p in new[] { "Atacar", "Herido", "Morir" }) controller.AddParameter(p, AnimatorControllerParameterType.Trigger);
        var sm = controller.layers[0].stateMachine;
        var idle = sm.AddState("Idle"); idle.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/animations/Idle.anim"); sm.defaultState = idle;
        var walk = sm.AddState("Caminar"); walk.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/animations/Caminar.anim");
        var jump = sm.AddState("Saltar"); jump.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/animations/Saltar.anim");
        var stealth = sm.AddState("Sigilo"); stealth.motion = sneak;
        var grip = sm.AddState("Agarrado"); grip.motion = jump.motion; grip.speed = 0;
        var attack = sm.AddState("Atacar"); attack.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/animations/Atacar.anim");
        var damage = sm.AddState("Herido"); damage.motion = hurt;
        var dead = sm.AddState("Morir"); dead.motion = death;
        foreach (var state in new[] { idle, walk, jump, stealth, grip })
        {
            if (state != grip) Conditions(state.AddTransition(grip), ("Agarrado", AnimatorConditionMode.If, 0));
            if (state != jump) Conditions(state.AddTransition(jump), ("Agarrado", AnimatorConditionMode.IfNot, 0), ("EnSuelo", AnimatorConditionMode.IfNot, 0));
            if (state != stealth) Conditions(state.AddTransition(stealth), ("EnSuelo", AnimatorConditionMode.If, 0), ("Sigilo", AnimatorConditionMode.If, 0));
            if (state != walk) Conditions(state.AddTransition(walk), ("EnSuelo", AnimatorConditionMode.If, 0), ("Sigilo", AnimatorConditionMode.IfNot, 0), ("Velocidad", AnimatorConditionMode.Greater, 0.1f));
            if (state != idle) Conditions(state.AddTransition(idle), ("EnSuelo", AnimatorConditionMode.If, 0), ("Sigilo", AnimatorConditionMode.IfNot, 0), ("Velocidad", AnimatorConditionMode.Less, 0.1f));
        }
        Conditions(sm.AddAnyStateTransition(dead), ("Morir", AnimatorConditionMode.If, 0));
        Conditions(sm.AddAnyStateTransition(damage), ("Herido", AnimatorConditionMode.If, 0));
        Conditions(sm.AddAnyStateTransition(attack), ("Atacar", AnimatorConditionMode.If, 0));
        foreach (var state in new[] { damage, attack }) { var t = state.AddTransition(idle); t.hasExitTime = true; t.exitTime = 1; t.duration = 0; }
    }
    private static void Conditions(AnimatorStateTransition t, params (string, AnimatorConditionMode, float)[] conditions)
    {
        t.duration = 0; t.hasExitTime = false; t.canTransitionToSelf = false;
        foreach (var c in conditions) t.AddCondition(c.Item2, c.Item3, c.Item1);
    }

    private static void FixRaycasts(Canvas canvas)
    {
        foreach (var graphic in canvas.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = graphic.GetComponent<Button>() != null;
        foreach (var slider in canvas.GetComponentsInChildren<Slider>(true))
        {
            slider.minValue = 0; slider.maxValue = 1; slider.wholeNumbers = false;
            var hitArea = slider.GetComponent<Image>(); if (hitArea == null) hitArea = slider.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear; hitArea.raycastTarget = true;
            if (slider.handleRect != null && slider.handleRect.GetComponent<Image>() != null) slider.handleRect.GetComponent<Image>().raycastTarget = true;
        }
    }
    private static void BuildSettingsPrefab()
    {
        var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        var copy = UnityEngine.Object.Instantiate(canvas.gameObject); copy.name = "Ajustes en juego";
        var control = copy.AddComponent<SettingsCanvasController>();
        control.optionsPanel = copy.transform.Find("PanelOpciones").gameObject;
        control.soundPanel = copy.transform.Find("PanelSubSonido").gameObject;
        control.controlsPanel = copy.transform.Find("PanelSubControle").gameObject;
        foreach (var button in copy.GetComponentsInChildren<Button>(true))
        {
            button.onClick = new Button.ButtonClickedEvent();
            UnityAction action = control.ShowOptions;
            if (button.name == "Sonido") action = control.ShowSound;
            else if (button.name == "Controles") action = control.ShowControls;
            else if (button.transform.parent == control.optionsPanel.transform || button.name == "Salir") action = control.Close;
            UnityEventTools.AddPersistentListener(button.onClick, action);
        }
        foreach (var slider in copy.GetComponentsInChildren<Slider>(true))
        {
            slider.onValueChanged = new Slider.SliderEvent();
            if (slider.name == "Musica") { control.musicSlider = slider; UnityEventTools.AddPersistentListener(slider.onValueChanged, control.SetMusicVolume); }
            else { control.sfxSlider = slider; UnityEventTools.AddPersistentListener(slider.onValueChanged, control.SetSfxVolume); }
        }
        var scaler = copy.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
        copy.GetComponent<Canvas>().sortingOrder = 50;
        FixRaycasts(copy.GetComponent<Canvas>());
        Label(control.controlsPanel.transform, "A/D: mover   Espacio: saltar\nCtrl / C: sigilo   Shift + pared: agarrarse\nW/S: trepar   Espacio: salto de pared\nJ: atacar   E: hablar / continuar   Esc: volver", new Vector2(0, 0), new Vector2(900, 220), 25);
        control.ShowOptions(); copy.SetActive(false);
        PrefabUtility.SaveAsPrefabAsset(copy, "Assets/Prefabs/AjustesEnJuego.prefab");
        UnityEngine.Object.DestroyImmediate(copy);
    }

    private static T Ensure<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }
    private static void ConfigureLevel(string name)
    {
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerControlerC>();
        var health = Ensure<Health>(player.gameObject); health.maximum = 100; health.invulnerability = 0.65f;
        player.animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/animations/Gameplay/AukiGameplay.controller");
        var capsule = Ensure<CapsuleCollider2D>(player.gameObject); capsule.size = new Vector2(0.68f, 0.95f); capsule.offset = new Vector2(0, -0.02f);
        var old = player.GetComponent<BoxCollider2D>();
        if (old != null) UnityEngine.Object.DestroyImmediate(old);
        var friction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Prefabs/SinFriccion.physicsMaterial2D");
        if (friction == null) { friction = new PhysicsMaterial2D("SinFriccion") { friction = 0, bounciness = 0 }; AssetDatabase.CreateAsset(friction, "Assets/Prefabs/SinFriccion.physicsMaterial2D"); }
        capsule.sharedMaterial = friction;
        var rb = player.GetComponent<Rigidbody2D>(); rb.interpolation = RigidbodyInterpolation2D.Interpolate; rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        foreach (var tile in UnityEngine.Object.FindObjectsByType<TilemapCollider2D>(FindObjectsSortMode.None))
        {
            var groundRb = Ensure<Rigidbody2D>(tile.gameObject); groundRb.bodyType = RigidbodyType2D.Static;
            var composite = Ensure<CompositeCollider2D>(tile.gameObject); composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            tile.compositeOperation = Collider2D.CompositeOperation.Merge; composite.sharedMaterial = friction;
        }
        var ui = UnityEngine.Object.FindFirstObjectByType<GameManagerUI>();
        var canvas = ui.GetComponent<Canvas>();
        if (ui.GetComponentInChildren<HealthBar>(true) == null)
        {
            var bar = CreateBar(canvas.transform, health, playerFrames, false);
            var rect = (RectTransform)bar.transform; rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(28, -22); rect.sizeDelta = new Vector2(260, 106);
            bar.valueText = Label(bar.transform, "100 / 100", new Vector2(15, -50), new Vector2(220, 30), 20);
        }
        if (ui.coinText != null)
        {
            var rect = ui.coinText.rectTransform; rect.anchorMin = rect.anchorMax = new Vector2(1, 1); rect.anchoredPosition = new Vector2(-130, -40);
        }
        foreach (var text in canvas.GetComponentsInChildren<Text>(true))
            if (text.text.StartsWith("A/D o flechas")) { text.text = "A/D: mover | Espacio: saltar | J: atacar | Ctrl/C: sigilo | Shift: pared | E: hablar | Esc: pausa"; text.fontSize = 20; }
        var settings = ui.settingsCanvas;
        if (settings == null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/AjustesEnJuego.prefab"));
            settings = instance.GetComponent<SettingsCanvasController>(); settings.owner = ui; ui.settingsCanvas = settings;
            PrefabUtility.RecordPrefabInstancePropertyModifications(settings);
            var pauseButtons = ui.pausePanel.GetComponentsInChildren<Button>(true);
            foreach (var b in pauseButtons)
            {
                var rect = (RectTransform)b.transform;
                if (b.name == "Continuar") rect.anchoredPosition = new Vector2(0, 65);
                else if (b.name == "Reiniciar") rect.anchoredPosition = new Vector2(0, -65);
                else rect.anchoredPosition = new Vector2(0, -130);
            }
            Button(ui.pausePanel.transform, "Ajustes", Vector2.zero, ui.OpenSettings);
        }
        if (UnityEngine.Object.FindFirstObjectByType<SoundManager>() == null)
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/AudioDelJuego.prefab"));
        foreach (var enemy in UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
        {
            var enemyHealth = Ensure<Health>(enemy.gameObject); enemyHealth.maximum = 60; enemyHealth.invulnerability = 0.2f;
            enemy.groundMask = 1 << 6; enemy.attackDamage = 15;
            if (!canvas.GetComponentsInChildren<HealthBar>(true).Any(b => b.target == enemyHealth)) CreateBar(canvas.transform, enemyHealth, enemyFrames, true);
        }
        CreateDialogues(canvas, player, name);
        foreach (var sr in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (sr.name == "fondo 1" || sr.name.StartsWith("Arboles_fondo"))
            {
                var parallax = Ensure<ParallaxBackground>(sr.gameObject);
                parallax.horizontalFollow = sr.name == "fondo 1" ? .8f : .35f;
                parallax.verticalFollow = sr.name == "fondo 1" ? .85f : .3f;
                parallax.repeatHorizontally = sr.name == "fondo 1"; sr.sortingOrder = sr.name == "fondo 1" ? -30 : -15;
            }
            if (sr.name.StartsWith("manzanilla planta") || sr.name.StartsWith("pino ancho"))
            {
                // A separate trigger keeps decorative cover from becoming a movement obstacle.
                if (sr.GetComponentInChildren<CoverZone>() == null)
                {
                    var zone = new GameObject("Zona de sigilo"); zone.transform.SetParent(sr.transform, false);
                    var trigger = zone.AddComponent<BoxCollider2D>(); trigger.isTrigger = true;
                    trigger.size = sr.sprite.bounds.size; zone.AddComponent<CoverZone>();
                }
                foreach (var c in sr.GetComponents<Collider2D>()) c.isTrigger = true;
            }
        }
        int missing = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
        if (missing > 0) throw new Exception(name + ": scripts faltantes " + missing);
        report += name + ": jugador, HUD, " + canvas.GetComponentsInChildren<HealthBar>(true).Count(b => b.enemyBar) + " barras enemigas, dialogos, Canvas de ajustes, audio y paralaje conectados\n";
    }
    private static HealthBar CreateBar(Transform parent, Health target, Sprite[] frames, bool enemy)
    {
        var go = new GameObject(enemy ? "Vida " + target.name : "Vida Auki", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(HealthBar)); go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform; rect.sizeDelta = enemy ? new Vector2(115, 46) : new Vector2(260, 106);
        var bar = go.GetComponent<HealthBar>(); bar.target = target; bar.frames = frames; bar.enemyBar = enemy;
        bar.picture = go.GetComponent<Image>(); bar.picture.sprite = frames[0]; bar.picture.preserveAspect = true; bar.picture.raycastTarget = false;
        bar.visibility = go.GetComponent<CanvasGroup>(); bar.visibility.blocksRaycasts = false; bar.visibility.interactable = false; bar.visibility.alpha = enemy ? 0 : 1;
        return bar;
    }
    private static void CreateDialogues(Canvas canvas, PlayerControlerC player, string level)
    {
        foreach (var sr in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(s => s.name == "npc"))
        {
            var npc = Ensure<NpcDialogue>(sr.gameObject); npc.displayName = level == "Nivel1" ? "Anciano" : "Habitante herido";
            if (npc.lines == null || npc.lines.Length == 0) npc.lines = level == "Nivel1" ?
                new[] { "ANCIANO: ¿Vienes de Q'ara Q'ara?", "AUKI: Traigo respuesta del ayllu.", "ANCIANO: No sabemos qué pasó. Vieron hombres extraños cerca del camino.", "AUKI: ¿Cuántos?", "ANCIANO: No lo sabemos. Pero no eran de aquí." } :
                new[] { "AUKI: ¿Qué ocurrió?", "HOMBRE: Buscaban algo. Preguntaron por una planta.", "AUKI: ¿Coca?", "HOMBRE: No. Preguntaban por una planta antigua.", "AUKI: ¿Dónde está?", "HOMBRE: La yatiri sabe." };
            sr.sortingOrder = 9;
        }
        if (level == "Nivel1" && !UnityEngine.Object.FindObjectsByType<NpcDialogue>(FindObjectsSortMode.None).Any(n => n.displayName == "Yatiri"))
        {
            var sprite = ImportSprite("Assets/DiseñosJuego/Personajes secundarios_/yatiriimujer.png");
            var npc = new GameObject("Yatiri").AddComponent<NpcDialogue>(); npc.displayName = "Yatiri";
            npc.transform.localScale = Vector3.one * 0.1f;
            npc.transform.position = player.transform.position + Vector3.right * 4;
            var sr = npc.gameObject.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = 9;
            Physics2D.SyncTransforms();
            var floor = Physics2D.Raycast(npc.transform.position + Vector3.up * 8, Vector2.down, 30, 1 << 6);
            if (floor) npc.transform.position = new Vector3(npc.transform.position.x, floor.point.y + sprite.bounds.extents.y * 0.1f, 0);
            npc.lines = new[] { "YATIRI: Otra vez has corrido demasiado.", "AUKI: El camino estaba tranquilo.", "YATIRI: Un chasqui que no conoce el cansancio es un mal mensajero.", "YATIRI: Toma.", "AUKI: ¿Coca?", "YATIRI: Para el camino." };
        }
        if (canvas.GetComponent<DialogueUI>() != null) return;
        var dialogue = canvas.gameObject.AddComponent<DialogueUI>();
        dialogue.prompt = Label(canvas.transform, "", new Vector2(0, -225), new Vector2(700, 50), 24);
        var panel = new GameObject("Dialogo NPC", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(canvas.transform, false);
        var rect = (RectTransform)panel.transform; rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0); rect.pivot = new Vector2(0.5f, 0);
        rect.sizeDelta = new Vector2(1040, 190); rect.anchoredPosition = new Vector2(0, 70);
        panel.GetComponent<Image>().color = new Color(0.12f, 0.09f, 0.05f, 0.97f);
        dialogue.dialogueText = Label(panel.transform, "", new Vector2(0, 12), new Vector2(920, 110), 25);
        dialogue.dialogueText.alignment = TextAnchor.MiddleLeft;
        Label(panel.transform, "E: continuar    Esc: cerrar", new Vector2(0, -62), new Vector2(700, 35), 19);
        dialogue.panel = panel; panel.SetActive(false);
    }
    private static Text Label(Transform parent, string value, Vector2 position, Vector2 size, int fontSize)
    {
        var go = new GameObject(string.IsNullOrEmpty(value) ? "Texto" : value, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>(); text.text = value; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = fontSize; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
        text.rectTransform.anchoredPosition = position; text.rectTransform.sizeDelta = size; return text;
    }
    private static void Button(Transform parent, string label, Vector2 position, UnityAction action)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform; rect.sizeDelta = new Vector2(260, 52); rect.anchoredPosition = position;
        go.GetComponent<Image>().color = new Color(0.18f, 0.3f, 0.23f);
        UnityEventTools.AddPersistentListener(go.GetComponent<Button>().onClick, action);
        Label(go.transform, label, Vector2.zero, rect.sizeDelta, 24); go.AddComponent<ButtonSound>();
    }
}
