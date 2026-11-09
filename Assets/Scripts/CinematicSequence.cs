using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

// Exists only while a cinematic is running; survives the level load to keep the screen black.
public class CinematicSequence : MonoBehaviour
{
    public static bool IsPlaying { get; private set; }
    public static int LastClosedFrame { get; private set; } = -1;
    public static bool BlocksInput => IsPlaying || LastClosedFrame == Time.frameCount;
    public const string MissingClipMessage = "Colocar cinematica en el editor";
    private CanvasGroup overlay;
    private RawImage picture;
    private Text message;
    private VideoPlayer video;
    private RenderTexture texture;
    private AudioSource music;
    private bool resumeMusic, skip, ended, failed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState() { IsPlaying = false; LastClosedFrame = -1; }

    public static void Begin(VideoClip clip, string destination, float fadeDuration, bool opening)
    {
        if (IsPlaying) return;
        if (!string.IsNullOrEmpty(destination) && !Application.CanStreamedLevelBeLoaded(destination))
        { Debug.LogError("Destino de cinematica no disponible: " + destination); return; }
        IsPlaying = true;
        var runner = new GameObject("Transicion cinematica").AddComponent<CinematicSequence>();
        DontDestroyOnLoad(runner.gameObject);
        runner.StartCoroutine(runner.Play(clip, destination, Mathf.Max(0, fadeDuration), opening));
    }
    public void Skip() { skip = true; }
    private void Update() { if (Input.GetKeyDown(KeyCode.Escape)) Skip(); }

    private IEnumerator Play(VideoClip clip, string destination, float duration, bool opening)
    {
        Time.timeScale = 0;
        BuildOverlay(); overlay.alpha = opening ? 1 : 0;
        // Let scene Awake/Start initialize audio and UI before using their references.
        yield return null;
        Time.timeScale = 0;
        music = SoundManager.Instance != null ? SoundManager.Instance.musicAudioSource : null;
        resumeMusic = music != null && music.isPlaying;
        if (resumeMusic) music.Pause();
        if (!opening) yield return Fade(1, duration);
        if (!skip)
        {
            if (clip == null) yield return MissingClip();
            else yield return PlayVideo(clip);
        }
        picture.gameObject.SetActive(false); message.gameObject.SetActive(false);
        if (video != null) video.Stop();
        if (!string.IsNullOrEmpty(destination))
        {
            var loading = SceneManager.LoadSceneAsync(destination);
            while (!loading.isDone) yield return null;
            yield return null;
        }
        yield return Fade(0, duration);
        Destroy(gameObject);
    }

    private IEnumerator MissingClip()
    {
        message.text = MissingClipMessage;
        message.gameObject.SetActive(true);
        float until = Time.realtimeSinceStartup + 3;
        while (!skip && Time.realtimeSinceStartup < until) yield return null;
        message.gameObject.SetActive(false);
    }
    private IEnumerator PlayVideo(VideoClip clip)
    {
        video = gameObject.AddComponent<VideoPlayer>();
        video.playOnAwake = false; video.isLooping = false;
        video.source = VideoSource.VideoClip; video.clip = clip;
        video.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        video.renderMode = VideoRenderMode.RenderTexture;
        texture = new RenderTexture(1920, 1080, 0); texture.Create();
        video.targetTexture = texture; picture.texture = texture;
        video.aspectRatio = VideoAspectRatio.FitInside;
        video.audioOutputMode = VideoAudioOutputMode.Direct;
        video.errorReceived += OnVideoError;
        video.loopPointReached += OnVideoEnd;
        video.Prepare();
        float deadline = Time.realtimeSinceStartup + 15;
        while (!video.isPrepared && !failed && !skip && Time.realtimeSinceStartup < deadline) yield return null;
        if (skip) yield break;
        if (!video.isPrepared || failed)
        {
            Debug.LogWarning("No se pudo preparar la cinematica; se continuara al juego.");
            video.Stop(); yield return MissingClip(); yield break;
        }
        for (ushort track = 0; track < video.audioTrackCount; track++)
            video.SetDirectAudioVolume(track, PlayerPrefs.GetFloat("SfxVolume", .8f));
        picture.gameObject.SetActive(true);
        video.Play();
        deadline = Time.realtimeSinceStartup + Mathf.Max(15, (float)video.length + 10);
        while (!ended && !failed && !skip && Time.realtimeSinceStartup < deadline) yield return null;
        picture.gameObject.SetActive(false); video.Stop();
        if (failed && !skip) yield return MissingClip();
    }
    private void OnVideoEnd(VideoPlayer player) { ended = true; }
    private void OnVideoError(VideoPlayer player, string error) { failed = true; Debug.LogWarning("Cinematica: " + error); }

    private IEnumerator Fade(float target, float duration)
    {
        float start = overlay.alpha;
        for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        { overlay.alpha = Mathf.Lerp(start, target, elapsed / duration); yield return null; }
        overlay.alpha = target;
    }
    private void BuildOverlay()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32760;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        gameObject.AddComponent<GraphicRaycaster>();
        overlay = gameObject.AddComponent<CanvasGroup>();
        var background = Child("Fundido negro").gameObject.AddComponent<Image>(); background.color = Color.black;
        picture = Child("Video").gameObject.AddComponent<RawImage>(); picture.raycastTarget = false;
        var fitter = picture.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent; fitter.aspectRatio = 16f / 9f;
        picture.gameObject.SetActive(false);
        message = Child("Aviso cinematica").gameObject.AddComponent<Text>();
        message.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); message.fontSize = 42;
        message.alignment = TextAnchor.MiddleCenter; message.color = Color.white; message.raycastTarget = false;
        message.gameObject.SetActive(false);
    }
    private RectTransform Child(string label)
    {
        var rect = new GameObject(label, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(transform, false); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
    }
    private void OnDestroy()
    {
        if (video != null) { video.errorReceived -= OnVideoError; video.loopPointReached -= OnVideoEnd; video.Stop(); }
        if (texture != null) { texture.Release(); Destroy(texture); }
        if (resumeMusic && music != null) music.UnPause();
        Time.timeScale = 1; IsPlaying = false; LastClosedFrame = Time.frameCount;
    }
}
