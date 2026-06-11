using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Adaptador simple que expone la API mínima que PatrolAction espera:
/// - SetTarget(Vector3)
/// - CancelMove()
/// - HasArrived()
/// 
/// Usa NavMeshAgent internamente. Añadir este componente al prefab del enemigo
/// junto con un NavMeshAgent y un NavMesh baked elimina el NullReferenceException
/// dentro de PatrolAction.Update().
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class Movement : MonoBehaviour
{
    [Header("Ajustes")]
    public NavMeshAgent agent;
    [Tooltip("Distancia mínima para considerar que ha llegado.")]
    public float arrivalThreshold = 0.25f;

    void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("[Movement] No NavMeshAgent encontrado en el GameObject.", this);
        }
    }

    /// <summary>
    /// Manda al agente a la posición objetivo.
    /// </summary>
    public void SetTarget(Vector3 worldPosition)
    {
        if (agent == null) return;
        agent.isStopped = false;
        agent.SetDestination(worldPosition);
    }

    /// <summary>
    /// Cancela el movimiento (reset path).
    /// </summary>
    public void CancelMove()
    {
        if (agent == null) return;
        agent.ResetPath();
        agent.isStopped = true;
    }

    /// <summary>
    /// True si el agente ha llegado al destino.
    /// </summary>
    public bool HasArrived()
    {
        if (agent == null) return true;

        // Si aún está calculando camino, no ha llegado.
        if (agent.pathPending) return false;

        // Si no tiene camino o remainingDistance está dentro del umbral
        if (!agent.hasPath) return true;

        if (agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, arrivalThreshold))
            return true;

        return false;
    }
}
