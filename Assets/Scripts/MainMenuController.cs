using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [Header("Paneles (dentro del mismo Canvas)")]
    [Tooltip("Panel de opciones que se mostrará al pulsar Ajustes")]
    [SerializeField] private GameObject optionsPanel;

    [Tooltip("Panel de controles (teclas, mando, etc.)")]
    [SerializeField] private GameObject controlsPanel;

    [Tooltip("Panel de sonido (volumen música, efectos, etc.)")]
    [SerializeField] private GameObject soundPanel;

    [Tooltip("(Opcional) Panel con los botones del menú principal, para ocultarlo mientras se ven las opciones")]
    [SerializeField] private GameObject mainMenuPanel;

    private void Start()
    {
        // Todos los paneles secundarios empiezan cerrados
        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        if (controlsPanel != null)
            controlsPanel.SetActive(false);

        if (soundPanel != null)
            soundPanel.SetActive(false);
    }

    // ---------- OPCIONES ----------
    public void OpenSettings()
    {
        if (optionsPanel == null)
        {
            Debug.LogWarning("MainMenuController: no hay un Options Panel asignado en el Inspector.");
            return;
        }

        optionsPanel.SetActive(true);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
    }

    public void CloseSettings()
    {
        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    // ---------- CONTROLES ----------
    public void OpenControls()
    {
        if (controlsPanel == null)
        {
            Debug.LogWarning("MainMenuController: no hay un Controls Panel asignado en el Inspector.");
            return;
        }

        controlsPanel.SetActive(true);

        if (optionsPanel != null)
            optionsPanel.SetActive(false);
    }

    public void CloseControls()
    {
        if (controlsPanel != null)
            controlsPanel.SetActive(false);

        if (optionsPanel != null)
            optionsPanel.SetActive(true);
    }

    // ---------- SONIDO ----------
    public void OpenSound()
    {
        if (soundPanel == null)
        {
            Debug.LogWarning("MainMenuController: no hay un Sound Panel asignado en el Inspector.");
            return;
        }

        soundPanel.SetActive(true);

        // Oculta el panel de opciones mientras se ve el de sonido
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
    }

    public void CloseSound()
    {
        if (soundPanel != null)
            soundPanel.SetActive(false);

        // Vuelve al panel de opciones
        if (optionsPanel != null)
            optionsPanel.SetActive(true);
    }

    // ---------- ESCENAS ----------
    public void ReturnToMainMenu()
    {
        SceneFlow.Load(SceneFlow.MainMenu);
    }

    public void PlayGame(string nombreEscenaNivel)
    {
        SceneFlow.LoadLevel(nombreEscenaNivel);
    }

    public void GoToLoadingScreen(string nombreEscenaCarga)
    {
        SceneFlow.Load(nombreEscenaCarga);
    }

    // ---------- UTILIDADES ----------
    public void TogglePanel(GameObject panel)
    {
        if (panel != null)
            panel.SetActive(!panel.activeSelf);
    }

    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}