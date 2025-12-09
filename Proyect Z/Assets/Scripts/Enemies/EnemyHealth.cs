using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Atributos de salud")]
    public float vidaMaxima = 30f;
    private float vidaActual;
    private bool isDead = false;

    [Header("Cooldown daño de alambre")]
    public float cooldownDañoAlambre = 1f;
    private float tiempoUltimoDañoAlambre = -999f;

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

    public void RecibirDañoAlambre(int cantidad)
    {
        if (isDead) return;

        if (Time.time - tiempoUltimoDañoAlambre < cooldownDañoAlambre)
            return;

        tiempoUltimoDañoAlambre = Time.time;

        vidaActual -= cantidad;
        Debug.Log($"{gameObject.name} recibió {cantidad} de daño por alambre. Vida restante: {vidaActual}");

        if (vidaActual <= 0)
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
