using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class LevelExit : MonoBehaviour
{
    private bool loading;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (loading || CinematicSequence.IsPlaying || !other.CompareTag("Player")) return;
        loading = true;
        SceneFlow.NextLevel();
    }
}
