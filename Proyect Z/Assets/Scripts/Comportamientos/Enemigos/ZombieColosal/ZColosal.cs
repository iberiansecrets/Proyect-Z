using UnityEngine;

public class ZColosal : MonoBehaviour
{
    [Header("Rangos del Colosal")]
    public float rangoVision = 15f;
    public float rangoAtaque = 2.5f;
    public float radioLlamadaHorda = 25f;
    public float distanciaMinimaParaGritar = 8f;
    public float radioOlfato = 12f;

    [Header("Atributos de Movimiento")]
    public float speedPatrulla = 2f;
    public float speedPersecucion = 4f;
    public float speedRotation = 250f;

    [Header("Combate")]
    public float damageImpacto = 5f;
    public float damageGolpeMelee = 25f;
    public float cooldownGrito = 10f;

    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public Transform jugador;
    private ZColosalBehaviour zBehaviour;

    [Header("Renderizado de FOV")]
    public Color fovColor = new Color(1, 0.5f, 0, 0.2f); // Naranja translúcido para el jefe

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        zBehaviour = GetComponent<ZColosalBehaviour>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            jugador = playerObj.transform;
        }
        else
        {
            //Debug.LogWarning("[ZCOLOSAL] No se encontró ningún objeto con el tag 'Player'.");
        }
    }

    void OnCollisionStay(Collision collision)
    {
        // Daño por aplastamiento físico directo al chocar el cuerpo contra el jugador
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth saludJugador = collision.gameObject.GetComponent<PlayerHealth>();
            if (saludJugador != null)
            {
                saludJugador.RecibirDaño(damageImpacto * Time.deltaTime);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (jugador == null || rb == null || zBehaviour == null) return;

        float distanciaAlJugador = Vector3.Distance(jugador.position, rb.position);

        // 1. DIBUJO DEL CONO DE VISIÓN (FOV)
        if (distanciaAlJugador > rangoAtaque)
        {
            Gizmos.color = (distanciaAlJugador > rangoVision) ? fovColor : new Color(1, 0, 0, 0.3f);

            float anguloVisionTotal = 120f; // Campo de visión masivo del coloso
            Vector3 forward = transform.forward * rangoVision;

            Vector3 leftDir = Quaternion.Euler(0, -anguloVisionTotal / 2f, 0) * forward;
            Vector3 rightDir = Quaternion.Euler(0, anguloVisionTotal / 2f, 0) * forward;

            Gizmos.DrawLine(transform.position, transform.position + leftDir);
            Gizmos.DrawLine(transform.position, transform.position + rightDir);

            int steps = 12;
            Vector3 prevPoint = transform.position + leftDir;

            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector3 point = transform.position + Quaternion.Euler(0, -anguloVisionTotal / 2f + anguloVisionTotal * t, 0) * forward;
                Gizmos.DrawLine(prevPoint, point);
                Gizmos.DrawLine(transform.position, point);
                prevPoint = point;
            }
        }

        // 2. DIBUJO DEL RADIO DE RECOLECCIÓN DE LA HORDA (GIZMO ADICIONAL)
        Gizmos.color = new Color(1f, 0f, 1f, 0.15f); // Magenta sutil
        Gizmos.DrawWireSphere(transform.position, radioLlamadaHorda);
    }
}