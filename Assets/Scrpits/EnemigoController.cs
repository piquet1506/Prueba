using UnityEngine;

public class EnemigoController : MonoBehaviour
{
    public enum TipoEnemigo { CuerpoACuerpo, ADistancia }

    [Header("Configuración General")]
    public TipoEnemigo tipoDeEnemigo;
    public float velocidadMovimiento = 2f;
    public float distanciaDeteccion = 8f;
    public float distanciaAtaque = 1.5f;

    [Header("Patrulla (NUEVO)")]
    public float velocidadPatrulla = 1f;
    public Transform controladorSueloPared; // Punto para lanzar los rayos
    public float distanciaRayoSuelo = 1f;
    public float distanciaRayoPared = 0.2f;
    public LayerMask capaSuelo; // Para saber qué es piso/pared

    [Header("Ataque")]
    public float tiempoEntreAtaques = 2f;
    public int danoAtaque = 20;
    public LayerMask capaJugador;
    private float proximoAtaque = 0f;

    [Header("Cuerpo a Cuerpo")]
    public Transform puntoAtaque;
    public float radioAtaque = 0.5f;

    [Header("A Distancia")]
    public GameObject prefabProyectil;
    public Transform puntoDisparo;

    private Transform jugador;
    private Rigidbody2D rb;
    private Animator anim;
    private bool mirandoDerecha = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        GameObject objJugador = GameObject.FindGameObjectWithTag("Player");
        if (objJugador != null) jugador = objJugador.transform;
    }

    void Update()
    {
        if (jugador == null) return;

        float distanciaAlJugador = Vector2.Distance(transform.position, jugador.position);

        if (distanciaAlJugador < distanciaDeteccion)
        {
            MirarAlJugador();

            if (distanciaAlJugador > distanciaAtaque)
            {
                MoverHaciaJugador();
            }
            else
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
                ComprobarAtaque();
            }
        }
        else
        {
            // Si el jugador no está cerca, activamos la patrulla
            Patrullar();
        }
    }

    void Patrullar()
    {
        if (anim != null) anim.SetBool("isRunning", true);

        // 1. Moverse en la dirección actual a velocidad de patrulla
        float direccion = mirandoDerecha ? 1 : -1;
        rb.velocity = new Vector2(direccion * velocidadPatrulla, rb.velocity.y);

        // 2. Lanzar rayo hacia ABAJO para detectar precipicios
        RaycastHit2D infoSuelo = Physics2D.Raycast(controladorSueloPared.position, Vector2.down, distanciaRayoSuelo, capaSuelo);

        // 3. Lanzar rayo hacia ADELANTE para detectar paredes
        Vector2 direccionMuro = mirandoDerecha ? Vector2.right : Vector2.left;
        RaycastHit2D infoPared = Physics2D.Raycast(controladorSueloPared.position, direccionMuro, distanciaRayoPared, capaSuelo);

        // Si no detecta suelo (es un borde) o si detecta una pared, da la vuelta
        if (infoSuelo.collider == null || infoPared.collider != null)
        {
            Girar();
        }
    }

    void MoverHaciaJugador()
    {
        if (anim != null) anim.SetBool("isRunning", true);
        float direccion = jugador.position.x > transform.position.x ? 1 : -1;
        rb.velocity = new Vector2(direccion * velocidadMovimiento, rb.velocity.y);
    }

    void MirarAlJugador()
    {
        if (jugador.position.x > transform.position.x && !mirandoDerecha) Girar();
        else if (jugador.position.x < transform.position.x && mirandoDerecha) Girar();
    }

    void Girar()
    {
        mirandoDerecha = !mirandoDerecha;
        Vector3 escala = transform.localScale;
        escala.x *= -1;
        transform.localScale = escala;
    }

    void ComprobarAtaque()
    {
        if (anim != null) anim.SetBool("isRunning", false);

        if (Time.time >= proximoAtaque)
        {
            if (anim != null) anim.SetTrigger("Attack");

            if (tipoDeEnemigo == TipoEnemigo.CuerpoACuerpo) EjecutarAtaqueCuerpoACuerpo();
            else if (tipoDeEnemigo == TipoEnemigo.ADistancia) EjecutarAtaqueDistancia();

            proximoAtaque = Time.time + tiempoEntreAtaques;
        }
    }

    public void EjecutarAtaqueCuerpoACuerpo()
    {
        Collider2D jugadorDañado = Physics2D.OverlapCircle(puntoAtaque.position, radioAtaque, capaJugador);
        if (jugadorDañado != null)
        {
            SistemaVida vida = jugadorDañado.GetComponent<SistemaVida>();
            if (vida != null) vida.RecibirDano(danoAtaque);
        }
    }

    public void EjecutarAtaqueDistancia()
    {
        if (prefabProyectil != null && puntoDisparo != null)
        {
            Instantiate(prefabProyectil, puntoDisparo.position, puntoDisparo.rotation);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Dibuja los rangos de detección
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaDeteccion);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanciaAtaque);

        if (puntoAtaque != null && tipoDeEnemigo == TipoEnemigo.CuerpoACuerpo)
        {
            Gizmos.DrawWireSphere(puntoAtaque.position, radioAtaque);
        }

        // Dibuja los rayos de patrulla para que puedas ajustarlos visualmente
        if (controladorSueloPared != null)
        {
            Gizmos.color = Color.blue;
            // Rayo del suelo (precipicios)
            Gizmos.DrawLine(controladorSueloPared.position, controladorSueloPared.position + Vector3.down * distanciaRayoSuelo);
            // Rayo de pared
            Vector3 direccionMuro = transform.localScale.x > 0 ? Vector3.right : Vector3.left;
            Gizmos.DrawLine(controladorSueloPared.position, controladorSueloPared.position + direccionMuro * distanciaRayoPared);
        }
    }
}