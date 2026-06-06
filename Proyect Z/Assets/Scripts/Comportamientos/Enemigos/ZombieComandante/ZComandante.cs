using UnityEngine;

public class ZComandante : MonoBehaviour
{
    private Rigidbody rb;
    public Transform jugador;

    private ZComandanteBehaviour zBehaviour;
    private float anguloVision = 180f;


    //Daño por choque
    public float damage = 2f;

    //Cono de vision
    public Color fovColor = new Color(1, 1, 0, 0.3f);

    void Awake()
    {
        jugador = GameObject.FindGameObjectWithTag("Player").transform;
        rb = GetComponent<Rigidbody>();
        // Congela la rotación en los ejes X y Z para evitar que el enemigo se vuelque
        //rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            //Debug.Log("Enemigo colision con el jugador");
            PlayerHealth saludJugador = collision.gameObject.GetComponent<PlayerHealth>();
            if (saludJugador != null)
            {
                saludJugador.RecibirDaño(damage);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDrawGizmos()
    {
        Vector3 direction = (jugador.position - rb.position).normalized;

        float distanciaAlJugador = Vector3.Distance(jugador.position, rb.position);

        if (distanciaAlJugador > zBehaviour.distanciaPanico)
        {

            //Color del triángulo
            if (distanciaAlJugador > zBehaviour.distanciaSegura)
            {
                Gizmos.color = fovColor; // Amarillo opaco
            }
            else
            {
                Gizmos.color = new Color(1, 0, 0, 0.3f); // Opaco
            }

            //Calculamos la dirección izquierda y derecha del FOV
            Vector3 forward = transform.forward * zBehaviour.distanciaSegura;

            Vector3 leftDir = Quaternion.Euler(0, -anguloVision / 2f, 0) * forward;
            Vector3 rightDir = Quaternion.Euler(0, anguloVision / 2f, 0) * forward;

            //Dibujamos las líneas del cono
            Gizmos.DrawLine(transform.position, transform.position + leftDir);
            Gizmos.DrawLine(transform.position, transform.position + rightDir);

            //Relleno del triángulo usando DrawLine
            int steps = 10; // más pasos = más suave
            Vector3 prevPoint = transform.position + leftDir;

            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector3 point = transform.position + Quaternion.Euler(0, -anguloVision / 2f + anguloVision * t, 0) * forward;
                Gizmos.DrawLine(prevPoint, point);
                Gizmos.DrawLine(transform.position, point);
                prevPoint = point;
            }
        }
    }
}
