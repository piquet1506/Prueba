using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("Panel de Pausa")]
    public GameObject pausePanel; // Arrastra aquí tu PausePanel

    private bool isPaused = false;

    void Start()
    {
        // Asegurarnos de que el juego empiece despausado y el panel oculto
        Time.timeScale = 1f;
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    void Update()
    {
        // Detectar si se presiona la tecla Escape para pausar / despausar
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Pause()
    {
        pausePanel.SetActive(true);
        Time.timeScale = 0f; // Congela el tiempo del juego
        isPaused = true;
    }

    public void Resume()
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1f; // Reanuda el tiempo del juego
        isPaused = false;
    }

    // Método para el botón de ir al Menú Principal (asegúrate de que la escena se llame exactamente "menu")
    public void GoToMainMenu(string menuSceneName)
    {
        Time.timeScale = 1f; // IMPORTANTE: restaurar el tiempo antes de cambiar de escena
        SceneManager.LoadScene(menuSceneName);
    }

    // Método opcional para salir del juego desde la pausa
    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}