using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public Transform target;
    public float speed = 3f;
    public float damage = 10f;
    
    void Start()
    {
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
