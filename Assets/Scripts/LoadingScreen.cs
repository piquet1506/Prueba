using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    [Header("Configuración")]
    public Slider barraProgreso;
    public string nombreNivelACargar = "Nivel1";

    void Start()
    {
        Time.timeScale = 1f;
        nombreNivelACargar = SceneFlow.ConsumeLevel(nombreNivelACargar);
        if (nombreNivelACargar == SceneManager.GetActiveScene().name ||
            !Application.CanStreamedLevelBeLoaded(nombreNivelACargar))
        {
            Debug.LogError("Destino de carga no valido: " + nombreNivelACargar);
            return;
        }
        StartCoroutine(CargarNivelAsync());
    }

    IEnumerator CargarNivelAsync()
    {
        AsyncOperation operacion = SceneManager.LoadSceneAsync(nombreNivelACargar);

        while (!operacion.isDone)
        {
            // Normaliza el progreso de carga de 0 a 1
            float progreso = Mathf.Clamp01(operacion.progress / 0.9f);

            if (barraProgreso != null)
            {
                barraProgreso.value = progreso;
            }

            yield return null;
        }
    }
}
