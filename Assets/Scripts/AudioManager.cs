using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    // Método para controlar el volumen general mediante un Slider
    public void SetVolume(float sliderValue)
    {
        // Controla el volumen maestro de todo el juego instantáneamente
        AudioListener.volume = sliderValue;
    }
}