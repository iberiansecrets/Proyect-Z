using BehaviourAPI.BehaviourTrees;
using BehaviourAPI.Core;
using BehaviourAPI.Core.Actions;
using BehaviourAPI.Core.Perceptions;
using BehaviourAPI.StateMachines.StackFSMs;
using BehaviourAPI.UnityToolkit;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class ZNormalBehaviour : BehaviourRunner
{
    private NavMeshAgent agent;
    [Header("Referencia al Zombi y al jugador")]
    public Rigidbody rb;
    public ZNormal zombi;
    public Transform jugador;
    private PlayerHealth jugadorVida;
    [SerializeField] private Animator zombiAnim;
    private TextMeshPro zDormido;

    [Header("Datos del Zombi")]
    private float tiempo = 0f;
    private float tiempoMax = 5f;
    private float distanciaAlJugador;
    public float rangoPersecucion = 10f;
    public float rangoAtaque = 2f;
    public float rangoMovimiento = 10f;
    public float anguloVision = 45f;
    public float rangoDespertar = 4f;
    public bool empiezaTirado;

    public float speed;
    public float speedRotation;

    [Header("Ataque")]
    public float cadenciaAtaque = 1.5f;
    private float ultimoAtaqueTime = 0f;

    private Vector3 destino;
    private float temporizadorSeguridadMover = 0f;

    [Header("Objetivo Actual (Jugador o Señuelo)")]
    public Transform targetActual;

    public bool jugadorDetectado;

    [Header("Control de Horda (Stack-FSM)")]
    public bool pushHordeSignal = false;
    public bool popHordeSignal = false;

    protected override void Init()
    {
        jugador = GameObject.FindGameObjectWithTag("Player").transform;
        zombi = GetComponent<ZNormal>();
        rb = GetComponent<Rigidbody>();
        rangoPersecucion = zombi.rangoPersecucion;
        rangoAtaque = zombi.rangoAtaque;
        rangoMovimiento = zombi.rangoMovimiento;
        anguloVision = zombi.anguloVision;
        speed = zombi.speed;
        speedRotation = zombi.speedRotation;
        destino = transform.position;
        zombiAnim = GetComponentInChildren<Animator>();

        jugadorVida = jugador.GetComponent<PlayerHealth>();
        targetActual = jugador;

        zDormido = GetComponentInChildren<TextMeshPro>(true);

        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }

        agent.updatePosition = false;
        agent.updateRotation = false;

        base.Init();
    }

    private void FixedUpdate()
    {
        if (agent != null && agent.enabled && !agent.updatePosition)
        {
            agent.nextPosition = rb.position;
        }
    }

    protected override BehaviourGraph CreateGraph()
    {
        // MIGRACIÓN: Cambiado a Máquina con soporte de Pila
        var fsm = new StackFSM();

        // Estados
        var buscarBT = CreateBuscarBT();
        var buscarPJ = fsm.CreateState("Buscar", new BehaviourTreeAction(buscarBT, this));
        var perseguir = fsm.CreateState("Perseguir", new FunctionalAction(PerseguirPj));
        var atacar = fsm.CreateState("Atacar", new FunctionalAction(AtacarPj));
        var letargo = fsm.CreateState("Letargo", new FunctionalAction(EstarTirado));

        // Estado Horda
        var horda = fsm.CreateState("Horda", new FunctionalAction(TickHordeNormal));

        // Percepciones
        var jugadorCerca = new DistancePerception(jugador, rangoPersecucion);
        var jugadorEnRangoAtaque = new DistancePerception(jugador, rangoAtaque);
        var jugadorEnVision = new AnglePerceptionCustom(rb.transform, jugador, 45f);
        var verJugador = new AndPerception(jugadorEnVision, jugadorCerca);
        var jugadorMuyCerca = new DistancePerception(jugador, rangoDespertar);

        // Transiciones Clásicas
        fsm.CreateTransition("Jugador detectado", buscarPJ, perseguir, statusFlags: StatusFlags.Failure);
        fsm.CreateTransition("Jugador cerca", perseguir, atacar, statusFlags: StatusFlags.Success);
        fsm.CreateTransition("Jugador lejos", atacar, perseguir, statusFlags: StatusFlags.Failure);
        fsm.CreateTransition("Jugador perdido", perseguir, buscarPJ, statusFlags: StatusFlags.Failure);
        fsm.CreateTransition("Jugador muerto", atacar, buscarPJ, statusFlags: StatusFlags.Success);
        fsm.CreateTransition("Despertar", letargo, perseguir, jugadorMuyCerca);

        // --- CONEXIONES DE INTERRUPCIÓN DE LA PILA ---
        var pushHordePerception = new ConditionPerception(() => pushHordeSignal);
        var popHordePerception = new ConditionPerception(() => popHordeSignal);

        // Apilamos el estado Horda si el líder llama, viniendo de cualquier comportamiento básico
        fsm.CreatePushTransition("EntrarEnHorda_Buscar", buscarPJ, horda, pushHordePerception);
        fsm.CreatePushTransition("EntrarEnHorda_Perseguir", perseguir, horda, pushHordePerception);

        // Desapilamos al morir el líder
        fsm.CreatePopTransition("SalirDeHorda", horda, popHordePerception);

        if (!empiezaTirado)
        {
            float random = Random.value;
            empiezaTirado = random > 0.5f;
        }

        if (empiezaTirado) fsm.SetEntryState(letargo);
        else fsm.SetEntryState(buscarPJ);

        return fsm;
    }

    private Status TickHordeNormal()
    {
        pushHordeSignal = false; // Consumimos la señal de entrada inmediatamente
        popHordeSignal = false;

        if (zDormido != null && zDormido.enabled) zDormido.enabled = false;
        zombiAnim.SetBool("Movimiento", true);

        if (targetActual == null) targetActual = jugador;

        // Despierta de inmediato al NavMesh si estaba tirado en el suelo
        if (agent != null && !agent.enabled)
        {
            rb.isKinematic = false;
            agent.enabled = true;
        }

        agent.SetDestination(targetActual.position);

        Vector3 direccionGiro = (agent.steeringTarget - rb.position).normalized;
        direccionGiro.y = 0;

        rb.MovePosition(rb.position + direccionGiro * speed * Time.deltaTime);

        if (direccionGiro != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direccionGiro);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRotation, speedRotation * Time.deltaTime));
        }

        // Si han llegado a menos de 4 metros del Colosal, o si el Colosal ya no existe, salen del modo Horda
        if (targetActual == null || Vector3.Distance(transform.position, targetActual.position) < 4f)
        {
            popHordeSignal = true; // Hace Pop de la Stack-FSM y destruye el estado Horda
            targetActual = jugador; // Vuelve a tener al jugador como objetivo
            return Status.Success;
        }

        return Status.Running;
    }

    private BehaviourTree CreateBuscarBT()
    {
        BehaviourTree bt = new BehaviourTree();

        var moverAction = CrearAccionMover();
        var esperarAction = new FunctionalAction(Esperar);
        var elegirDestinoAction = new FunctionalAction(ElegirDestino);

        LeafNode nodoEsperar = bt.CreateLeafNode("Esperar", esperarAction);
        LeafNode nodoElegirDestino = bt.CreateLeafNode("Elegir Destino", elegirDestinoAction);
        LeafNode nodoMover = bt.CreateLeafNode("Mover a destino", moverAction);

        SequencerNode Patrullando = bt.CreateComposite<SequencerNode>(false, nodoElegirDestino, nodoMover, nodoEsperar);
        Patrullando.IsRandomized = false;

        var root = bt.CreateDecorator<LoopNode>(Patrullando);
        bt.SetRootNode(root);

        return bt;
    }

    private Status Esperar()
    {
        if (JugadorEnCono())
        {
            jugadorDetectado = true;
            return Status.Running;
        }

        tiempo += Time.deltaTime;
        if (tiempo >= tiempoMax)
        {
            tiempo = 0f;
            return Status.Success;
        }
        return Status.Running;
    }

    private Status MoverA()
    {
        if (JugadorEnCono())
        {
            jugadorDetectado = true;
            return Status.Running;
        }

        zombiAnim.SetBool("Movimiento", true);
        temporizadorSeguridadMover += Time.deltaTime;

        if (temporizadorSeguridadMover > 3.5f)
        {
            temporizadorSeguridadMover = 0f;
            zombiAnim.SetBool("Movimiento", false);
            return Status.Success;
        }

        if (temporizadorSeguridadMover > 0.5f && rb.linearVelocity.sqrMagnitude < 0.02f)
        {
            temporizadorSeguridadMover = 0f;
            zombiAnim.SetBool("Movimiento", false);
            return Status.Success;
        }

        Vector3 dir = (destino - rb.position).normalized;

        Quaternion targetRot = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
        Quaternion smoothRot = Quaternion.RotateTowards(rb.rotation, targetRot, speedRotation * Time.deltaTime);
        rb.MoveRotation(smoothRot);

        rb.MovePosition(rb.transform.position + dir * speed * Time.deltaTime);

        if (Vector3.Distance(rb.position, destino) < 0.4f)
        {
            temporizadorSeguridadMover = 0f;
            zombiAnim.SetBool("Movimiento", false);
            return Status.Success;
        }

        return Status.Running;
    }

    private FunctionalAction CrearAccionMover() => new FunctionalAction(MoverA);

    private Status ElegirDestino()
    {
        Vector3 puntoAleatorio = transform.position + new Vector3(
            Random.Range(-rangoMovimiento, rangoMovimiento),
            0,
            Random.Range(-rangoMovimiento, rangoMovimiento)
        );

        NavMeshHit hit;
        if (NavMesh.SamplePosition(puntoAleatorio, out hit, rangoMovimiento, NavMesh.AllAreas))
        {
            destino = hit.position;
        }
        else
        {
            destino = transform.position;
        }

        temporizadorSeguridadMover = 0f;
        return Status.Success;
    }

    private Status PerseguirPj()
    {
        if (zDormido != null && zDormido.enabled) zDormido.enabled = false;

        if (agent != null && !agent.enabled)
        {
            Transform modeloHijo = zombiAnim != null ? zombiAnim.transform : transform.GetChild(0);
            if (modeloHijo != null) modeloHijo.localRotation = Quaternion.Euler(0f, 0f, 0f);

            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            agent.enabled = true;
        }

        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (targetActual == null) targetActual = jugador;

        distanciaAlJugador = Vector3.Distance(targetActual.position, rb.position);
        Vector3 direccionAlJugador = (targetActual.position - rb.position).normalized;
        float anguloEntreZombiYJugador = Vector3.Angle(transform.forward, direccionAlJugador);

        if (distanciaAlJugador <= rangoAtaque)
        {
            zombiAnim.SetBool("Movimiento", false);
            return Status.Success;
        }

        bool estaAtascado = rb.linearVelocity.magnitude < 0.2f;

        if (targetActual == jugador)
        {
            if (distanciaAlJugador > rangoPersecucion || (anguloEntreZombiYJugador > anguloVision && distanciaAlJugador > (rangoAtaque + 0.5f) && !estaAtascado))
            {
                zombiAnim.SetBool("Movimiento", false);
                return Status.Failure;
            }
        }
        else
        {
            if (distanciaAlJugador > rangoPersecucion)
            {
                targetActual = jugador;
                return Status.Failure;
            }
        }

        zombiAnim.SetBool("Movimiento", true);
        agent.SetDestination(targetActual.position);

        Vector3 direccionGiro = (agent.steeringTarget - rb.position).normalized;
        direccionGiro.y = 0;

        rb.MovePosition(rb.position + direccionGiro * speed * Time.deltaTime);

        if (direccionGiro != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direccionGiro);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRotation, speedRotation * Time.deltaTime));
        }

        return Status.Running;
    }

    private bool JugadorEnCono()
    {
        distanciaAlJugador = Vector3.Distance(jugador.position, rb.position);
        AnglePerceptionCustom vision = new AnglePerceptionCustom(rb.transform, jugador, anguloVision);

        if (distanciaAlJugador > rangoPersecucion) return false;
        return vision.Check();
    }

    private Status AtacarPj()
    {
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        zombiAnim.SetBool("Ataque", true);

        if (targetActual == null || (targetActual == jugador && jugadorVida != null && jugadorVida.GetVidaActual() <= 0))
        {
            zombiAnim.SetBool("Ataque", false);
            return Status.Success;
        }

        distanciaAlJugador = Vector3.Distance(targetActual.position, rb.position);

        if (distanciaAlJugador > (rangoAtaque + 0.4f))
        {
            zombiAnim.SetBool("Ataque", false);
            return Status.Failure;
        }

        Vector3 dir = (targetActual.position - rb.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero) rb.MoveRotation(Quaternion.LookRotation(dir));

        if (Time.time >= ultimoAtaqueTime + cadenciaAtaque)
        {
            if (targetActual == jugador && jugadorVida != null) jugadorVida.RecibirDaño(zombi.damage);
            ultimoAtaqueTime = Time.time;
        }

        return Status.Running;
    }

    private Status EstarTirado()
    {
        if (zDormido != null) zDormido.enabled = true;
        zombiAnim.SetBool("Movimiento", false);
        zombiAnim.SetBool("Ataque", false);

        if (agent != null && agent.enabled) agent.enabled = false;
        return Status.Running;
    }
}

// Clases custom de ejecución del BT y percepciones
public class BehaviourTreeAction : Action
{
    public BehaviourTree _bt;
    private ZNormalBehaviour _owner;

    public BehaviourTreeAction(BehaviourTree bt, ZNormalBehaviour owner)
    {
        _bt = bt;
        _owner = owner;
    }

    public override void Start()
    {
        _bt.Start();
        _owner.jugadorDetectado = false;
    }

    public override Status Update()
    {
        _bt.Update();

        // Si el jugador ha sido detectado por alguna acción del BT, se informa a la FSM para que corte el BT y cambie de estado
        if (_owner.jugadorDetectado)
        {
            //Debug.Log("Jugador detectado por el BT. Paso a PERSIGUIENDO");

            return Status.Failure;
        }

        return Status.Running;
    }

    public override void Stop()
    {
        _bt.Stop();
    }
}