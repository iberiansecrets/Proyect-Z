using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float vidaMaxima = 30f;
    private float vidaActual;

    void Start()
    {
        vidaActual = vidaMaxima;
    }

    public void RecibirDaño(float cantidad)
    {
        vidaActual -= cantidad;
        Debug.Log($"{gameObject.name} recibió {cantidad} de daño. Vida restante: {vidaActual}");

        if (vidaActual <= 0f)
        {
            Morir();
        }
    }

    void Morir()
    {
        Debug.Log($"{gameObject.name} ha sido derrotado!");
        if (GameManager.Instance != null)
            GameManager.Instance.DesregistrarEnemigo(gameObject);
        Destroy(gameObject);
    }
}
