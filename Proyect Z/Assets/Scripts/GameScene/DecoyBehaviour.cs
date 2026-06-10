using UnityEngine;
using System.Collections;

public class DecoyBehaviour : MonoBehaviour
{
    public int radioAtraccion = 15;   // Radio de atracción de enemigos
    public float duracion = 8f;       // Tiempo que dura el señuelo activo

    public AudioClip alarmaSFX;
    private AudioSource audioSource;

    private float debugGizmoTimer = 0f;
    private float debugGizmoDuration = 0.2f;

    private Transform jugadorReal;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null && alarmaSFX != null)
        {
            audioSource.clip = alarmaSFX;
            audioSource.loop = true;
            audioSource.Play();
        }

        // Guardamos la referencia del jugador para devolverle el foco al destruirse
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            jugadorReal = playerObj.transform;
        }

        StartCoroutine(DecoyLife());
    }

    void Update()
    {
        if (debugGizmoTimer > 0)
        {
            debugGizmoTimer -= Time.deltaTime;
        }
    }

    private IEnumerator DecoyLife()
    {
        float timer = 0f;

        while (timer < duracion)
        {
            AtraerZombies();
            timer += 1f;
            yield return new WaitForSeconds(1f);
        }

        RestaurarZombies();
        Destroy(gameObject);
    }

    private void AtraerZombies()
    {
        debugGizmoTimer = debugGizmoDuration;

        Collider[] colliders = Physics.OverlapSphere(transform.position, radioAtraccion);
        foreach (Collider collider in colliders)
        {
            HearingSensor hearingSensor = collider.GetComponent<HearingSensor>() ?? collider.GetComponentInParent<HearingSensor>();
            if (hearingSensor != null)
            {
                hearingSensor.NotifySound(transform.position, 1f);
            }

            SoundSensor soundSensor = collider.GetComponent<SoundSensor>() ?? collider.GetComponentInParent<SoundSensor>();
            if (soundSensor != null)
            {
                soundSensor.NotifySound(transform.position, 1f);
            }

            if (collider.CompareTag("Enemy"))
            {
                // Intenta atraer al Zombie Normal
                ZNormalBehaviour zombiNormal = collider.GetComponent<ZNormalBehaviour>();
                if (zombiNormal != null)
                {
                    zombiNormal.targetActual = transform;
                    continue; // Si ya es este, saltamos al siguiente enemigo
                }

                // Intenta atraer al Zombie Corredor
                ZombieFSMBehaviourRunner zombieCorredor = collider.GetComponent<ZombieFSMBehaviourRunner>();
                if (zombieCorredor != null)
                {
                    // Le cambiamos el target de su FSM hacia este señuelo
                    zombieCorredor.target = transform;
                }
            }
        }
    }

    private void RestaurarZombies()
    {
        if (jugadorReal == null) return;

        Collider[] colliders = Physics.OverlapSphere(transform.position, radioAtraccion);
        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("Enemy"))
            {
                // 1 - Restaurar Zombie Normal
                ZNormalBehaviour zombiNormal = collider.GetComponent<ZNormalBehaviour>();
                if (zombiNormal != null && zombiNormal.targetActual == transform)
                {
                    zombiNormal.targetActual = jugadorReal;
                    continue;
                }

                // 2 - Restaurar Zombie Corredor
                ZombieFSMBehaviourRunner zombieCorredor = collider.GetComponent<ZombieFSMBehaviourRunner>();
                if (zombieCorredor != null && zombieCorredor.target == transform)
                {
                    // Le devolvemos el target al jugador real
                    zombieCorredor.target = jugadorReal;
                }
            }
        }
    }

    // Dibuja el radio de acción en el editor de Unity al seleccionarlo
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radioAtraccion);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.92f, 0.01f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, radioAtraccion);

        if (debugGizmoTimer > 0)
        {
            float alpha = debugGizmoTimer / debugGizmoDuration;

            Gizmos.color = new Color(1f, 0.5f, 0f, alpha * 0.25f);
            Gizmos.DrawSphere(transform.position, radioAtraccion);

            Gizmos.color = new Color(1f, 1f, 0f, alpha);
            Gizmos.DrawWireSphere(transform.position, radioAtraccion);
        }
    }
}