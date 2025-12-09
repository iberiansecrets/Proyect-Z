using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Atributos de salud")]
    public float vidaMaxima = 30f;
    private float vidaActual;
    private bool isDead = false;

    // Evento para notificar la muerte (utilizado por el EnemiesSpawner)
    public delegate void DeathEvent();
    public event DeathEvent onDeath;

    void Start()
    {
        vidaActual = vidaMaxima;
    }

    public void RecibirDaño(int cantidad)
    {
        if (isDead) return; // Evita aplicar daño una vez muerto

        GetComponent<ParticleSystem>().Play();
        vidaActual -= cantidad;
        Debug.Log($"{gameObject.name} recibió {cantidad} de daño. Vida restante: {vidaActual}");

        if (vidaActual <= 0f)
        {
            Morir();
        }
    }

    private void Morir()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"{gameObject.name} ha sido derrotado!");

        // Notificar al GameManager que el enemigo ha sido eliminado
        if (GameManager.Instance != null)
            GameManager.Instance.DesregistrarEnemigo(gameObject);

        // Notificar al spawner que este zombie ha muerto
        onDeath?.Invoke();

        // Destruir el objeto
        Destroy(gameObject);
    }
}
