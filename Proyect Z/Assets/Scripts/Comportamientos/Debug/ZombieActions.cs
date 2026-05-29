using BehaviourAPI.Core;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(VisionSensor))]
[RequireComponent(typeof(HearingSensor))]
[RequireComponent(typeof(NavMeshAgent))]
public class ZombieActions : MonoBehaviour
{
    [Header("Refs")]
    public Transform target;

    [Header("Roaming")]
    public float roamSpeed = 2f;              // velocidad (m/s) durante roaming
    public float roamWaitDuration = 3f;       // espera antes de moverse
    public float roamMoveDuration = 3f;       // tiempo moviéndose en la dirección elegida
    public float roamMoveDistance = 6f;       // distancia objetivo (se calcula como preferencia), si =0, se usa roamSpeed*roamMoveDuration

    [Header("Chase")]
    public float chaseSpeed = 3f;
    public float chaseLoseMaxTime = 3f;       // tiempo que persigue lastKnown si pierde visión

    [Header("Investigate")]
    public float investigateSearchMaxRadius = 6f;
    public int investigateSearchSteps = 12;
    public float investigateArriveThreshold = 0.6f;

    // internals
    private Rigidbody rb;
    private VisionSensor vision;
    private HearingSensor hearing;
    private NavMeshAgent agent;

    // Roaming state:
    private enum RoamPhase { Waiting, Moving }
    private RoamPhase roamPhase = RoamPhase.Waiting;
    private float roamTimer = 0f;
    private Vector3 roamTarget = Vector3.zero;

    // Investigate
    private Vector3 investigateTarget = Vector3.zero;      // posición original del sonido (centro de emisión)
    private Vector3 investigateNavTarget = Vector3.zero;   // punto sobre NavMesh alcanzable
    private float currentInvestigateTimestamp = -Mathf.Infinity; // timestamp de la emisión que estamos investigando

    // Chasing
    private Vector3 lastKnownPlayerPos = Vector3.zero;
    private float chaseLoseTimer = 0f;

    // thresholds
    private float arriveThreshold = 0.5f;
    private float destUpdateThreshold = 0.5f;

    // Ruta de navegación hacia el origen del sonido (breadcrumb)
    private List<Vector3> investigateNavPath = new List<Vector3>();
    private int investigatePathIndex = 0;

    // Parámetros para la construcción de la cadena
    private float pathSampleStep = 1.0f;      // distancia entre muestras a lo largo de la línea agente->origen
    private int maxPathSamples = 40;          // límite de muestras a lo largo de la línea
    private float sampleRadius = 0.8f;        // radio para NavMesh.SamplePosition
    private float finalProximityThreshold = 1.0f; // distancia al origen que consideraremos “alcanzado”

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        vision = GetComponent<VisionSensor>();
        hearing = GetComponent<HearingSensor>();
        agent = GetComponent<NavMeshAgent>();

        // sensible defaults
        if (agent != null)
        {
            // Desactivamos la actualización automática del NavMesh para que las físicas manden
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.isStopped = true; // parado hasta que se necesite
            agent.speed = Mathf.Max(0.1f, chaseSpeed);
            agent.stoppingDistance = Mathf.Max(0.4f, arriveThreshold);
        }

        // Aseguramos que el zombi siga usando físicas para poder ser empujado
        if (rb != null) rb.isKinematic = false;

