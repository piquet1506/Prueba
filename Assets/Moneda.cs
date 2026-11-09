using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Moneda : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (!GetComponent<Collider2D>().enabled) return;
            GetComponent<Collider2D>().enabled = false;
            GameManagerUI ui = FindFirstObjectByType<GameManagerUI>();
            if (ui != null) ui.AddCoin();
            Destroy(gameObject);
        }
    }
}
