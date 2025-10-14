using UnityEngine;

public class BulletController : MonoBehaviour
{
    public float distanciaMaxima = 10f;
    public float damage = 10f;
    private Vector3 puntoInicial;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        puntoInicial = transform.position;
    }

    // Update is called once per frame
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
        //Si impacta con un enemigo
        if (collision.gameObject.CompareTag("Enemy"))
        {
            EnemyHealth enemigo = collision.gameObject.GetComponent<EnemyHealth>();
            if (enemigo != null)
            {
                enemigo.RecibirDaño(damage);
            }
        }

        //Destruye la bala en cualquier caso al impactar
        Destroy(gameObject);
    }
}
