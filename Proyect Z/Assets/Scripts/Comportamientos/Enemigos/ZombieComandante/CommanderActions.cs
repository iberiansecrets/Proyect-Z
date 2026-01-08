using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements.Experimental;

[RequireComponent(typeof(NavMeshAgent))]
public class CommanderActions : MonoBehaviour
{
    public Transform target;
    private NavMeshAgent agent;
    private EnemyHealth health;
    private VisionSensor vision;
    private SoundSensor soundSensor;

    public float visionRange = 20f;
    public float velocidadGiroAlerta = 100f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<EnemyHealth>();
        vision = GetComponent<VisionSensor>();
        soundSensor = GetComponent<SoundSensor>();
    }

    private void Start()
    {
        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }
    }

    public bool EscuchaSonido() => soundSensor != null && soundSensor.HasHeardSound();

    public bool VeAlJugador() => vision != null && target != null && vision.CanSeePlayerSimple(target);

    public float GetVidaNorm()
    {
        // Lectura del atributo interno de salud normalizado
        return Mathf.Clamp01(health.vidaActual / (float)health.vidaMaxima);
    }

    public float GetInvVidaNorm() => 1 - GetVidaNorm();

    public float GetDistNorm()
    {
        if (target == null) return 1f;
        float dist = Vector3.Distance(transform.position, target.position);
        return Mathf.Clamp01(dist / visionRange); // Normalización según rango de visión
    }

    public float GetInvDistNorm() => 1 - GetDistNorm();

    public bool JugadorEstaLejos()
    {
        // Consideramos que está lejos si la distancia normalizada es alta (ej. > 0.7)
        // o si la distancia lineal supera un umbral de seguridad.
        return GetDistNorm() > 0.8f;
    }

    public void EnterAlerta()
    {
        agent.isStopped = true; // Se detiene por completo
        Debug.Log("Comandante: Algo suena... inspeccionando.");
    }

    public void TickAlerta()
    {
        // El zombie gira sobre su propio eje Y para buscar al jugador
        transform.Rotate(Vector3.up, velocidadGiroAlerta * Time.deltaTime);
    }

    // Para la transición de "No hay peligro"
    public bool NoDetectoNada()
    {
        // Es true si no escucha nada Y no ve al jugador
        return !EscuchaSonido() && !VeAlJugador();
    }

    public void TickHuir()
    {
        // 1. Verificaciones críticas
        if (target == null || agent == null  || !agent.isOnNavMesh) return;

        // 2. Desbloqueo del agente (por si viene de Alerta)
        agent.isStopped = false;
        agent.speed = 20f; // Velocidad de huida

        // 3. Cálculo de la dirección opuesta
        Vector3 direccionContraria = (transform.position - target.position).normalized;
        Vector3 puntoDestino = transform.position + direccionContraria * 30f;

        // 4. Muestreo del NavMesh con radio amplio para evitar errores de colisión
        if (NavMesh.SamplePosition(puntoDestino, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }

        // DEBUG: Verás una línea roja en la ventana 'Scene' indicando a dónde quiere ir
        Debug.DrawLine(transform.position, agent.destination, Color.red);
        Debug.Log("Huida completada");
    }

    public void Summon()
    {
        EnemiesSpawner es = Object.FindFirstObjectByType<EnemiesSpawner>();
        if (es != null) es.SpawnBalanceado(3);
    }

    public void Cure()
    {
        EnemyHealth eh = GetComponent<EnemyHealth>();

        eh.vidaActual = eh.vidaActual + 10;
    }

    public void Fortify()
    {
        EnemiesSpawner es = GetComponent<EnemiesSpawner>();

        for (int i = 0; i < es.zombiesSpawned.Count; i++)
        {
            EnemyController ec = es.zombiesSpawned[i].GetComponent<EnemyController>();

            ec.damage += 5;
        }
    }

    public void Accelerate()
    {
        EnemiesSpawner es = GetComponent<EnemiesSpawner>();

        for (int i = 0; i < es.zombiesSpawned.Count; i++)
        {
            EnemyController ec = es.zombiesSpawned[i].GetComponent<EnemyController>();

            ec.speed += 3;
        }
    }
}
