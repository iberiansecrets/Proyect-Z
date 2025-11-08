using UnityEngine;

public class BulletPushController : MonoBehaviour
{
    public float distanciaMaxima = 10f;
    public float damage = 0f;
    public float pushForce = 500f; // Fuerza base de empuje
    private Vector3 puntoInicial;

    void Start()
    {
        puntoInicial = transform.position;
    }

    void Update()
    {
        float distanciaRecorrida = Vector3.Distance(puntoInicial, transform.position);
        if (distanciaRecorrida >= distanciaMaxima)
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            PlayerHealth player = GameManager.Instance?.playerHealth;

            // Aplicar daño solo si el empuje hace daño
            if (player != null && player.dañoEmpuje > 0)
            {
                EnemyHealth enemigo = collision.gameObject.GetComponent<EnemyHealth>();
                if (enemigo != null)
                {
                    float dañoFinal = player.dañoEmpuje * player.multiplicadorDaño;
                    enemigo.RecibirDaño((int)dañoFinal);
                }
            }

            // Calcular empuje con el multiplicador del jugador
            Rigidbody enemyRb = collision.gameObject.GetComponent<Rigidbody>();
            if (enemyRb != null && !enemyRb.isKinematic)
            {
                Vector3 pushDirection = (collision.transform.position - transform.position).normalized;
                pushDirection.y = 0f;

                float fuerzaFinal = pushForce;
                if (player != null)
                    fuerzaFinal *= player.multiplicadorEmpuje;

                enemyRb.AddForce(pushDirection * fuerzaFinal, ForceMode.Impulse);

                Debug.Log($"Empuje aplicado: {pushDirection * fuerzaFinal}");
            }
        }

        Destroy(gameObject);
    }
}
