using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class ZNormal : MonoBehaviour
{
    [Header("Referencias")]
    
    public float rangoPersecucion = 10f;
    public float rangoAtaque = 2f;
    public float rangoMovimiento = 10f;
    public float anguloVision = 45f;

    public float speed = 3f;

    public float damage = 10f;

    public StateMachine fsm;
    public Rigidbody rb;
    public Transform jugador;

    //Destino de movimiento aleatorio
    Vector3 destino;

    //Cono de vision
    public Color fovColor = new Color(1, 1, 0, 0.3f);

    void Awake()
    {
        fsm = new StateMachine();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        //Busca al jugador en la escena
        if (jugador == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                jugador = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("No se encontro ningun objeto con tag 'Player'.");
            }
        }

        fsm.Initialize(new BuscarState(this));
    }

    /*void FixedUpdate()
    {
        currentState?.FixedUpdate();
    }*/
    /*void Update()
    {
        fsm.Update();
    }*/

    /*public void ChangeState(IState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }*/

    public void ElegirDestinoAleatorio()
    {
        destino = transform.position + new Vector3(Random.Range(-rangoMovimiento, rangoMovimiento),0,Random.Range(-rangoMovimiento, rangoMovimiento));
    }

    // Calcular posición aleatoria en el plano XZ dentro de un rango
    public void MoverHaciaDestino()
    {
        Vector3 direccion = (destino - rb.position).normalized;

        // Rota a la nueva posicion
        Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion);
        rb.MoveRotation(Quaternion.RotateTowards(
            rb.rotation,
            rotacionObjetivo,
            180f * Time.fixedDeltaTime
        ));


        // Calcula la nueva posicion con deteccion de colisiones
        rb.MovePosition(rb.position + direccion * speed * Time.fixedDeltaTime);

    }

    public void DetenerMovimiento()
    {
        rb.MovePosition(rb.position);
        rb.angularVelocity = Vector3.zero;
    }

    //Si el zombi colisiona con el jugador, le hace daño
    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Enemigo colision con el jugador");
            PlayerHealth saludJugador = collision.gameObject.GetComponent<PlayerHealth>();
            if (saludJugador != null)
            {
                saludJugador.RecibirDaño(damage);
            }
        }
    }

    //Dibujo del cono de visión
    void OnDrawGizmos()
    {
        Vector3 direction = (jugador.position - rb.position).normalized;

        float distanciaAlJugador = Vector3.Distance(jugador.position, rb.position);

        if (distanciaAlJugador > rangoAtaque)
        {

            //Color del triángulo
            if (distanciaAlJugador > rangoPersecucion)
            {
                Gizmos.color = fovColor; // Amarillo opaco
            }
            else
            {
                Gizmos.color = new Color(1, 0, 0, 0.3f); // Opaco
            }

            //Calculamos la dirección izquierda y derecha del FOV
            Vector3 forward = transform.forward * rangoPersecucion;

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
