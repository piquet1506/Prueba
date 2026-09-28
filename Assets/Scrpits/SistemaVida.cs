using UnityEngine;

public class SistemaVida : MonoBehaviour
{
    public int vidaMaxima = 100;
    private int vidaActual;
    private Animator anim;

    void Start()
    {
        vidaActual = vidaMaxima;
        anim = GetComponent<Animator>();
    }

    public void RecibirDano(int cantidad)
    {
        vidaActual -= cantidad;

        // Dispara la animación de recibir daño si la tienes
        if (anim != null) anim.SetTrigger("Hit");

        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    void Morir()
    {
        // Aquí puedes reproducir animación de muerte o destruir el objeto
        Destroy(gameObject);
    }
}