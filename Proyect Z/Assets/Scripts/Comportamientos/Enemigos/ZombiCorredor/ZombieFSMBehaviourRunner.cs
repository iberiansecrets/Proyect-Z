using BehaviourAPI.Core;
using BehaviourAPI.Core.Actions;
using BehaviourAPI.Core.Perceptions;
using BehaviourAPI.StateMachines;
using BehaviourAPI.StateMachines.StackFSMs;
using BehaviourAPI.UnityToolkit;
using System;
using UnityEngine;
using UnityEngine.AI;

public class ZombieFSMBehaviourRunner : BehaviourRunner
{
    [SerializeField] private ZombieActions m_ZombieActions;

    [Header("Objetivo de la IA")]
    public Transform target;

    [SerializeField] private bool debugLogs;

    private VisionSensor visionSensor;
    private HearingSensor hearingSensor;

    [SerializeField] private Animator zombiAnim;
    public float rangoAtaque = 1.5f;
    private PlayerHealth jugadorVida;
    private Transform jugadorReal;

    private Rigidbody rb;
    private NavMeshAgent agent;
    private Vector3 lastFixedPosition;
    private float currentRealSpeed;

    [Header("Control de Horda (Stack-FSM)")]
    public bool pushHordeSignal = false; // El Colosal activará esto para meter al zombi en la horda
    public bool popHordeSignal = false;  // El Colosal activará esto para sacarlo de la horda

    protected override void Init()
    {
        if (m_ZombieActions == null) m_ZombieActions = GetComponent<ZombieActions>();
        if (visionSensor == null) visionSensor = GetComponent<VisionSensor>();
        if (hearingSensor == null) hearingSensor = GetComponent<HearingSensor>();

        zombiAnim = GetComponentInChildren<Animator>();

        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        lastFixedPosition = transform.position;

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

        base.Init();
    }

    protected override void OnUpdated()
    {
        if (m_ZombieActions != null && m_ZombieActions.target != target)
        {
            m_ZombieActions.target = target;
        }

        if (target == null)
        {
            target = jugadorReal;
        }

        base.OnUpdated();
    }

    protected override BehaviourGraph CreateGraph()
    {
        // Soporte de pila
        StackFSM ZombieFSM = new StackFSM();

        System.Action ResetAgentAndAttack = () => {
            if (zombiAnim != null) zombiAnim.SetBool("Ataque", false);
            if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
        };

        // Estados
        FunctionalAction Roaming_action = new FunctionalAction();
        Roaming_action.onStarted = () => {
            m_ZombieActions.EnterRoaming();
            ResetAgentAndAttack();
        };
        Roaming_action.onUpdated = m_ZombieActions.TickRoaming;
        State Roaming = ZombieFSM.CreateState(Roaming_action);

        FunctionalAction Chasing_action = new FunctionalAction();
        Chasing_action.onStarted = ResetAgentAndAttack;
        Chasing_action.onUpdated = m_ZombieActions.TickChasing;
        State Chasing = ZombieFSM.CreateState(Chasing_action);

        FunctionalAction InvestigateSound_action = new FunctionalAction();
        InvestigateSound_action.onStarted = () => {
            m_ZombieActions.EnterInvestigateSound();
            ResetAgentAndAttack();
        };
        InvestigateSound_action.onUpdated = m_ZombieActions.TickInvestigateSound;
        State InvestigateSound = ZombieFSM.CreateState(InvestigateSound_action);

        FunctionalAction Attacking_action = new FunctionalAction();
        Attacking_action.onStarted = () => {
            if (zombiAnim != null)
            {
                zombiAnim.SetBool("Movimiento", false);
                zombiAnim.SetBool("Ataque", true);
            }
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
        };
        Attacking_action.onUpdated = TickAttacking;
        State Attacking = ZombieFSM.CreateState(Attacking_action);

        // Estado de horda
        FunctionalAction Horde_action = new FunctionalAction();
        Horde_action.onStarted = () => {
            pushHordeSignal = false; // Consumimos la señal de entrada para evitar bucles en la pila
            ResetAgentAndAttack();
        };
        Horde_action.onUpdated = TickHordeBehavior;
        State Horde = ZombieFSM.CreateState(Horde_action);

        // Transiciones
        ConditionPerception RoamToChase_perception = new ConditionPerception();
        RoamToChase_perception.onCheck = CheckCanSeePlayer;
        ZombieFSM.CreateTransition("RoamToChase", Roaming, Chasing, RoamToChase_perception);

        ConditionPerception SoundToChase_perception = new ConditionPerception();
        SoundToChase_perception.onCheck = CheckCanSeePlayer;
        ZombieFSM.CreateTransition("SoundToChase", InvestigateSound, Chasing, SoundToChase_perception);

        ZombieFSM.CreateTransition(Chasing, Roaming, statusFlags: StatusFlags.Failure);
        ZombieFSM.CreateTransition(InvestigateSound, Roaming, statusFlags: StatusFlags.Success);

        ConditionPerception RoamToInvestigate_perception = new ConditionPerception();
        RoamToInvestigate_perception.onCheck = CheckHasHeardSound;
        ZombieFSM.CreateTransition("RoamToInvestigate", Roaming, InvestigateSound, RoamToInvestigate_perception);

        ConditionPerception InAttackRange_perception = new ConditionPerception();
        InAttackRange_perception.onCheck = CheckPlayerInAttackRange;
        ZombieFSM.CreateTransition("ChaseToAttack", Chasing, Attacking, InAttackRange_perception);

        ConditionPerception OutOfAttackRange_perception = new ConditionPerception();
        OutOfAttackRange_perception.onCheck = () => !CheckPlayerInAttackRange();
        ZombieFSM.CreateTransition("AttackToChase", Attacking, Chasing, OutOfAttackRange_perception);

        // Interrupción de la pila
        ConditionPerception pushHordePerception = new ConditionPerception(() => pushHordeSignal);
        ConditionPerception popHordePerception = new ConditionPerception(() => popHordeSignal);

        // Registramos el Push desde cualquier estado de movimiento para congelar su comportamiento
        ZombieFSM.CreatePushTransition("EntrarEnHorda_Roam", Roaming, Horde, pushHordePerception);
        ZombieFSM.CreatePushTransition("EntrarEnHorda_Chase", Chasing, Horde, pushHordePerception);
        ZombieFSM.CreatePushTransition("EntrarEnHorda_Sound", InvestigateSound, Horde, pushHordePerception);

        // Registramos el Pop: cuando se active, destruye el estado Horda y vuelve al anterior guardado
        ZombieFSM.CreatePopTransition("SalirDeHorda", Horde, popHordePerception);

        ZombieFSM.SetEntryState(Roaming);
        return ZombieFSM;
    }

