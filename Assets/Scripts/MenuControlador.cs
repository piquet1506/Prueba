using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuControlador : MonoBehaviour
{
    // Llama a esta función desde el botón Comenzar
    public void JugarJuego()
    {
        // Carga la pantalla de carga (Index 1)
        SceneManager.LoadScene("PantallaCarga"); 
    }

    // Llama a esta función desde el botón Salir (opcional)
    public void SalirDelJuego()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}
