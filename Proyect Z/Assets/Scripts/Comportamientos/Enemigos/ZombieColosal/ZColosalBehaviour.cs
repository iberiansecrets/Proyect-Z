using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using BehaviourAPI.Core;
using BehaviourAPI.Core.Actions;
using BehaviourAPI.Core.Perceptions;
using BehaviourAPI.StateMachines;
using BehaviourAPI.BehaviourTrees;
using BehaviourAPI.UtilitySystems;
using BehaviourAPI.UnityToolkit;

public class ZColosalBehaviour : BehaviourRunner
{
    [HideInInspector] public ZColosal zombi;
    private NavMeshAgent agent;
    private Rigidbody rb;
    private Animator colosalAnim;
    private PlayerHealth jugadorVida;

    // Sensores y control
    private float distanciaAlJugador;
    private float ultimoGritoTime = -10f;
    private bool estaGritando = false;
    private OdorPerception percepcionOlor;

    [Header("Objetivo Dinámico (Señuelo)")]
    public Transform targetActual;

    // Variables para la patrulla
    private Vector3 destinoPatrulla;
    private float tiempoEsperaPatrulla = 0f;

    private FSM fsmCombate;
    private BehaviourTree btGrito;

    protected override void Init()
    {
        zombi = GetComponent<ZColosal>();
        rb = GetComponent<Rigidbody>();
        colosalAnim = GetComponentInChildren<Animator>();

        if (zombi.jugador != null)
        {
            jugadorVida = zombi.jugador.GetComponent<PlayerHealth>();
        }

        agent = GetComponent<NavMeshAgent>();
        if (agent == null) agent = gameObject.AddComponent<NavMeshAgent>();

        agent.updatePosition = false;
        agent.updateRotation = false;

        percepcionOlor = new OdorPerception(transform, zombi.radioOlfato);
        destinoPatrulla = transform.position;

        if (targetActual == null) targetActual = zombi.jugador;

        base.Init();
    }

    protected override void OnUpdated()
    {
        // Failsafe por si el señuelo se destruye
        if (targetActual == null && zombi.jugador != null) targetActual = zombi.jugador;

        if (zombi != null && zombi.jugador != null)
        {
            distanciaAlJugador = Vector3.Distance(transform.position, zombi.jugador.position);
        }
        base.OnUpdated();
    }

    private void FixedUpdate()
    {
        if (agent != null && agent.enabled && !agent.updatePosition)
        {
            agent.nextPosition = rb.position;
        }
    }

    // --- ARQUITECTURA DEL SISTEMA DE UTILIDAD COMPUESTO ---
    protected override BehaviourGraph CreateGraph()
    {
        UtilitySystem us = new UtilitySystem(1.3f);

        fsmCombate = CreateFSMCombate();
        btGrito = CreateBTGrito();

        // 1. EVALUADOR GRITO
        VariableFactor fGritoDisponibilidad = us.CreateVariable(() =>
        {
            if (estaGritando) return 1.0f; // Candado lógico irrompible
            if (Time.time < ultimoGritoTime + zombi.cooldownGrito) return 0f;
            if (targetActual != zombi.jugador) return 0f; // No grita si hay señuelo activo

            // Grita fijamente si está en la franja de distancia (Sin cono para evitar bugs de rotación)
            if (distanciaAlJugador >= zombi.distanciaMinimaParaGritar && distanciaAlJugador <= zombi.rangoVision) return 1f;
            return 0f;
        }, 0f, 1f);

        // 2. EVALUADOR OLOR (SOLO SI HAY SEÑUELO)
        VariableFactor fRastroOlor = us.CreateVariable(() =>
        {
            // Verifica que targetActual NO sea el jugador (es decir, han tirado un señuelo) y huele el rastro
            if (targetActual != null && targetActual != zombi.jugador && percepcionOlor != null && percepcionOlor.Check())
            {
                return 0.85f;
            }
            return 0f;
        }, 0f, 1f);

        // 3. EVALUADOR COMBATE (ESTADO POR DEFECTO)
        VariableFactor fCombatePorDefecto = us.CreateVariable(() =>
        {
            if (distanciaAlJugador <= zombi.rangoAtaque) return 0.95f;
            return 0.4f;
        }, 0f, 1f);

        // ACCIONES
        FunctionalAction actionGrito = new FunctionalAction
        {
            onStarted = () => btGrito.Start(),
            onUpdated = () => { btGrito.Update(); return Status.Running; },
            onStopped = () => btGrito.Stop()
        };

        FunctionalAction actionRastreo = new FunctionalAction(TickRastreoOlor);

        FunctionalAction actionCombate = new FunctionalAction
        {
            onStarted = () => fsmCombate.Start(),
            onUpdated = () => { fsmCombate.Update(); return Status.Running; },
            onStopped = () => fsmCombate.Stop()
        };

        us.CreateAction(fGritoDisponibilidad, actionGrito);
        us.CreateAction(fRastroOlor, actionRastreo);
        us.CreateAction(fCombatePorDefecto, actionCombate);

        return us;
    }

