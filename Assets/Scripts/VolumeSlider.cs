using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class VolumeSlider : MonoBehaviour
{
    public enum AudioChannel { Music, Effects }
    public AudioChannel channel;
    private Slider slider;
    private void OnEnable()
    {
        slider = GetComponent<Slider>();
        slider.onValueChanged.AddListener(Apply);
        SoundManager.VolumesChanged += Synchronize;
        Synchronize();
    }
    private void OnDisable()
    {
        if (slider != null) slider.onValueChanged.RemoveListener(Apply);
        SoundManager.VolumesChanged -= Synchronize;
        PlayerPrefs.Save();
    }
    private void Synchronize()
    {
        slider.SetValueWithoutNotify(PlayerPrefs.GetFloat(channel == AudioChannel.Music ? "MusicVolume" : "SfxVolume", 0.8f));
    }
    public void Apply(float value)
    {
        SoundManager.SetChannelVolume(channel == AudioChannel.Music, value);
    }
}
