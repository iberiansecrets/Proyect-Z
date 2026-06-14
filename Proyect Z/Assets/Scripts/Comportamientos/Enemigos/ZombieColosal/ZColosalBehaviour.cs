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
using BehaviourAPI.UnityToolkit.GUIDesigner.Runtime;

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

    [Header("Objetivo Dinámico")]
    public Transform targetActual;
    private DecoyBehaviour senueloDetectado;

    private Vector3 destinoPatrulla;
    private float tiempoEsperaPatrulla = 0f;
    
    private int indiceMigaActual = 0;
    private bool haTerminadoRastro = false;
    private DecoyBehaviour ultimoSenuelo;

    private FSM fsmCombate;
    private BehaviourTree btGrito;

    [SerializeField] private BSRuntimeDebugger _debugger;
    [SerializeField] private AudioClip audio;
    private AudioSource _source;
    protected override void Init()
    {
        zombi = GetComponent<ZColosal>();
        rb = GetComponent<Rigidbody>();
        colosalAnim = GetComponentInChildren<Animator>();
        _source = GetComponent<AudioSource>();

        if(_source != null)
        {
            _source.clip = audio;
        }

        if (zombi.jugador != null)
            jugadorVida = zombi.jugador.GetComponent<PlayerHealth>();

        agent = GetComponent<NavMeshAgent>();
        if (agent == null) agent = gameObject.AddComponent<NavMeshAgent>();

        agent.updatePosition = false;
        agent.updateRotation = false;

        percepcionOlor = new OdorPerception(transform, zombi.radioOlfato);
        destinoPatrulla = transform.position;

        ultimoGritoTime = Time.time; // Evita que grite nada más nacer

        _debugger = GetComponent<BSRuntimeDebugger>();

        base.Init();
    }

    protected override void OnUpdated()
    {
        DecoyBehaviour senueloActual = FindFirstObjectByType<DecoyBehaviour>();

        // Si detecta un señuelo nuevo en el mapa, pone a cero su memoria del rastro
        if (senueloActual != null && senueloActual != ultimoSenuelo)
        {
            ultimoSenuelo = senueloActual;
            haTerminadoRastro = false;
        }

        // Si hay señuelo y AÚN NO ha llegado a tu posición real, va a por él
        if (senueloActual != null && !haTerminadoRastro)
        {
            targetActual = senueloActual.transform;
        }
        else
        {
            targetActual = zombi.jugador; // Si se acaba el rastro o no hay señuelo, va a por ti
        }

        if (zombi != null && zombi.jugador != null)
        {
            distanciaAlJugador = Vector3.Distance(transform.position, zombi.jugador.position);
        }

        base.OnUpdated();
    }

    private void FixedUpdate()
    {
        if (agent != null && agent.enabled && !agent.updatePosition)
            agent.nextPosition = rb.position;
    }

    protected override BehaviourGraph CreateGraph()
    {
        UtilitySystem us = new UtilitySystem(1.3f);

        fsmCombate = CreateFSMCombate();
        btGrito = CreateBTGrito();

        // Evaluador del grito
        VariableFactor fGritoDisponibilidad = us.CreateVariable(GetGritoDisponibilidad, 0f, 1f);

        // Evaluador del olor
        VariableFactor fRastroOlor = us.CreateVariable(GetRastroOlor, 0f, 1f);

        // Evaluador del combate
        VariableFactor fCombatePorDefecto = us.CreateVariable(GetCombatePorDefecto, 0f, 1f);

        // Acciones
        FunctionalAction actionGrito = new FunctionalAction
        {
            onStarted = () => btGrito.Start(),
            onUpdated = () => { btGrito.Update(); return Status.Running; },
            onStopped = () => btGrito.Stop()
        };

        FunctionalAction actionRastreo = new FunctionalAction
        {
            onStarted = () => {
                indiceMigaActual = 0; // Empieza a rastrear desde la primera huella
            },
            onUpdated = () => TickRastreoOlor()
        };

        FunctionalAction actionCombate = new FunctionalAction
        {
            onStarted = () => fsmCombate.Start(),
            onUpdated = () => { fsmCombate.Update(); return Status.Running; },
            onStopped = () => fsmCombate.Stop()
        };

        us.CreateAction(fGritoDisponibilidad, actionGrito);
        us.CreateAction(fRastroOlor, actionRastreo);
        us.CreateAction(fCombatePorDefecto, actionCombate);

        _debugger.RegisterGraph(us, "Main US");

        return us;
    }

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
                
        ConditionPerception veAlObjetivo = new ConditionPerception(CheckVeAlObjetivo);
        ConditionPerception pierdeAlObjetivo = new ConditionPerception(CheckPierdeAlObjetivo);
        ConditionPerception enRangoMele = new ConditionPerception(CheckEnRangoMele);
        ConditionPerception saleRangoMele = new ConditionPerception(CheckSaleRangoMele);

        fsm.CreateTransition("VeObjetivo", estadoRoam, estadoChase, veAlObjetivo);
        fsm.CreateTransition("PierdeObjetivo", estadoChase, estadoRoam, pierdeAlObjetivo);

        fsm.CreateTransition("EnRangoAtaque", estadoChase, estadoAttack, enRangoMele);
        fsm.CreateTransition("FueraRangoAtaque", estadoAttack, estadoChase, saleRangoMele);

        fsm.SetEntryState(estadoRoam);

        _debugger.RegisterGraph(fsm, "Sub_FSM");

        return fsm;
    }

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

        _debugger.RegisterGraph(bt, "Sub_BT");

        return bt;
    }

    private void EmitirGritoLiderDeHorda()
    {
        //Debug.Log("[COLOSAL] ¡RUGIDO INVOCADO! Reclutando a la horda...");

        Collider[] cercanos = Physics.OverlapSphere(transform.position, zombi.radioLlamadaHorda);
        _source.Play();
        foreach (var col in cercanos)
        {            
            var runner = col.GetComponent<ZombieFSMBehaviourRunner>();
            if (runner != null) { runner.target = transform; runner.pushHordeSignal = true; }

            var normal = col.GetComponent<ZNormalBehaviour>();
            if (normal != null) { normal.targetActual = transform; normal.pushHordeSignal = true; }
        }
    }

    // Evaluadores del sistema de utilidad

    private float GetGritoDisponibilidad()
    {
        if (estaGritando) return 1.0f;
        if (Time.time < ultimoGritoTime + zombi.cooldownGrito) return 0f;
        if (senueloDetectado != null) return 0f; // No grita si hay señuelo distrayendo

        // Llama a la horda si ve al jugador de lejos
        if (distanciaAlJugador >= zombi.distanciaMinimaParaGritar && CheckJugadorEnCono()) return 1f;
        return 0f;
    }

    private float GetRastroOlor()
    {
        if (haTerminadoRastro) return 0f; // Si ya te ha encontrado, el olor se apaga

        // Solo activa el olor si hay un señuelo en el mapa Y además huele al jugador
        if (targetActual != zombi.jugador && percepcionOlor != null && percepcionOlor.Check())
        {
            return 0.85f;
        }
        return 0f;
    }

    private float GetCombatePorDefecto()
    {
        if (distanciaAlJugador <= zombi.rangoAtaque && senueloDetectado == null) return 0.95f;
        return 0.4f;
    }

    // Percepciones de la FSM
    private bool CheckVeAlObjetivo()
    {
        return CheckObjetivoEnCono() && Vector3.Distance(transform.position, targetActual.position) <= zombi.rangoVision;
    }

    private bool CheckPierdeAlObjetivo()
    {
        return !CheckObjetivoEnCono() || Vector3.Distance(transform.position, targetActual.position) > zombi.rangoVision;
    }

    private bool CheckEnRangoMele()
    {
        return Vector3.Distance(transform.position, targetActual.position) <= zombi.rangoAtaque;
    }

    private bool CheckSaleRangoMele()
    {
        return Vector3.Distance(transform.position, targetActual.position) > zombi.rangoAtaque;
    }

    // Seguimiento de olor
    private Status TickRastreoOlor()
    {
        if (colosalAnim != null) colosalAnim.SetBool("Movimiento", true);
        if (agent != null) agent.isStopped = false;

        var puntosRastro = PlayerOdorTrail.Instance.rastroPosiciones;
        if (puntosRastro.Count == 0) return Status.Failure;

        if (indiceMigaActual >= puntosRastro.Count)
            indiceMigaActual = puntosRastro.Count - 1;

        Vector3 puntoObjetivo = puntosRastro[indiceMigaActual];
        agent.SetDestination(puntoObjetivo);
        MoverFisicamenteHaciaCamino(zombi.speedPersecucion);

        // Si llega a la huella, pasa a la siguiente
        if (Vector3.Distance(transform.position, puntoObjetivo) < 1.5f)
        {
            if (indiceMigaActual < puntosRastro.Count - 1)
            {
                indiceMigaActual++;
            }
            else
            {
                // Ha llegado a la última miga de pan
                haTerminadoRastro = true;
               // Debug.Log("<color=orange><b>[CEREBRO COLOSAL]</b> ¡Rastro terminado! He llegado hasta el jugador.</color>");
            }
        }

        return Status.Running;
    }

    // Roaming
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

            //if (targetActual == senueloDetectado?.transform)
                //Debug.Log("[CEREBRO COLOSAL] Persiguiendo el SEÑUELO (Modo Combate normal).");
        }
        return Status.Running;
    }

    private Status ExecuteAtaqueMelee()
    {
        if (colosalAnim != null) { colosalAnim.SetBool("Movimiento", false); colosalAnim.SetBool("Ataque", true); }

        if (targetActual != null)
        {
            Vector3 dir = (targetActual.position - rb.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero) rb.MoveRotation(Quaternion.LookRotation(dir));

            // Si es el jugador y está en rango, daño
            if (targetActual == zombi.jugador && jugadorVida != null && jugadorVida.GetVidaActual() > 0 && distanciaAlJugador <= zombi.rangoAtaque)
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

    private bool CheckObjetivoEnCono()
    {
        if (targetActual == null) return false;
        Vector3 direccion = (targetActual.position - transform.position).normalized;
        float angulo = Vector3.Angle(transform.forward, direccion);
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