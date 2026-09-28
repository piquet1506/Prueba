using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void Jugar()
    {
        // Carga la escena de tu juego (debe estar agregada en Build Settings)
        SceneManager.LoadScene(1);
    }

    public void Salir()
    {
        Debug.Log("El jugador ha salido del juego");
        Application.Quit(); // Cierra el juego (nota: no se nota en el editor, solo en la versión final)
    }
}