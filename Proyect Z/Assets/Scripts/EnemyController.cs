using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public Transform target;
    public float speed = 3f;
    public float damage = 10f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame 
    void Update()
    {
        if (target == null) return;

        Vector3 direction = (target.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;
        transform.forward = direction;
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
