using Unity.VisualScripting;
using System.Collections;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    public Transform target;           // Objetivo actual (jugador o señuelo)
    public float speed;
    public float damage;
    public float ratioAtaque;

    private Rigidbody rb;
    private Transform player;          // Referencia permanente al jugador
    private Transform decoyTarget;     // Referencia al señuelo actual (si hay)
    private bool followingDecoy = false;

    private bool isStunned = false;
    private float originalSpeed;

    //Parámetros de Animación Enemigos
    public float velocidad;
    [SerializeField] public Animator zombiAnim;
    private float distancia;

    private bool isAttacking = false; // bandera interna


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        originalSpeed = speed;

        if (rb != null)
        {
            // Congela la rotación en los ejes X y Z para evitar que el enemigo se vuelque
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

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

        velocidad = rb.linearVelocity.magnitude; //Saber la velocidad de movimiento
        distancia = Vector3.Distance(transform.position, target.position); //Saber la distancia al objetivo

        // Dirección hacia el objetivo actual
        Vector3 direction = target.position - transform.position;
        direction.y = 0f; // Mantener movimiento en el plano horizontal
        direction.Normalize();

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

        if (velocidad > 0.1f && distancia >= ratioAtaque)
        {
            zombiAnim.SetBool("Movimiento", true);
            zombiAnim.SetBool("Ataque", false);
        }
        if (distancia < ratioAtaque)
        {
            newPosition = rb.position; // No se mueve
            StartCoroutine(AttackRoutine());

        }
        else
        {
            zombiAnim.SetBool("Movimiento", true);
            zombiAnim.SetBool("Ataque", false);
            newPosition = rb.position + direction * speed * Time.fixedDeltaTime;
        }
    }

    IEnumerator AttackRoutine()
    {

        zombiAnim.SetBool("Movimiento", false);
        zombiAnim.SetBool("Ataque", true);

        AnimatorStateInfo stateInfo = zombiAnim.GetCurrentAnimatorStateInfo(0);

        // Duración normalizada: 1.0 equivale a todo el clip
        float clipLength = stateInfo.length;
        yield return new WaitForSeconds(stateInfo.length);
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

    public void Stun(float duration)
    {
        if (!isStunned)
            StartCoroutine(StunCoroutine(duration));
    }

    private IEnumerator StunCoroutine(float duration)
    {
        isStunned = true;

        // Desactivar velocidad
        speed = 0f;

        // Optional: congelar movimiento físico
        rb.linearVelocity = Vector3.zero;

        yield return new WaitForSeconds(duration);

        // Restablecer estado
        isStunned = false;
        speed = originalSpeed;
    }
}