    // --- SUB-SISTEMA 1: FSM DE COMBATE ---
    private FSM CreateFSMCombate()
    {
        FSM fsm = new FSM();

        FunctionalAction roam = new FunctionalAction
        {
            onStarted = () => { if (agent != null) agent.speed = zombi.speedPatrulla; ElegirNuevoDestinoPatrulla(); },
            onUpdated = ExecuteRoamingBasico
        };
        State estadoRoam = fsm.CreateState("Roaming", roam);

        FunctionalAction chase = new FunctionalAction
        {
            onStarted = () => { if (agent != null) agent.speed = zombi.speedPersecucion; },
            onUpdated = ExecutePersecucionBasica
        };
        State estadoChase = fsm.CreateState("Chasing", chase);

        FunctionalAction attack = new FunctionalAction
        {
            onStarted = () => { if (agent != null) agent.isStopped = true; },
            onUpdated = ExecuteAtaqueMelee,
            onStopped = () => { if (agent != null) agent.isStopped = false; }
        };
        State estadoAttack = fsm.CreateState("Attacking", attack);

        ConditionPerception veAlJugador = new ConditionPerception(() => CheckJugadorEnCono() && distanciaAlJugador <= zombi.rangoVision);
        ConditionPerception pierdeAlJugador = new ConditionPerception(() => !CheckJugadorEnCono() || distanciaAlJugador > zombi.rangoVision);

        ConditionPerception enRangoMelé = new ConditionPerception(() => distanciaAlJugador <= zombi.rangoAtaque);
        ConditionPerception saleRangoMelé = new ConditionPerception(() => distanciaAlJugador > zombi.rangoAtaque);

        fsm.CreateTransition("VeObjetivo", estadoRoam, estadoChase, veAlJugador);
        fsm.CreateTransition("PierdeObjetivo", estadoChase, estadoRoam, pierdeAlJugador);

        fsm.CreateTransition("EnRangoAtaque", estadoChase, estadoAttack, enRangoMelé);
        fsm.CreateTransition("FueraRangoAtaque", estadoAttack, estadoChase, saleRangoMelé);

        fsm.SetEntryState(estadoRoam);
        return fsm;
    }

    // --- SUB-SISTEMA 2: ÁRBOL DE COMPORTAMIENTO (GRITO) ---
    private BehaviourTree CreateBTGrito()
    {
        BehaviourTree bt = new BehaviourTree();

        FunctionalAction inicioGritoAction = new FunctionalAction(() => {
            estaGritando = true;
            if (agent != null) agent.isStopped = true;
            if (rb != null) rb.linearVelocity = Vector3.zero;
            return Status.Success;
        });

        FunctionalAction animGritoAction = new FunctionalAction(() => {
            if (colosalAnim != null) colosalAnim.SetBool("Ataque", true);
            return Status.Success;
        });

        FunctionalAction alertaHordaAction = new FunctionalAction(() => {
            EmitirGritoLiderDeHorda();
            return Status.Success;
        });

        FunctionalAction esperaGritoAction = new FunctionalAction();
        float tGrito = 0f;
        esperaGritoAction.onStarted = () => tGrito = 0f;
        esperaGritoAction.onUpdated = () => {
            tGrito += Time.deltaTime;
            return (tGrito >= 2.5f) ? Status.Success : Status.Running;
        };
        esperaGritoAction.onStopped = () => {
            estaGritando = false;
            ultimoGritoTime = Time.time;
            if (colosalAnim != null) colosalAnim.SetBool("Ataque", false);
            if (agent != null) agent.isStopped = false;
        };

        LeafNode nInicio = bt.CreateLeafNode(inicioGritoAction);
        LeafNode nAnim = bt.CreateLeafNode(animGritoAction);
        LeafNode nAlerta = bt.CreateLeafNode(alertaHordaAction);
        LeafNode nEspera = bt.CreateLeafNode(esperaGritoAction);

        SequencerNode secuenciaGrito = bt.CreateComposite<SequencerNode>(false, nInicio, nAnim, nAlerta, nEspera);
        bt.SetRootNode(secuenciaGrito);

        return bt;
    }

