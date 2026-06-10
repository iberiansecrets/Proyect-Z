using UnityEngine;
using System.Collections;

public class DecoyBehaviour : MonoBehaviour
{
    public int radioAtraccion = 15;
    public float duracion = 8f;
    public SoundType tipoSonidoDecoy; // Añade esta variable para seleccionar el sonido "Decoy" en el inspector

    public AudioClip alarmaSFX;
    private AudioSource audioSource;
    private SoundEmitter soundEmitter; // Referencia al emisor de sonido

    private float debugGizmoTimer = 0f;
    private float debugGizmoDuration = 0.2f;

    private Transform jugadorReal;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        soundEmitter = GetComponent<SoundEmitter>(); // Pillamos el emisor

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
        if (debugGizmoTimer > 0) debugGizmoTimer -= Time.deltaTime;
    }

    private IEnumerator DecoyLife()
    {
        float timer = 0f;
        while (timer < duracion)
        {
            AtraerZombies();
            EmitirOndaSonora();
            timer += 1f;
            yield return new WaitForSeconds(1f); // Pulso de sonido cada segundo
        }
        RestaurarZombies();
        Destroy(soundEmitter);
        Destroy(gameObject);
    }

    private void EmitirOndaSonora()
    {
        debugGizmoTimer = debugGizmoDuration;

        // Llama a los corredores por el sonido del Decoy
        if (soundEmitter != null)
        {
            soundEmitter.EmitSound(tipoSonidoDecoy);
        }
        else
        {
            // Fallback por si acaso no tiene el componente SoundEmitter:
            Collider[] colliders = Physics.OverlapSphere(transform.position, radioAtraccion);
            foreach (Collider collider in colliders)
            {
                HearingSensor hearing = collider.GetComponent<HearingSensor>() ?? collider.GetComponentInParent<HearingSensor>();
                if (hearing != null)
                {
                    hearing.NotifySound(transform.position, 1f);
                }
            }
        }
    }

    private void AtraerZombies()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, radioAtraccion);
        foreach(Collider collider in colliders)
        {
            if (collider.CompareTag("Enemy"))
            {
                // Atrae a los zombies normales
                ZNormalBehaviour normal = collider.GetComponent<ZNormalBehaviour>();
                if (normal != null)
                {
                    normal.targetActual = transform;
                    continue;
                }

                // Atrae a los zombies corredores
                ZombieFSMBehaviourRunner corredor = collider.GetComponent<ZombieFSMBehaviourRunner>();
                if (corredor != null)
                {
                    corredor.target = transform;
                }
            }            
        }
    }

    private void RestaurarZombies()
    {
        if (jugadorReal != null)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, radioAtraccion);
            foreach(Collider collider in colliders)
            {
                if (collider.CompareTag("Enemy"))
                {
                    // Restaurar zombies normales
                    ZNormalBehaviour normal = collider.GetComponent<ZNormalBehaviour>();
                    if(normal != null && normal.targetActual == transform)
                    {
                        normal.targetActual = jugadorReal;
                        continue;
                    }

                    // Restaurar zombies corredores
                    ZombieFSMBehaviourRunner corredor = collider.GetComponent<ZombieFSMBehaviourRunner>();
                    if(corredor != null && corredor.target == transform)
                    {
                        corredor.target = jugadorReal;
                    }
                }
            }
        }
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