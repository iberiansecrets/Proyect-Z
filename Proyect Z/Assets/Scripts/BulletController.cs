using UnityEngine;

public class BulletController : MonoBehaviour
{
    public float distanciaMaxima = 10f;
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
        // Aqu� puedes agregar l�gica adicional si quieres da�ar enemigos, etc.
        Destroy(gameObject);
    }
}