    private void EmitirGritoLiderDeHorda()
    {
        Debug.Log("<color=red><b>[COLOSAL]</b> ¡RUGIDO INVOCADO! Reclutando a la horda...</color>");

        Collider[] cercanos = Physics.OverlapSphere(transform.position, zombi.radioLlamadaHorda);
        foreach (var col in cercanos)
        {
            // IMPORTANTE: Ahora los zombis van a proteger al COLOSAL (transform), no al jugador
            var runner = col.GetComponent<ZombieFSMBehaviourRunner>();
            if (runner != null) { runner.target = transform; runner.pushHordeSignal = true; }

            var normal = col.GetComponent<ZNormalBehaviour>();
            if (normal != null) { normal.targetActual = transform; normal.pushHordeSignal = true; }
        }
    }

    // --- SEGUIMIENTO DE OLOR ---
    private Status TickRastreoOlor()
    {
        if (colosalAnim != null) colosalAnim.SetBool("Movimiento", true);
        if (agent != null) agent.isStopped = false;

        var puntosRastro = PlayerOdorTrail.Instance.rastroPosiciones;
        if (puntosRastro.Count == 0) return Status.Failure;

        // Persigue siempre el punto más NUEVO (el último que dejó el jugador)
        Vector3 puntoObjetivo = puntosRastro[puntosRastro.Count - 1];

        agent.SetDestination(puntoObjetivo);
        MoverFisicamenteHaciaCamino(zombi.speedPersecucion);

        return Status.Running;
    }

    // --- PATRULLA REAL ---
    private Status ExecuteRoamingBasico()
    {
        if (Vector3.Distance(transform.position, destinoPatrulla) < 1f)
        {
            if (colosalAnim != null) colosalAnim.SetBool("Movimiento", false);

            tiempoEsperaPatrulla -= Time.deltaTime;
            if (tiempoEsperaPatrulla <= 0f) ElegirNuevoDestinoPatrulla();
        }
        else
        {
            if (colosalAnim != null) colosalAnim.SetBool("Movimiento", true);
            agent.SetDestination(destinoPatrulla);
            MoverFisicamenteHaciaCamino(zombi.speedPatrulla);
        }

        return Status.Running;
    }

    private void ElegirNuevoDestinoPatrulla()
    {
        Vector3 puntoAleatorio = transform.position + new Vector3(UnityEngine.Random.Range(-10f, 10f), 0, UnityEngine.Random.Range(-10f, 10f));
        NavMeshHit hit;
        if (NavMesh.SamplePosition(puntoAleatorio, out hit, 10f, NavMesh.AllAreas)) destinoPatrulla = hit.position;
        else destinoPatrulla = transform.position;

        tiempoEsperaPatrulla = UnityEngine.Random.Range(2f, 4f);
    }

    private Status ExecutePersecucionBasica()
    {
        if (colosalAnim != null) colosalAnim.SetBool("Movimiento", true);

        if (targetActual != null && agent != null)
        {
            agent.SetDestination(targetActual.position);
            MoverFisicamenteHaciaCamino(zombi.speedPersecucion);
        }
        return Status.Running;
    }

    private Status ExecuteAtaqueMelee()
    {
        if (colosalAnim != null) { colosalAnim.SetBool("Movimiento", false); colosalAnim.SetBool("Ataque", true); }

        if (zombi.jugador != null)
        {
            Vector3 dir = (zombi.jugador.position - rb.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero) rb.MoveRotation(Quaternion.LookRotation(dir));

            if (jugadorVida != null && jugadorVida.GetVidaActual() > 0 && distanciaAlJugador <= zombi.rangoAtaque)
            {
                jugadorVida.RecibirDaño(zombi.damageGolpeMelee * Time.deltaTime);
            }
        }
        return Status.Running;
    }

    private void MoverFisicamenteHaciaCamino(float velocidadActual)
    {
        if (agent.pathPending) return;

        Vector3 direccionCamino = (agent.steeringTarget - rb.position).normalized;
        direccionCamino.y = 0;

        if (direccionCamino.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direccionCamino);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRot, zombi.speedRotation * Time.deltaTime));
            rb.MovePosition(rb.position + direccionCamino * velocidadActual * Time.deltaTime);
        }

        agent.nextPosition = transform.position;
    }

    private bool CheckJugadorEnCono()
    {
        if (zombi.jugador == null) return false;
        Vector3 direccionAlJugador = (zombi.jugador.position - transform.position).normalized;
        float angulo = Vector3.Angle(transform.forward, direccionAlJugador);
        return angulo <= 90f;
    }

    private void OnDisable()
    {
        if (colosalAnim != null) colosalAnim.SetBool("Ataque", false);

        var runners = FindObjectsByType<ZombieFSMBehaviourRunner>(FindObjectsSortMode.None);
        foreach (var r in runners) r.popHordeSignal = true;

        var normales = FindObjectsByType<ZNormalBehaviour>(FindObjectsSortMode.None);
        foreach (var n in normales) n.popHordeSignal = true;
    }
}