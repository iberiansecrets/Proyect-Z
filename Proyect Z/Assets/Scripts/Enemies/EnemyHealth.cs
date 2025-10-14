using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float vidaMaxima = 30f;
    private float vidaActual;

    void Start()
    {
        vidaActual = vidaMaxima;
    }

    public void RecibirDa�o(float cantidad)
    {
        vidaActual -= cantidad;
        Debug.Log($"{gameObject.name} recibi� {cantidad} de da�o. Vida restante: {vidaActual}");

        if (vidaActual <= 0f)
        {
            Morir();
        }
    }

    void Morir()
    {
        // Aqu� puedes a�adir efectos de muerte, part�culas, sonido, etc.
        Debug.Log($"{gameObject.name} ha sido derrotado!");
        Destroy(gameObject);
    }
}
