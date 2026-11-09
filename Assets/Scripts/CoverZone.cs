using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CoverZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    { var player = other.GetComponent<PlayerControlerC>(); if (player != null) player.EnterCover(this); }
    private void OnTriggerExit2D(Collider2D other)
    { var player = other.GetComponent<PlayerControlerC>(); if (player != null) player.LeaveCover(this); }
}