    private Status TickHordeBehavior()
    {
        popHordeSignal = false; // Aseguramos limpieza de la bandera de salida

        if (zombiAnim != null) zombiAnim.SetBool("Movimiento", true);

        // El Colosal controlará dinámicamente el target de este zombi. 
        // El corredor se moverá hacia donde el líder le mande usando el NavMesh físico.
        if (target != null && agent != null && agent.enabled)
        {
            agent.SetDestination(target.position);
            Vector3 direction = agent.desiredVelocity.normalized;
            direction.y = 0;

            rb.MovePosition(rb.position + direction * (m_ZombieActions != null ? 3.5f : 2f) * Time.deltaTime);

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRotation, 300f * Time.deltaTime));
            }
        }

        // Si han llegado a menos de 4 metros del Colosal, o si el Colosal ya no existe, salen del trance
        if (target == null || Vector3.Distance(transform.position, target.position) < 4f)
        {
            popHordeSignal = true; // Hace Pop de la Stack-FSM y destruye el estado Horda
            target = jugadorReal; // Vuelve a tener al jugador como objetivo
            return Status.Success;
        }

        return Status.Running;
    }

    private void FixedUpdate()
    {
        currentRealSpeed = (transform.position - lastFixedPosition).magnitude / Time.fixedDeltaTime;
        lastFixedPosition = transform.position;

        if (agent != null && !agent.updatePosition)
        {
            agent.nextPosition = rb.position;
        }
    }

    private void LateUpdate()
    {
        if (zombiAnim == null || zombiAnim.GetBool("Ataque")) return;
        zombiAnim.SetBool("Movimiento", currentRealSpeed > 0.1f);
    }

    private Boolean CheckCanSeePlayer()
    {
        if (target != jugadorReal) return true;
        if (visionSensor == null || target == null) return false;
        return visionSensor.CanSeePlayerSimple(target);
    }

    private Boolean CheckHasHeardSound()
    {
        if (target != jugadorReal) return false;
        if (hearingSensor == null) return false;
        return hearingSensor.HasHeardSound();
    }

    private Boolean CheckPlayerInAttackRange()
    {
        if (target == null) return false;
        return Vector3.Distance(transform.position, target.position) <= rangoAtaque;
    }

    private Status TickAttacking()
    {
        if (target != null)
        {
            Vector3 dir = (target.position - transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);

            if (target == jugadorReal && jugadorVida != null && jugadorVida.GetVidaActual() > 0)
            {
                jugadorVida.RecibirDaño(10f * Time.deltaTime);
            }
        }
        return Status.Running;
    }
}