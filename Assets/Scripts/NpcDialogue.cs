using UnityEngine;

public class NpcDialogue : MonoBehaviour
{
    public string displayName = "Habitante";
    public float interactionRange = 2.2f;
    [TextArea(2, 5)] public string[] lines;
    public bool InRange(Transform player) => player != null && Vector2.Distance(transform.position, player.position) <= interactionRange;
}
