using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    // Método para iniciar el juego
    public void Jugar(string nombreEscenaJuego)
    {
        SceneManager.LoadScene(nombreEscenaJuego);
    }

    // Método para salir del juego
    public void Salir()
    {
        Debug.Log("Saliendo del juego..."); // Esto se verá en la consola de Unity al probarlo
        Application.Quit();                  // Esto cierra el juego cuando ya esté compilado/exportado
    }
}
