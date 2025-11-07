using UnityEngine;

public class BulletController : MonoBehaviour
{
    public float distanciaMaxima = 10f;
    public float damage = 10f;
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
            EnemyHealth enemigo = collision.gameObject.GetComponent<EnemyHealth>();
            if (enemigo != null)
            {
                // Escalar el daño según la mejora del jugador
                float dañoFinal = damage;

                if (GameManager.Instance != null && GameManager.Instance.playerHealth != null)
                {
                    dañoFinal *= GameManager.Instance.playerHealth.multiplicadorDaño;
                }

                enemigo.RecibirDaño((int)dañoFinal);
            }
        }

        Destroy(gameObject);
    }
}
