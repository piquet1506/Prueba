using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControl : MonoBehaviour
{
    public float velocidad = 5f;
    public float fuerzaSalto = 10f;
    public float longitudRaycast = 0.1f;

    public LayerMask capaSuelo;
    public LayerMask capaEnemigos; // Capa para detectar a quién golpeas

    // Variables para los ataques (iniciando con minúscula)
    public Transform puntoDisparo;
    public GameObject proyectilPrefab;
    public Transform puntoAtaqueCuerpo;
    public Vector2 tamañoAtaque = new Vector2(1f, 1f);

    private bool enSuelo;
    private Rigidbody2D rb;
    public Animator animator;

    void Start()
    {
        // Obtener el componente del objeto actual
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // MOVIMIENTO Y SALTO
        float inputX = Input.GetAxis("Horizontal"); // Lee la dirección

        // Es mejor definir directamente la velocidad con Rigidbody2D.velocity en lugar de transform.position para evitar conflictos con el salto
        rb.velocity = new Vector2(inputX * velocidad, rb.velocity.y);

        animator.SetFloat("movement", Mathf.Abs(inputX * velocidad));

        // Voltear el personaje
        if (inputX < 0) transform.localScale = new Vector3(-1, 1, 1);
        if (inputX > 0) transform.localScale = new Vector3(1, 1, 1);

        // Raycast para detectar el suelo
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, longitudRaycast, capaSuelo);
        enSuelo = hit.collider != null;
        animator.SetBool("ensuelo", enSuelo);

        // Se lee solo una vez cuando la tecla se ha presionado
        if (enSuelo && Input.GetKeyDown(KeyCode.Space))
        {
            // Aplica una fuerza de empuje (impulse)
            rb.AddForce(new Vector2(0f, fuerzaSalto), ForceMode2D.Impulse);
        }

        // ATAQUE A DISTANCIA (Tecla X)
        if (Input.GetKeyDown(KeyCode.X))
        {
            AtaqueDistancia();
        }

        // ATAQUE CUERPO A CUERPO (Tecla Z)
        if (Input.GetKeyDown(KeyCode.Z))
        {
            AtaqueCuerpoACuerpo();
        }
    }

    void AtaqueDistancia()
    {
        animator.SetTrigger("ataqueDistancia");

        // Crea el objeto proyectil en la posición y rotación especificadas[cite: 1]
        GameObject proyectil = Instantiate(proyectilPrefab, puntoDisparo.position, puntoDisparo.rotation);

        // Lógica para que sea estrictamente Horizontal o Vertical (sin diagonales)
        Vector2 direccionDisparo = Vector2.right * transform.localScale.x; // Horizontal por defecto

        float inputVertical = Input.GetAxis("Vertical"); // Lee la dirección vertical[cite: 1]
        if (Mathf.Abs(inputVertical) > 0.1f)
        {
            // Si el jugador presiona arriba o abajo, anulamos el movimiento horizontal
            direccionDisparo = new Vector2(0, Mathf.Sign(inputVertical));
        }

        /* 
        Aquí deberás enviarle la "direccionDisparo" al script de tu proyectil.
        Ejemplo: proyectil.GetComponent<ScriptProyectil>().AsignarDireccion(direccionDisparo);
        */
    }

    void AtaqueCuerpoACuerpo()
    {
        animator.SetTrigger("ataqueCerca");

        // Detecta colisiones de manera inmediata "creando" una forma temporal en esa posición[cite: 1]
        Collider2D enemigoGolpeado = Physics2D.OverlapBox(puntoAtaqueCuerpo.position, tamañoAtaque, 0f, capaEnemigos);

        if (enemigoGolpeado != null)
        {
            Debug.Log("Enemigo golpeado: " + enemigoGolpeado.name);
            // enemigoGolpeado.GetComponent<EnemigoScript>().RecibirDaño();
        }
    }

    // Dibuja guías en el editor de Unity para visualizar los ataques
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * longitudRaycast);

        if (puntoAtaqueCuerpo != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(puntoAtaqueCuerpo.position, tamañoAtaque);
        }
    }
}