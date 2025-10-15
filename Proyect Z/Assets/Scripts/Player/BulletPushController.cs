using UnityEngine;

public class BulletPushController : MonoBehaviour
{
    public float distanciaMaxima = 10f;
    public float damage = 10f;
    public float pushForce = 15f; // fuerza del retroceso
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
            // Daño
            EnemyHealth enemigo = collision.gameObject.GetComponent<EnemyHealth>();
            if (enemigo != null)
            {
                enemigo.RecibirDaño(damage);
            }

            // Rigidbody del enemigo
            Rigidbody enemyRb = collision.gameObject.GetComponent<Rigidbody>();
            if (enemyRb != null && !enemyRb.isKinematic)
            {
                //  Dirección desde el enemigo hacia la bala (opuesta al impacto)
                Vector3 pushDirection = (collision.transform.position - transform.position).normalized;
                pushDirection.y = 0f;

                enemyRb.AddForce(pushDirection * pushForce, ForceMode.Impulse);

                Debug.Log($"Empuje aplicado: {pushDirection * pushForce}");
            }
        }

        Destroy(gameObject);
    }
}
