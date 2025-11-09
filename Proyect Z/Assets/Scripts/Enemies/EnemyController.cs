using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public Transform target;           // Objetivo actual (jugador o señuelo)
    public float speed = 3f;
    public float damage = 10f;

    private Rigidbody rb;
    private Transform player;          // Referencia permanente al jugador
    private Transform decoyTarget;     // Referencia al señuelo actual (si hay)
    private bool followingDecoy = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Busca al jugador al iniciar
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            target = player;
        }
        else
        {
            Debug.LogWarning("No se encontró ningún objeto con tag 'Player'.");
        }
    }

    void FixedUpdate()
    {
        if (target == null) return;

        // Dirección hacia el objetivo actual
        Vector3 direction = (target.position - transform.position).normalized;

        // Movimiento con física
        Vector3 newPosition = rb.position + direction * speed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);

        // Rotación para mirar al objetivo
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(targetRotation);
        }

        // Si el señuelo ha sido destruido, volver al jugador
        if (followingDecoy && decoyTarget == null)
        {
            ResetTarget();
        }
    }

    void OnCollisionStay(Collision collision)
    {
        // Solo puede dañar al jugador si no sigue un señuelo
        if (collision.gameObject.CompareTag("Player") && !followingDecoy)
        {
            PlayerHealth saludJugador = collision.gameObject.GetComponent<PlayerHealth>();
            if (saludJugador != null)
            {
                saludJugador.RecibirDaño(damage);
            }
        }
    }

    // Llamado por el señuelo cuando aparece
    public void SetDecoyTarget(Transform decoy)
    {
        decoyTarget = decoy;
        target = decoy;
        followingDecoy = true;
    }

    // Vuelve al jugador como objetivo
    public void ResetTarget()
    {
        followingDecoy = false;
        decoyTarget = null;
        target = player;
    }
}
