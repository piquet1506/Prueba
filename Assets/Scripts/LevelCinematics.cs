using UnityEngine;
using UnityEngine.Video;

public class LevelCinematics : MonoBehaviour
{
    [Header("Inicio (activar solo en el primer nivel)")]
    public bool playOnStart;
    public VideoClip openingCinematic;
    [Header("Al terminar este nivel")]
    public VideoClip endingCinematic;
    [Min(0)] public float fadeDuration = 0.6f;

    private void Awake()
    {
        if (playOnStart && !CinematicSequence.IsPlaying)
            CinematicSequence.Begin(openingCinematic, null, fadeDuration, true);
    }
    public void FinishLevel(string destination)
    {
        CinematicSequence.Begin(endingCinematic, destination, fadeDuration, false);
    }
}
