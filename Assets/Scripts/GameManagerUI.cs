using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManagerUI : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject pausePanel;
    public GameObject gameOverPanel;
    public GameObject checkpointNotificationPanel; // Opcional para el aviso de guardado

    private bool isPaused = false;
    private bool gameOver;
    private int coins;
    public UnityEngine.UI.Text coinText;
    public SettingsCanvasController settingsCanvas;
    public bool IsPaused => isPaused;

    void Start()
    {
        Time.timeScale = CinematicSequence.IsPlaying ? 0f : 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (checkpointNotificationPanel != null) checkpointNotificationPanel.SetActive(false);
        if (settingsCanvas != null) settingsCanvas.Hide();
        AddCoin(0);
    }

    public void AddCoin() { AddCoin(1); }
    private void AddCoin(int amount)
    {
        coins += amount;
        if (coinText != null) coinText.text = "Monedas: " + coins;
    }

    void Update()
    {
        if (CinematicSequence.BlocksInput) return;
        if (DialogueUI.IsOpen || DialogueUI.LastClosedFrame == Time.frameCount) return;
        // Detectar si presionas Escape para pausar/reanudar
        if (!gameOver && Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsCanvas != null && settingsCanvas.IsOpen) { settingsCanvas.Back(); return; }
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        if (CinematicSequence.BlocksInput) return;
        if (gameOver || DialogueUI.IsOpen) return;
        isPaused = true;
        if (pausePanel != null) pausePanel.SetActive(true);
        Time.timeScale = 0f; // Congela el tiempo del juego
    }

    public void ResumeGame()
    {
        if (gameOver) return;
        isPaused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsCanvas != null) settingsCanvas.Hide();
        Time.timeScale = 1f; // Reanuda el tiempo del juego
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f; // Asegurar que el tiempo corra al reiniciar
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu(string nombreEscenaMenu)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nombreEscenaMenu);
    }

    // Método para activar la pantalla de Game Over (llámalo cuando la vida llegue a 0)
    public void TriggerGameOver()
    {
        gameOver = true;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsCanvas != null) settingsCanvas.Hide();
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    // Método simulado para mostrar el aviso de Checkpoint
    public void ShowCheckpointNotification()
    {
        if (checkpointNotificationPanel != null)
        {
            checkpointNotificationPanel.SetActive(true);
            StartCoroutine(HideCheckpoint());
            // Aquí podrías usar una Corutina para ocultarlo a los 2 segundos, por ahora lo dejamos activo
        }
    }

    private System.Collections.IEnumerator HideCheckpoint()
    {
        yield return new WaitForSecondsRealtime(2f);
        if (checkpointNotificationPanel != null) checkpointNotificationPanel.SetActive(false);
    }

    public void ReturnToMenu() { SceneFlow.Load(SceneFlow.MainMenu); }

    public void OpenSettings()
    {
        if (gameOver || DialogueUI.IsOpen || settingsCanvas == null) return;
        PauseGame();
        if (pausePanel != null) pausePanel.SetActive(false);
        settingsCanvas.Open();
    }
    public void ShowPauseMenu()
    {
        if (!gameOver && pausePanel != null) pausePanel.SetActive(true);
    }
}
