using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float vidaMaxima = 30f;
    private float vidaActual;
    private bool isDead = false;

    void Start()
    {
        vidaActual = vidaMaxima;
    }

    public void RecibirDaño(float cantidad)
    {
        if (isDead) return;

        vidaActual -= cantidad;
        Debug.Log($"{gameObject.name} recibió {cantidad} de daño. Vida restante: {vidaActual}");

        if (vidaActual <= 0f)
        {
            Morir();
        }
    }

    void Morir()
    {
        isDead = true;

        Debug.Log($"{gameObject.name} ha sido derrotado!");
        if (GameManager.Instance != null)
            GameManager.Instance.DesregistrarEnemigo(gameObject);
        Destroy(gameObject);
    }
}
