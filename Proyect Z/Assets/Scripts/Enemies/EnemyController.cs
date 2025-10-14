using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public Transform target;
    public float speed = 3f;
    public float damage = 10f;

    private Rigidbody rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();

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
                Debug.LogWarning("No se encontró ningún objeto con tag 'Player'.");
            }
        }
    }
    
    void FixedUpdate()
    {
        if (target == null) return;

        // Dirección hacia el jugador
        Vector3 direction = (target.position - transform.position).normalized;

        // Calcula la nueva posición con detección de colisiones
        Vector3 newPosition = rb.position + direction * speed * Time.fixedDeltaTime;

        // Mueve el Rigidbody usando físicas
        rb.MovePosition(newPosition);

        // Rota para mirar al jugador
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
            Debug.Log("Enemigo colisionó con el jugador");
            PlayerHealth saludJugador = collision.gameObject.GetComponent<PlayerHealth>();
            if (saludJugador != null)
            {
                saludJugador.RecibirDaño(damage);
            }
        }
    }
}
