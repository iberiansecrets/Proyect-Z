using System;
using UnityEngine;
using BehaviourAPI.Core;
using BehaviourAPI.Core.Actions;
using BehaviourAPI.Core.Perceptions;
using BehaviourAPI.UnityToolkit;
using BehaviourAPI.StateMachines;
using UnityEngine.AI;

public class ZombieFSMBehaviourRunner : BehaviourRunner
{
    [SerializeField] private ZombieActions m_ZombieActions;

    [Header("Objetivo de la IA")]
    public Transform target; // Objetivo dinámico modificable desde el script del señuelo

    [SerializeField] private bool debugLogs;

    private VisionSensor visionSensor;
    private HearingSensor hearingSensor;

    // Variables del animador, el rango de ataque y la vida del jugador
    [SerializeField] private Animator zombiAnim;
    public float rangoAtaque = 1.5f;
    private PlayerHealth jugadorVida;
    private Transform jugadorReal; // Referencia para restaurar el foco tras destruir el señuelo

    // Variables para controlar velocidades y componentes físicos
    private Rigidbody rb;
    private NavMeshAgent agent;
    private Vector3 lastFixedPosition;
    private float currentRealSpeed;

    protected override void Init()
    {
        if (m_ZombieActions == null) m_ZombieActions = GetComponent<ZombieActions>();
        if (visionSensor == null) visionSensor = GetComponent<VisionSensor>();
        if (hearingSensor == null) hearingSensor = GetComponent<HearingSensor>();

        // Engancha el animator del hijo
        zombiAnim = GetComponentInChildren<Animator>();

        // Referencias físicas
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        lastFixedPosition = transform.position;

        // Desacopla el movimiento del NavMesh
        if (agent != null)
        {
            agent.updatePosition = false;
            agent.updateRotation = false;
        }

        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                target = p.transform;
                jugadorReal = p.transform;
                jugadorVida = target.GetComponent<PlayerHealth>();
            }
        }
        else
        {
            jugadorReal = target;
            jugadorVida = jugadorReal.GetComponent<PlayerHealth>();
        }

        if (debugLogs)
        {
            Debug.Log($"[ZombieFSMBehaviourRunner.Init] actions={(m_ZombieActions != null)}, vision={(visionSensor != null)}, hearing={(hearingSensor != null)}, target={(target != null ? target.name : "NULL")}");
        }

        base.Init();
    }

    protected override void OnUpdated()
    {
        // Sincroniza el objetivo dinámico con el script de movimientos físicos de forma continua
        if (m_ZombieActions != null && m_ZombieActions.target != target)
        {
            m_ZombieActions.target = target;
        }

        // Recupera la referencia del jugador si el objetivo actual se destruye
        if (target == null)
        {
            target = jugadorReal;
        }

        base.OnUpdated();
    }

    protected override BehaviourGraph CreateGraph()
    {
        FSM ZombieFSM = new FSM();

        // Se llama cada vez que el zombie sale del estado de ataque para volver a moverse
        System.Action ResetAgentAndAttack = () => {
            if (zombiAnim != null) zombiAnim.SetBool("Ataque", false);
            if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
        };

        FunctionalAction Roaming_action = new FunctionalAction();

        // Animación de patrulla
        Roaming_action.onStarted = () => {
            m_ZombieActions.EnterRoaming();
            ResetAgentAndAttack();
        };
        Roaming_action.onUpdated = m_ZombieActions.TickRoaming;
        State Roaming = ZombieFSM.CreateState(Roaming_action);

        FunctionalAction Chasing_action = new FunctionalAction();

        // Animación para perseguir
        Chasing_action.onStarted = ResetAgentAndAttack;
        Chasing_action.onUpdated = m_ZombieActions.TickChasing;
        State Chasing = ZombieFSM.CreateState(Chasing_action);

        FunctionalAction InvestigateSound_action = new FunctionalAction();

        // Animación al investigar sonido
        InvestigateSound_action.onStarted = () => {
            m_ZombieActions.EnterInvestigateSound();
            ResetAgentAndAttack();
        };
        InvestigateSound_action.onUpdated = m_ZombieActions.TickInvestigateSound;
        State InvestigateSound = ZombieFSM.CreateState(InvestigateSound_action);

        // Estado de ataque
        FunctionalAction Attacking_action = new FunctionalAction();
        Attacking_action.onStarted = () => {
            if (zombiAnim != null)
            {
                zombiAnim.SetBool("Movimiento", false); // Forzamos Idle visual
                zombiAnim.SetBool("Ataque", true);
            }

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true; // Corta la ruta del NavMesh para que no empuje
                agent.velocity = Vector3.zero;
            }
        };
        Attacking_action.onUpdated = TickAttacking;
        State Attacking = ZombieFSM.CreateState(Attacking_action);

        ConditionPerception RoamToChase_perception = new ConditionPerception();
        RoamToChase_perception.onCheck = CheckCanSeePlayer;
        ZombieFSM.CreateTransition("RoamToChase", Roaming, Chasing, RoamToChase_perception);

        ConditionPerception SoundToChase_perception = new ConditionPerception();
        SoundToChase_perception.onCheck = CheckCanSeePlayer;
        ZombieFSM.CreateTransition("SoundToChase", InvestigateSound, Chasing, SoundToChase_perception);

        StateTransition FromChasing = ZombieFSM.CreateTransition(Chasing, Roaming, statusFlags: StatusFlags.Failure);

        StateTransition FromInvestigate = ZombieFSM.CreateTransition(InvestigateSound, Roaming, statusFlags: StatusFlags.Success);

        ConditionPerception RoamToInvestigate_perception = new ConditionPerception();
        RoamToInvestigate_perception.onCheck = CheckHasHeardSound;
        ZombieFSM.CreateTransition("RoamToInvestigate", Roaming, InvestigateSound, RoamToInvestigate_perception);

        // Transiciones para entrar y salir del ataque
        ConditionPerception InAttackRange_perception = new ConditionPerception();
        InAttackRange_perception.onCheck = CheckPlayerInAttackRange;
        ZombieFSM.CreateTransition("ChaseToAttack", Chasing, Attacking, InAttackRange_perception);

        ConditionPerception OutOfAttackRange_perception = new ConditionPerception();
        OutOfAttackRange_perception.onCheck = () => !CheckPlayerInAttackRange();
        ZombieFSM.CreateTransition("AttackToChase", Attacking, Chasing, OutOfAttackRange_perception);

        return ZombieFSM;
    }

    private void FixedUpdate()
    {
        currentRealSpeed = (transform.position - lastFixedPosition).magnitude / Time.fixedDeltaTime;
        lastFixedPosition = transform.position;

        // Arrastra el cerebro del NavMesh junto al cuerpo físico
        if (agent != null && !agent.updatePosition)
        {
            agent.nextPosition = rb.position;
        }
    }

    private void LateUpdate()
    {
        if (zombiAnim == null) return;

        // Si está en medio de un ataque, respeta esa animación
        if (zombiAnim.GetBool("Ataque")) return;

        // Si se mueve a más de 0.1 unidades por segundo, corre. Si se frena, pasa a Idle.
        bool isMoving = currentRealSpeed > 0.1f;
        zombiAnim.SetBool("Movimiento", isMoving);
    }

    private Boolean CheckCanSeePlayer()
    {
        // Fuerza la persecución inmediata si detecta un señuelo activo en la escena
        if (target != jugadorReal)
        {
            return true;
        }

        if (visionSensor == null) visionSensor = GetComponent<VisionSensor>();
        if (visionSensor == null)
        {
            if (debugLogs) Debug.Log("[ZombieRunner] Runner_CanSeePlayer = false (no visionSensor)");
            return false;
        }

        if (target == null)
        {
            if (debugLogs) Debug.Log("[ZombieRunner] Runner_CanSeePlayer = false (no target)");
            return false;
        }

        bool sees = visionSensor.CanSeePlayerSimple(target);
        if (debugLogs) Debug.Log($"[ZombieRunner] Runner_CanSeePlayer = {sees}");
        return sees;
    }

    private Boolean CheckHasHeardSound()
    {
        // Ignora distracciones de sonido menores si ya está persiguiendo un señuelo activo
        if (target != jugadorReal) return false;

        if (hearingSensor == null) hearingSensor = GetComponent<HearingSensor>();
        if (hearingSensor == null)
        {
            if (debugLogs) Debug.Log("[ZombieRunner] Runner_HasHeardSound = false (no hearingSensor)");
            return false;
        }

        bool h = hearingSensor.HasHeardSound();
        if (debugLogs) Debug.Log($"[ZombieRunner] Runner_HasHeardSound = {h}");
        return h;
    }

    // Funciones para el ataque
    private Boolean CheckPlayerInAttackRange()
    {
        if (target == null) return false;
        float distance = Vector3.Distance(transform.position, target.position);
        return distance <= rangoAtaque;
    }

    private Status TickAttacking()
    {
        if (target != null)
        {
            // Mirar al objetivo mientras ataca
            Vector3 dir = (target.position - transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }

            // Aplica daño por segundo al jugador real o interactúa con el señuelo
            if (target == jugadorReal && jugadorVida != null && jugadorVida.GetVidaActual() > 0)
            {
                jugadorVida.RecibirDaño(10f * Time.deltaTime);
            }
        }
        return Status.Running;
    }
}