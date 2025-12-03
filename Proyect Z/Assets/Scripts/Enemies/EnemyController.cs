using System.Collections;
using UnityEditor.Experimental.GraphView;
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
    private bool isStunned = false;
    private float originalSpeed;

    //Parámetros de Animación Enemigos
    public float velocidad;
    [SerializeField] public Animator zombiAnim;
    
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

    void Update()
    {
        if (isStunned)
        {
            // En animaciones mostramos idle
            velocidad = 0;
            zombiAnim.SetBool("Movimiento", false);
            zombiAnim.SetBool("Ataque", false);
            zombiAnim.ResetTrigger("AtacandoTrigger");
            zombiAnim.SetTrigger("Idle");
            return;
        }

        velocidad = rb.linearVelocity.magnitude; //Saber la velocidad del enemigo
        if (target == null) return;

        // Dirección hacia el objetivo actual
        //Vector3 direction = (target.position - transform.position).normalized;
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

        if ( velocidad > 0.3f)
        {
            zombiAnim.SetBool("Movimiento", true);
            zombiAnim.ResetTrigger("Idle");
            zombiAnim.ResetTrigger("AtacandoTrigger");
        }
        else 
        {
            zombiAnim.SetBool("Movimiento", false);
            
            if(!zombiAnim.GetBool("Ataque"))
                zombiAnim.SetTrigger("Idle");
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

                zombiAnim.SetBool("Ataque", true);
                zombiAnim.SetTrigger("AtacandoTrigger");
                zombiAnim.ResetTrigger("Idle");
                zombiAnim.SetBool("Movimiento", false);
        }
            else
            {
                zombiAnim.SetBool("Ataque", false);
                zombiAnim.ResetTrigger("AtacandoTrigger");
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
