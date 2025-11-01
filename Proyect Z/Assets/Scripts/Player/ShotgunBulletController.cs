using UnityEngine;

public class ShotgunBulletController : MonoBehaviour
{
    public float distanciaMaxima = 6f; // Menor rango que pistola
    public float damage = 6f;         // Menos daño x proyectil
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
                enemigo.RecibirDaño(damage);
            }
        }
        Destroy(gameObject);
    }
}
