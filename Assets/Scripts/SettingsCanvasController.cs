using UnityEngine;
using UnityEngine.UI;

public class SettingsCanvasController : MonoBehaviour
{
    public GameObject optionsPanel;
    public GameObject soundPanel;
    public GameObject controlsPanel;
    public Slider musicSlider;
    public Slider sfxSlider;
    public GameManagerUI owner;
    public void PlayClick() { if (SoundManager.Instance != null) SoundManager.Instance.PlayClick(); }
    public bool IsOpen => (optionsPanel != null && optionsPanel.activeInHierarchy) || (soundPanel != null && soundPanel.activeInHierarchy) || (controlsPanel != null && controlsPanel.activeInHierarchy);
    public void Hide()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (soundPanel != null) soundPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
    }
    public void Open()
    {
        gameObject.SetActive(true);

        ShowOptions();
        SyncVolumes();
    }
    private void OnEnable() { SyncVolumes(); }
    private void SyncVolumes()
    {
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MusicVolume", 0.8f));
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("SfxVolume", 0.8f));
    }
    public void ShowOptions() { optionsPanel.SetActive(true); soundPanel.SetActive(false); controlsPanel.SetActive(false); }
    public void ShowSound() { optionsPanel.SetActive(false); soundPanel.SetActive(true); controlsPanel.SetActive(false); }
    public void ShowControls() { optionsPanel.SetActive(false); soundPanel.SetActive(false); controlsPanel.SetActive(true); }
    public void SetMusicVolume(float value) { if (SoundManager.Instance != null) SoundManager.Instance.SetMusicVolume(value); }
    public void SetSfxVolume(float value) { if (SoundManager.Instance != null) SoundManager.Instance.SetSfxVolume(value); }
    public void Back()
    {
        if ((soundPanel != null && soundPanel.activeSelf) || (controlsPanel != null && controlsPanel.activeSelf)) ShowOptions();
        else Close();
    }
    public void Close()
    {
        PlayerPrefs.Save();
        Hide();
        if (owner != null) owner.ShowPauseMenu();
    }
}
