using BehaviourAPI.Core;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(VisionSensor))]
[RequireComponent(typeof(HearingSensor))]
public class ZombieActions : MonoBehaviour
{
    [Header("Refs")]
    public Transform target;

    [Header("Roaming")]
    public float roamSpeed = 2f;
    public float roamMoveTime = 2f;
    public float roamWaitTime = 5f;

    [Header("Chase")]
    public float chaseSpeed = 3f;

    // internal
    private Rigidbody rb;
    private VisionSensor vision;
    private HearingSensor hearing;

    private Vector3 roamDir;
    private float roamTimer;
    private bool isMoving;

    private Vector3 investigateTarget;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        vision = GetComponent<VisionSensor>();
        hearing = GetComponent<HearingSensor>();
    }

    private void Start()
    {
        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }
    }

    // --- Roaming ---
    public void EnterRoaming()
    {
        Vector2 rnd = Random.insideUnitCircle.normalized;
        roamDir = new Vector3(rnd.x, 0f, rnd.y);
        isMoving = true;
        roamTimer = roamMoveTime;
        if (roamDir != Vector3.zero) rb.MoveRotation(Quaternion.LookRotation(roamDir));
        Debug.Log("[ZombieActions] EnterRoaming");
    }

    public Status TickRoaming()
    {
        roamTimer -= Time.deltaTime;
        if (isMoving)
        {
            rb.MovePosition(rb.position + roamDir * roamSpeed * Time.deltaTime);
            if (roamTimer <= 0f) { isMoving = false; roamTimer = roamWaitTime; }
        }
        else
        {
            if (roamTimer <= 0f) EnterRoaming();
        }
        return Status.Running;
    }

    // --- Idle ---
    public void EnterIdle()
    {
        roamTimer = 5f;
        isMoving = false;
        Debug.Log("[ZombieActions] EnterIdle");
    }

    public Status TickIdle()
    {
        roamTimer -= Time.deltaTime;
        return roamTimer <= 0f ? Status.Success : Status.Running; // success -> go back to roaming via transition
    }

    // --- Chasing ---
    public Status TickChasing()
    {
        if (target == null) return Status.Failure;

        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent == null || !agent.isOnNavMesh) return Status.Failure;

        // 1) Actualiza destino cada frame
        agent.isStopped = false;
        agent.SetDestination(target.position);

        // 2) Rotación (elige la que encaje con tu setup)
        // Opción A: deja que NavMesh rote (más simple)
        // agent.updateRotation = true;

        // Opción B: rota manualmente (si usas animaciones o quieres control fino)
        agent.updateRotation = false;
        Vector3 dir = (target.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion look = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 10f); // suaviza la rotación
        }

        // 3) Opcional: ajustar velocidad en chase
        agent.speed = chaseSpeed;

        // 4) Visión: si no ve al jugador, decide comportamiento
        bool sees = false;
        var vision = GetComponent<VisionSensor>();
        if (vision != null)
        {
            sees = vision.CanSeePlayerSimple(target);
        }

        if (!sees)
        {
            // Opción 1 (si quieres que vuelva inmediatamente): terminar y dejar que la transición Chasing->Roaming ocurra
            Debug.Log("[TickChasing] Perdió de vista al jugador -> return Failure");
            return Status.Failure;

            // Opción 2 (más realista): perseguir hasta la última posición vista antes de rendirse
            // agent.SetDestination(lastKnownPlayerPos); // necesitarías guardar lastKnownPlayerPos en EnterChasing
            // return Status.Running;
        }

        // Si llega suficientemente cerca, puedes causar daño (collisions/hits) u otro comportamiento
        return Status.Running;
    }

    // --- Investigate ---
    public void EnterInvestigateSound()
    {
        if (hearing.TryGetLastHeardPosition(out Vector3 pos))
            investigateTarget = pos;
        else investigateTarget = transform.position;

        Debug.Log($"[ZombieActions] EnterInvestigateSound -> {investigateTarget}");
    }

    public Status TickInvestigateSound()
    {
        // prioridad: si ve al player, salir a chasing (devolvemos Success para que la transición CanSee->Chasing la gestione)
        if (target != null && vision.CanSeePlayerSimple(target)) return Status.Failure; // signal to FSM: leave investigate (will go to Chasing via perception)

        Vector3 dir = (investigateTarget - transform.position);
        dir.y = 0f;
        if (dir.magnitude < 0.5f) return Status.Success; // reached -> transition back to idle

        dir.Normalize();
        rb.MovePosition(rb.position + dir * roamSpeed * Time.deltaTime);
        if (dir != Vector3.zero) rb.MoveRotation(Quaternion.LookRotation(dir));
        return Status.Running;
    }
}
