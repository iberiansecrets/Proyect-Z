using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Roaming")]
    public float roamSpeed = 2f;
    public float roamMoveTime = 2f;
    public float roamWaitTime = 5f;

    private Vector3 roamDirection;
    private float roamTimer;
    private bool isRoamingMoving;

    public Transform target;
    public float speed = 3f;
    public float damage = 10f;

    private Rigidbody rb;
    private SightSensor sightSensor;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        sightSensor = GetComponent<SightSensor>();

        //Busca al jugador en la escena
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                target = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("No se encontro ningun objeto con tag 'Player'.");
            }
        }
        StartRoaming();
    }

    void FixedUpdate()
    {
        UpdateRoaming();

        if (target == null) return;
        /*
        // Direccion hacia el jugador
        Vector3 direction = (target.position - transform.position).normalized;

        // Calcula la nueva posicion con deteccion de colisiones
        Vector3 newPosition = rb.position + direction * speed * Time.fixedDeltaTime;

        // Mueve el Rigidbody usando fisicas
        rb.MovePosition(newPosition);

        // Rota para mirar al jugador
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(targetRotation);
        }*/

        if (!sightSensor.CanSeeTarget(target))
            return;

        MoveTowardsTarget();
    }

    void UpdateRoaming()
    {
        roamTimer -= Time.deltaTime;

        if (isRoamingMoving)
        {
            // Moverse
            rb.MovePosition(rb.position + roamDirection * roamSpeed * Time.deltaTime);

            if (roamTimer <= 0f)
            {
                // Pasar a esperar
                isRoamingMoving = false;
                roamTimer = roamWaitTime;
            }
        }
        else
        {
            // Esperando
            if (roamTimer <= 0f)
            {
                StartRoaming();
            }
        }
    }


    void StartRoaming()
    {
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        roamDirection = new Vector3(randomDir.x, 0f, randomDir.y);

        isRoamingMoving = true;
        roamTimer = roamMoveTime;

        // Rotar hacia la dirección
        if (roamDirection != Vector3.zero)
        {
            Quaternion rot = Quaternion.LookRotation(roamDirection);
            rb.MoveRotation(rot);
        }
    }


    private void MoveTowardsTarget()
    {
        Vector3 direction = (target.position - transform.position).normalized;

        Vector3 newPosition = rb.position + direction * speed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(targetRotation);
        }
    }

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
}
