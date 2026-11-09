using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
    public static event System.Action VolumesChanged;
    public AudioSource musicAudioSource;
    public AudioSource sfxAudioSource;
    public AudioClip backgroundMusic;
    public AudioClip clickSound;
    public Slider musicSlider;
    public Slider sfxSlider;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance() { Instance = null; VolumesChanged = null; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (musicAudioSource != null)
            {
                musicAudioSource.volume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
                musicAudioSource.clip = backgroundMusic;
                musicAudioSource.loop = true;
                if (backgroundMusic != null) musicAudioSource.Play();
            }
            if (sfxAudioSource != null) sfxAudioSource.volume = PlayerPrefs.GetFloat("SfxVolume", 0.8f);
        }
        else if (Instance != this)
        {
            // Keep this scene's event targets alive; forward changes to the persistent audio.
            if (musicAudioSource != null) { musicAudioSource.Stop(); musicAudioSource.enabled = false; }
            if (sfxAudioSource != null) { sfxAudioSource.Stop(); sfxAudioSource.enabled = false; }
        }
    }

    private void Start()
    {
        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MusicVolume", 0.8f));
            if (musicSlider.GetComponent<VolumeSlider>() == null && musicSlider.onValueChanged.GetPersistentEventCount() == 0)
                musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }
        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("SfxVolume", 0.8f));
            if (sfxSlider.GetComponent<VolumeSlider>() == null && sfxSlider.onValueChanged.GetPersistentEventCount() == 0)
                sfxSlider.onValueChanged.AddListener(SetSfxVolume);
        }
    }

    public void SetMusicVolume(float value)
    {
        SetChannelVolume(true, value);
    }
    public void SetSfxVolume(float value)
    {
        SetChannelVolume(false, value);
    }
    public static void SetChannelVolume(bool music, float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(music ? "MusicVolume" : "SfxVolume", value);
        if (Instance != null)
        {
            var source = music ? Instance.musicAudioSource : Instance.sfxAudioSource;
            if (source != null) source.volume = value;
        }
        VolumesChanged?.Invoke();
    }
    public void PlayClick()
    {
        if (Instance != null && Instance.sfxAudioSource != null && Instance.clickSound != null)
            Instance.sfxAudioSource.PlayOneShot(Instance.clickSound);
    }
    private void OnDestroy()
    {
        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(SetSfxVolume);
        if (Instance == this) Instance = null;
    }
}