        // default roamMoveDistance if zero
        if (roamMoveDistance <= 0f)
            roamMoveDistance = roamSpeed * roamMoveDuration;
    }

    private void Start()
    {
        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }
    }

    // Se encarga de mover el Rigidbody hacia el objetivo que marca el NavMesh
    private void MoveTowardsNavTarget(float speed)
    {
        if (agent != null && agent.isOnNavMesh && !agent.pathPending)
        {
            // Usamos la velocidad deseada del agente (que respeta las curvas del NavMesh) en lugar de ir en línea recta hacia el steeringTarget.
            Vector3 dir = agent.desiredVelocity.normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                rb.MovePosition(rb.position + dir * speed * Time.deltaTime);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime));
            }
        }
    }
    // ------------------------------------------------------------------

    // -------------------- ROAMING --------------------

    public void EnterRoaming()
    {
        // Make sure the agent will move the transform (we use NavMeshAgent for roaming here)
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;     // start in waiting phase
            agent.ResetPath();
            agent.speed = roamSpeed;
        }

        roamPhase = RoamPhase.Waiting;
        roamTimer = roamWaitDuration;
        roamTarget = transform.position; // no target yet
        Debug.Log("[ZombieActions] EnterRoaming: waiting " + roamWaitDuration + "s");
    }

    public Status TickRoaming()
    {
        // Interruptions: vision OR sound -> return Success so transition can fire.
        // IMPORTANT: in the editor ensure Roaming->Chasing transition is above Roaming->Investigate so vision has priority.
        if (vision != null && target != null && vision.CanSeePlayerSimple(target))
        {
            // set last known for chasing
            lastKnownPlayerPos = target.position;
            chaseLoseTimer = chaseLoseMaxTime;
            Debug.Log("[ZombieActions] TickRoaming: saw player -> interrupt Roaming");
            return Status.Success; // signal to FSM: go to Chasing (transition priority in editor)
        }

        if (hearing != null && hearing.HasHeardSound())
        {
            // store center of sound as investigateTarget (the sensor gives the last heard position / center)
            if (hearing.TryGetLastHeardPosition(out Vector3 pos))
                investigateTarget = pos;
            else if (target != null)
                investigateTarget = target.position; // fallback
            Debug.Log($"[ZombieActions] TickRoaming: heard sound at {investigateTarget} -> interrupt Roaming");
            return Status.Success; // signal to FSM: go to Investigate (editor must prioritize Chasing over Investigate)
        }

        // normal roaming behavior
        switch (roamPhase)
        {
            case RoamPhase.Waiting:
                roamTimer -= Time.deltaTime;
                if (roamTimer <= 0f)
                {
                    // choose a direction and target on NavMesh
                    ChooseRoamTarget();
                    if (roamTarget != transform.position && agent != null && agent.isOnNavMesh)
                    {
                        agent.isStopped = false;
                        agent.speed = roamSpeed;
                        agent.SetDestination(roamTarget);
                        roamPhase = RoamPhase.Moving;
                        roamTimer = roamMoveDuration;
                        Debug.Log("[ZombieActions] Roaming: moving to " + roamTarget + " for " + roamMoveDuration + "s");
                    }
                    else
                    {
                        // no valid roam target -> wait again
                        roamPhase = RoamPhase.Waiting;
                        roamTimer = roamWaitDuration;
                        Debug.Log("[ZombieActions] Roaming: no valid roam target found, waiting again");
                    }
                }
                break;

            case RoamPhase.Moving:
                // countdown movement time
                roamTimer -= Time.deltaTime;

                // Usamos la función física en lugar de dejar que el NavMesh mueva al zombi
                MoveTowardsNavTarget(roamSpeed);

                // If reached target earlier, we can stop early
                if (agent != null && agent.isOnNavMesh && !agent.pathPending)
                {
                    if (agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, arriveThreshold))
                    {
                        // arrived
                        agent.isStopped = true;
                        agent.ResetPath();
                        roamPhase = RoamPhase.Waiting;
                        roamTimer = roamWaitDuration;
                        Debug.Log("[ZombieActions] Roaming: arrived early, switching to Waiting");
                        break;
                    }
                }

                if (roamTimer <= 0f)
                {
                    // stop movement and wait
                    if (agent != null && agent.isOnNavMesh)
                    {
                        agent.isStopped = true;
                        agent.ResetPath();
                    }
                    roamPhase = RoamPhase.Waiting;
                    roamTimer = roamWaitDuration;
                    Debug.Log("[ZombieActions] Roaming: finished move duration, switching to Waiting");
                }
                break;
        }

        return Status.Running;
    }

    private void ChooseRoamTarget()
    {
        // pick random direction, then sample a navmesh position at distance roamMoveDistance
        for (int attempt = 0; attempt < 12; attempt++)
        {
            Vector2 rnd = Random.insideUnitCircle.normalized;
            Vector3 dir = new Vector3(rnd.x, 0f, rnd.y);
            Vector3 desired = transform.position + dir * roamMoveDistance;

            // Se cambió el '1.0f' original por 'roamMoveDistance' para evitar que la búsqueda falle cerca de paredes
            if (NavMesh.SamplePosition(desired, out NavMeshHit hit, roamMoveDistance, NavMesh.AllAreas))
            {
                // check reachability via path
                if (IsPathCompleteTo(hit.position, out float pathLen))
                {
                    // avoid trivially short path (i.e., target sampled near our position)
                    if (pathLen > 0.25f)
                    {
                        roamTarget = hit.position;
                        return;
                    }
                }
            }
        }

        // fallback: no valid target found -> stay put
        roamTarget = transform.position;
    }

    // -------------------- CHASING --------------------

    public void EnterChasing()
    {
        if (target != null)
        {
            // Aseguramos que la última posición conocida esté dentro del NavMesh azul
            if (NavMesh.SamplePosition(target.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                lastKnownPlayerPos = hit.position;
            else
                lastKnownPlayerPos = target.position;

            chaseLoseTimer = chaseLoseMaxTime;
        }
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
        Debug.Log("[ZombieActions] EnterChasing lastKnown = " + lastKnownPlayerPos);
    }

    public Status TickChasing()
    {
        if (target == null || agent == null || !agent.isOnNavMesh)
            return Status.Failure;

        // If we see player -> update lastKnown and move directly
        bool sees = vision != null && vision.CanSeePlayerSimple(target);
        if (sees)
        {
            // Aseguramos que la última posición conocida esté dentro del NavMesh azul
            if (NavMesh.SamplePosition(target.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                lastKnownPlayerPos = hit.position;
            else
                lastKnownPlayerPos = target.position;

            chaseLoseTimer = chaseLoseMaxTime;

            if (Vector3.Distance(agent.destination, target.position) > destUpdateThreshold)
            {
                agent.SetDestination(target.position);
                Debug.Log("[TickChasing] SetDestination -> " + target.position);
            }
            agent.speed = chaseSpeed;

            // Movemos físicamente
            MoveTowardsNavTarget(chaseSpeed);

            return Status.Running;
        }

        // Lost sight: go to lastKnown for a bit
        if (chaseLoseTimer > 0f)
        {
            chaseLoseTimer -= Time.deltaTime;

            if (Vector3.Distance(agent.destination, lastKnownPlayerPos) > destUpdateThreshold)
            {
                agent.SetDestination(lastKnownPlayerPos);
                Debug.Log("[TickChasing] Lost sight -> moving to lastKnown " + lastKnownPlayerPos);
            }

            // Movemos físicamente
            MoveTowardsNavTarget(chaseSpeed);

            if (!agent.pathPending && agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, arriveThreshold))
            {
                // arrived to last known and didn't see player -> stop chasing
                agent.isStopped = true;
                agent.ResetPath();
                Debug.Log("[TickChasing] Arrived lastKnown and didn't see player -> Failure");
                return Status.Failure;
            }

            if (chaseLoseTimer <= 0f)
            {
                agent.isStopped = true;
                agent.ResetPath();
                Debug.Log("[TickChasing] chase timeout expired -> Failure");
                return Status.Failure;
            }

            return Status.Running;
        }

        // no knowledge, give up
        return Status.Failure;
    }

    // -------------------- INVESTIGATE (sound) --------------------

    private void StartInvestigateForSound(Vector3 pos, float time)
    {
        // si es la misma emision o mas antigua, ignorar
        if (time <= currentInvestigateTimestamp) return;

        currentInvestigateTimestamp = time;
        investigateTarget = pos;
        investigateNavPath.Clear();
        investigatePathIndex = 0;

        Debug.Log($"[ZombieActions] StartInvestigateForSound: new sound at {pos} (t={time})");

        // Preferimos ruta directa al player si player fue emisor y accesible (opcional)
        if (target != null && Vector3.Distance(target.position, pos) < 0.5f)
        {
            // si coincide con player, intentamos su posición en NavMesh
            if (NavMesh.SamplePosition(target.position, out NavMeshHit phit, 1.0f, NavMesh.AllAreas)
                && agent != null && agent.CalculatePath(phit.position, new NavMeshPath()))
            {
                investigateNavPath.Add(phit.position);
                investigateNavTarget = phit.position;
                StartInvestigateMovement();
                return;
            }
        }

        // Construir la ruta progresiva
        if (BuildInvestigateNavPathTowardsOrigin(investigateTarget))
        {
            investigatePathIndex = 0;
            investigateNavTarget = investigateNavPath[0];
            StartInvestigateMovement();
            return;
        }

        // Fallback sample cercano al origen
        if (NavMesh.SamplePosition(investigateTarget, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
        {
            if (agent != null && IsPathCompleteTo(hit.position, out float _))
            {
                investigateNavTarget = hit.position;
                StartInvestigateMovement();
                return;
            }
        }

        // no encontrado -> abortar, dejar fallback rb movement
        investigateNavTarget = Vector3.zero;
        if (agent != null) { agent.isStopped = true; agent.ResetPath(); }
        
    }

    public void EnterInvestigateSound()
    {
        if (hearing == null)
        {
            Debug.Log("[EnterInvestigateSound] no hearing sensor");
            return;
        }

        if (hearing.TryGetLastHeardInfo(out Vector3 pos, out float time))
        {
            StartInvestigateForSound(pos, time);
        }
        else
        {
            Debug.Log("[EnterInvestigateSound] no recent sound info");
        }
    }


    // Helper: enable agent and set destination to current investigateNavTarget
    private void StartInvestigateMovement()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            // Eliminado isKinematic y updatePosition de aquí
            agent.isStopped = false;
            agent.SetDestination(investigateNavTarget);
        }
    }

    public Status TickInvestigateSound()
    {
        // Prioridad: si vemos al jugador, salir a Chasing
        if (vision != null && target != null && vision.CanSeePlayerSimple(target))
        {
            Debug.Log("[TickInvestigateSound] saw player -> leave investigate");
            if (agent != null && agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath(); }
            return Status.Failure; // let FSM transition to Chasing
        }

        // Priorizar nuevos sonidos: si hay uno mas nuevo que currentInvestigateTimestamp, cambiar
        if (hearing != null && hearing.TryGetLastHeardInfo(out Vector3 newPos, out float newTime))
        {
            if (newTime > currentInvestigateTimestamp)
            {
                Debug.Log($"[TickInvestigateSound] Detected NEWER sound (t={newTime}) replacing current (t={currentInvestigateTimestamp})");
                StartInvestigateForSound(newPos, newTime);
                // continuar; la StartInvestigateForSound ya ha llamado StartInvestigateMovement
            }
        }

        // Si tenemos una ruta de breadcrumbs
        if (investigateNavPath != null && investigateNavPath.Count > 0)
        {
            // Movemos físicamente
            MoveTowardsNavTarget(roamSpeed);

            // Si alcanzamos el nodo actual, avanzamos al siguiente
            if (agent != null && agent.isOnNavMesh && !agent.pathPending)
            {
                if (agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, 0.35f))
                {
                    // avanzamos
                    investigatePathIndex++;
                    if (investigatePathIndex >= investigateNavPath.Count)
                    {
                        // ya hemos llegado al último nodo navegable; ahora comprobamos proximidad al origen real
                        float distToOrigin = Vector3.Distance(transform.position, investigateTarget);
                        if (distToOrigin <= finalProximityThreshold)
                        {
                            // exito: estamos cerca del origen
                            agent.isStopped = true;
                            agent.ResetPath();
                            Debug.Log("[TickInvestigateSound] reached final proximity to origin -> Success");
                            return Status.Success;
                        }
                        else
                        {
                            // Intentamos un ultimo muestreo directo en torno al origen (posiblemente dentro de la habitación)
                            if (NavMesh.SamplePosition(investigateTarget, out NavMeshHit finalHit, 1.5f, NavMesh.AllAreas)
                                && IsPathCompleteTo(finalHit.position, out float _))
                            {
                                // dirigirnos al punto final muestreado
                                agent.SetDestination(finalHit.position);
                                investigateNavTarget = finalHit.position;
                                Debug.Log("[TickInvestigateSound] trying final sampled nav point near origin");
                                return Status.Running;
                            }
                            else
                            {
                                // no hay forma de acercarse más al origen; abortamos investigation para no quedarse pegado
                                agent.isStopped = true;
                                agent.ResetPath();
                                Debug.Log("[TickInvestigateSound] cannot reach closer to origin -> Failure");
                                return Status.Failure;
                            }
                        }
                    }
                    else
                    {
                        // ponemos el siguiente nodo como destino
                        investigateNavTarget = investigateNavPath[investigatePathIndex];
                        agent.SetDestination(investigateNavTarget);
                        Debug.Log($"[TickInvestigateSound] advancing to path node {investigatePathIndex} -> {investigateNavTarget}");
                        return Status.Running;
                    }
                }
            }

            // Si aun no ha llegado al nodo actual, esperar
            return Status.Running;
        }

        // Si no hay ruta navegable creada: fallback RB move hacia el origin (esto se mantendra si no hay NavMesh path)
        Vector3 dir = investigateTarget - transform.position;
        dir.y = 0f;
        if (dir.magnitude < finalProximityThreshold) return Status.Success;
        dir.Normalize();
        rb.MovePosition(rb.position + dir * roamSpeed * Time.deltaTime);
        if (dir != Vector3.zero) rb.MoveRotation(Quaternion.LookRotation(dir));
        return Status.Running;
    }

    // -------------------- NAV UTILS --------------------

    private bool BuildInvestigateNavPathTowardsOrigin(Vector3 origin)
    {
        investigateNavPath.Clear();
        if (agent == null || !agent.isOnNavMesh) return false;

        Vector3 start = transform.position;
        Vector3 dir = origin - start;
        float dist = dir.magnitude;
        if (dist < 0.01f) return false;
        Vector3 dirNorm = dir / dist;

        // Muestreamos pasos a lo largo de la línea desde el agente hacia el origen,
        // tratando de recoger puntos en NavMesh que sean alcanzables y progresen hacia el origen.
        Vector3 lastAdded = start;
        for (int step = 1; step <= maxPathSamples; step++)
        {
            float t = step * (pathSampleStep / Mathf.Max(dist, pathSampleStep));
            if (t > 1f) t = 1f;
            Vector3 samplePoint = start + dirNorm * (dist * t);

            // sample en NavMesh cerca del punto
            if (NavMesh.SamplePosition(samplePoint, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
            {
                // comprobar que hay un path completo desde nuestra posición al hit.position
                if (IsPathCompleteTo(hit.position, out float pathLen) && pathLen > 0.25f)
                {
                    // añadir sólo si representará progreso efectivo hacia el origin (evitar puntos repetidos)
                    if (investigateNavPath.Count == 0
                        || Vector3.Distance(hit.position, origin) < Vector3.Distance(lastAdded, origin) - 0.05f)
                    {
                        investigateNavPath.Add(hit.position);
                        lastAdded = hit.position;
                    }
                }
            }

            if (t >= 1f) break;
        }

        // Como extra: intentar samplear alrededor del origin para tener un posible último punto
        if (NavMesh.SamplePosition(origin, out NavMeshHit originHit, sampleRadius * 1.5f, NavMesh.AllAreas))
        {
            if (IsPathCompleteTo(originHit.position, out float len) && len > 0.25f)
            {
                // añadir sólo si mejora (más cercano al origin)
                if (investigateNavPath.Count == 0 || Vector3.Distance(originHit.position, origin) < Vector3.Distance(lastAdded, origin))
                {
                    investigateNavPath.Add(originHit.position);
                }
            }
        }

        // remove duplicates (por seguridad)
        for (int i = investigateNavPath.Count - 1; i > 0; i--)
        {
            if (Vector3.Distance(investigateNavPath[i], investigateNavPath[i - 1]) < 0.25f)
                investigateNavPath.RemoveAt(i);
        }

        return investigateNavPath.Count > 0;
    }


    private bool IsPathCompleteTo(Vector3 navPoint, out float pathLen)
    {
        pathLen = 0f;
        if (agent == null || !agent.isOnNavMesh) return false;

        NavMeshPath path = new NavMeshPath();
        bool ok = agent.CalculatePath(navPoint, path);
        if (!ok) return false;
        if (path.status != NavMeshPathStatus.PathComplete) return false;

        if (path.corners.Length >= 2)
        {
            for (int i = 1; i < path.corners.Length; i++)
                pathLen += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }
        if (pathLen < 0.2f) return false;
        return true;
    }

    private bool IsPathCompleteTo(Vector3 navPoint)
    {
        return IsPathCompleteTo(navPoint, out _);
    }
}