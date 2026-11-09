using UnityEngine;
using UnityEngine.SceneManagement;

public class PausaController : MonoBehaviour
{
    [Header("Paneles de Menú de Pausa")]
    public GameObject panelPausaPrincipal; // El panel con Continuar, Opciones, Reiniciar, Salir
    public GameObject panelOpciones;       // El panel de la imagen (con Sonido, Controles, Volver)

    [Header("Subpaneles de Opciones")]
    public GameObject panelSubSonido;
    public GameObject panelSubControles;

    private bool juegoPausado = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (juegoPausado)
            {
                Reanudar();
            }
            else
            {
                Pausar();
            }
        }
    }

    public void Pausar()
    {
        if (panelPausaPrincipal != null) panelPausaPrincipal.SetActive(true);
        OcultarTodosLosSubpaneles();
        Time.timeScale = 0f;
        juegoPausado = true;
    }

    public void Reanudar()
    {
        if (panelPausaPrincipal != null) panelPausaPrincipal.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);
        OcultarTodosLosSubpaneles();
        Time.timeScale = 1f;
        juegoPausado = false;
    }

    // --- Navegación dentro de Opciones ---

    public void AbrirOpciones()
    {
        if (panelPausaPrincipal != null) panelPausaPrincipal.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(true);
        OcultarTodosLosSubpaneles();
    }

    public void AbrirSubSonido()
    {
        OcultarTodosLosSubpaneles();
        if (panelSubSonido != null) panelSubSonido.SetActive(true);
    }

    public void AbrirSubControles()
    {
        OcultarTodosLosSubpaneles();
        if (panelSubControles != null) panelSubControles.SetActive(true);
    }

    public void VolverAMenuPausa()
    {
        OcultarTodosLosSubpaneles();
        if (panelOpciones != null) panelOpciones.SetActive(false);
        if (panelPausaPrincipal != null) panelPausaPrincipal.SetActive(true);
    }

    private void OcultarTodosLosSubpaneles()
    {
        if (panelSubSonido != null) panelSubSonido.SetActive(false);
        if (panelSubControles != null) panelSubControles.SetActive(false);
    }

    // --- Acciones de Juego ---

    public void ReiniciarNivel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void SalirAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu Principal");
    }
}