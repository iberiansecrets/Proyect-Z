using BehaviourAPI.BehaviourTrees;
using BehaviourAPI.Core;
using BehaviourAPI.Core.Actions;
using BehaviourAPI.Core.Perceptions;
using BehaviourAPI.StateMachines;
using BehaviourAPI.UnityToolkit;
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


    [Header("Datos del Zombi")]
    private float tiempo = 0f;
    private float tiempoMax = 5f;
    private float distanciaAlJugador;
    public float rangoPersecucion = 10f;
    public float rangoAtaque = 2f;
    public float rangoMovimiento = 10f;
    public float anguloVision = 45f;
    public float rangoDespertar = 4f; // Distancia a la que el zombie se levanta del suelo
    public bool empiezaTirado; // Decide si el zombie empieza en el estado Letargo o no

    public float speed;
    public float speedRotation;

    // Destino de movimiento aleatorio
    private Vector3 destino;


    public bool jugadorDetectado;

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
        speedRotation = zombi.speedRotation; // Grados por segundo
        destino = transform.position; // Inicializar destino en la posición actual
        zombiAnim = GetComponentInChildren<Animator>();

        jugadorVida = jugador.GetComponent<PlayerHealth>();

        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }

        // Configuración para que no tome el control del movimiento físico
        agent.updatePosition = false;
        agent.updateRotation = false;

        base.Init();
    }

    private void FixedUpdate()
    {
        // El cerebro del NavMesh se arrastra en el ciclo físico exacto, evitando tirones visuales
        if (agent != null && agent.enabled && !agent.updatePosition)
        {
            agent.nextPosition = rb.position;
        }
    }

    //FSM
    protected override BehaviourGraph CreateGraph()
    {
        var fsm = new FSM();

        // Estados
        var buscarBT = CreateBuscarBT();
        var buscarPJ = fsm.CreateState("Buscar", new BehaviourTreeAction(buscarBT, this));
        var perseguir = fsm.CreateState("Perseguir", new FunctionalAction(PerseguirPj));
        var atacar = fsm.CreateState("Atacar", new FunctionalAction(AtacarPj));
        var letargo = fsm.CreateState("Letargo", new FunctionalAction(EstarTirado));

        // Percepciones
        var jugadorCerca = new DistancePerception(jugador, rangoPersecucion);
        var jugadorEnRangoAtaque = new DistancePerception(jugador, rangoAtaque);
        var jugadorEnVision = new AnglePerceptionCustom(rb.transform, jugador, 45f);
        var verJugador = new AndPerception(jugadorEnVision, jugadorCerca);
        var jugadorMuyCerca = new DistancePerception(jugador, rangoDespertar);

        // Transiciones
        fsm.CreateTransition("Jugador detectado", buscarPJ, perseguir, statusFlags: StatusFlags.Failure);
        fsm.CreateTransition("Jugador cerca", perseguir, atacar, statusFlags: StatusFlags.Success);
        fsm.CreateTransition("Jugador lejos", atacar, perseguir, statusFlags: StatusFlags.Failure);
        fsm.CreateTransition("Jugador perdido", perseguir, buscarPJ, statusFlags: StatusFlags.Failure);
        fsm.CreateTransition("Jugador muerto", atacar, buscarPJ, statusFlags: StatusFlags.Success);
        fsm.CreateTransition("Despertar", letargo, perseguir, jugadorMuyCerca);

        // Si 'empiezaTirado' ya está activado en el Inspector, lo respetamos. Si no, lo calcula el azar.
        if (!empiezaTirado)
        {
            float random = Random.value;
            Debug.Log($"[ZOMBIE NORMAL] El valor del número aleatorio es de {random}");
            empiezaTirado = random > 0.5f; // 50% de probabilidad de empezar en el suelo si estaba en false

        }

        if (empiezaTirado)
        {
            fsm.SetEntryState(letargo);
        }
        else
        {
            fsm.SetEntryState(buscarPJ);
        }

        return fsm;
    }

    //BT
    private BehaviourTree CreateBuscarBT()
    {
        BehaviourTree bt = new BehaviourTree();

        //Debug.Log("BUSCANDO");

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
            return Status.Running; // el BT sigue, pero la FSM cortará
        }

        tiempo += Time.deltaTime;

        if (tiempo >= tiempoMax)
        {
            tiempo = 0f; // reset
            return Status.Success;
        }
        return Status.Running;
    }

    private Status MoverA()
    {
        if (JugadorEnCono())
        {
            jugadorDetectado = true;
            return Status.Running; // el BT sigue, pero la FSM cortará
        }

        // Activar animación de caminar
        zombiAnim.SetBool("Movimiento", true);
                
        // Usamos desiredVelocity para tomar las curvas perfectas sin salirnos de la malla
        Vector3 dir = agent.desiredVelocity.normalized;

        // Rotación suave hacia el destino
        Quaternion targetRot = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
        Quaternion smoothRot = Quaternion.RotateTowards(
            rb.rotation,
            targetRot,
            speedRotation * Time.deltaTime
        );
        rb.MoveRotation(smoothRot); // Rotación suave

        // Movimiento hacia el destino
        rb.MovePosition(rb.transform.position + dir * speed * Time.deltaTime);

        // ¿Ya en destino?
        if (Vector3.Distance(rb.position, destino) < 0.3f)
        {
            zombiAnim.SetBool("Movimiento", false);
            return Status.Success;
        }

        return Status.Running;
    }

    private FunctionalAction CrearAccionMover()
    {
        return new FunctionalAction(MoverA);
    }

    private Status ElegirDestino()
    {
        destino = transform.position + new Vector3(
            Random.Range(-rangoMovimiento, rangoMovimiento),
            0,
            Random.Range(-rangoMovimiento, rangoMovimiento)
        );

        return Status.Success;
    }

    private Status PerseguirPj()
    {
        // Si el agente estaba apagado (porque estaba tirado), lo encendemos y lo ponemos de pie
        if (agent != null && !agent.enabled)
        {
            // Rigidbody del padre de pie (Rotación X a 0)
            rb.transform.rotation = Quaternion.Euler(0f, rb.transform.rotation.eulerAngles.y, 0f);

            // Forzamos que el modelo hijo mire hacia adelante (Y = 0) si se había quedado girado
            Transform modeloHijo = zombiAnim != null ? zombiAnim.transform : transform.GetChild(0);
            if (modeloHijo != null)
            {
                modeloHijo.localRotation = Quaternion.Euler(0f, 0f, 0f);
            }

            // Nos aseguramos de que vuelva a ser físico antes de tocar sus velocidades
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            agent.enabled = true;
            int walkableMask = 1 << NavMesh.GetAreaFromName("Walkable");
            agent.areaMask = walkableMask;
            Debug.Log("¡El Zombi Normal se ha levantado de golpe y corregido su orientación!");
        }

        // Actualizar distancia al jugador
        distanciaAlJugador = Vector3.Distance(jugador.position, rb.position);
        Debug.Log($"[ZOMBIE NORMAL] La distancia al jugador es de {distanciaAlJugador}");

        // Calcular dirección y ángulo al jugador
        Vector3 direccionAlJugador = (jugador.position - rb.position).normalized;
        float anguloEntreZombiYJugador = Vector3.Angle(transform.forward, direccionAlJugador);

        if (jugador == null)
        {
            Debug.Log("Jugador null o perdido. Paso a BUSCANDO");
            return Status.Failure;
        }

        if (distanciaAlJugador <= rangoAtaque)
        {
            zombiAnim.SetBool("Movimiento", false); // Parar animación de caminar
            Debug.Log("Jugador en rango de ataque. Paso a ATACAR");
            return Status.Success; // El jugador está lo suficientemente cerca para atacar
        }

        bool estaAtascado = rb.linearVelocity.magnitude < 0.2f;

        if (distanciaAlJugador > rangoPersecucion || (anguloEntreZombiYJugador > anguloVision && distanciaAlJugador > rangoAtaque && !estaAtascado))
        {
            Debug.Log("Jugador perdido. Paso a BUSCANDO");
            zombiAnim.SetBool("Movimiento", false); // Parar animación de caminar
            return Status.Failure;
        }

        // Activar animación de caminar
        zombiAnim.SetBool("Movimiento", true);

        // Cálculo de dirección usando el NavMesh
        agent.SetDestination(jugador.position);
        // Usamos desiredVelocity para tomar las curvas perfectas sin salirnos de la malla
        Vector3 direction = agent.desiredVelocity.normalized;
        direction.y = 0; // Evita que el zombi se incline hacia arriba/abajo

        // Mover hacia el jugador
        rb.MovePosition(rb.position + direction * speed * Time.deltaTime);
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        rb.MoveRotation(
            Quaternion.RotateTowards(
                rb.rotation,
                targetRotation,
                speedRotation * Time.deltaTime
            )
        );

        return Status.Running; // Continuar persiguiendo
    }

    private bool JugadorEnCono()
    {
        // Calcular distancia al jugador
        distanciaAlJugador = Vector3.Distance(jugador.position, rb.position);
        AnglePerceptionCustom vision = new AnglePerceptionCustom(rb.transform, jugador, anguloVision);

        if (distanciaAlJugador > rangoPersecucion)
        {
            return false; // El jugador está demasiado lejos
        }

        // Verificar si el jugador está en el cono de visión
        bool enCono = vision.Check(); // Verifica si el jugador está dentro del ángulo de visión
        return enCono;
    }

    private Status AtacarPj()
    {
        //Debug.Log("Estoy en ATAQUE");
        zombiAnim.SetBool("Ataque", true); // Asegurar que la animación de ataque esté activa
        if (jugador == null || (jugadorVida != null && jugadorVida.GetVidaActual() <= 0))
        {
            Debug.Log("No hay jugador. Paso a BUSCANDO");
            zombiAnim.SetBool("Ataque", false); // Parar animación de ataque
            return Status.Success;
        }

        distanciaAlJugador = Vector3.Distance(jugador.position, rb.position);

        // Si el jugador se escapa del rango de ataque, fallamos para volver a perseguir
        if (distanciaAlJugador > rangoAtaque)
        {
            Debug.Log("Jugador en rango de ataque. Paso a PERSIGUIENDO");
            zombiAnim.SetBool("Ataque", false); // Parar animación
            return Status.Failure;
        }

        // Mirar al jugador mientras ataca
        Vector3 dir = (jugador.position - rb.position).normalized;
        dir.y = 0;
        rb.MoveRotation(Quaternion.LookRotation(dir));

        if (jugadorVida != null)
        {
            jugadorVida.RecibirDaño(zombi.damage);
        }

        return Status.Running;
    }

    private Status EstarTirado()
    {
        // Apagamos animaciones
        zombiAnim.SetBool("Movimiento", false);
        zombiAnim.SetBool("Ataque", false);

        // Apagamos el NavMeshAgent temporalmente
        if (agent != null && agent.enabled)
        {
            agent.enabled = false;
        }

        // Tumbamos al zombi 90 grados en el eje X
        rb.transform.rotation = Quaternion.Euler(90f, rb.transform.rotation.eulerAngles.y, 0f);

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
            Debug.Log("Jugador detectado por el BT. Paso a PERSIGUIENDO");

            return Status.Failure;
        }

        return Status.Running;
    }

    public override void Stop()
    {
        _bt.Stop();
    }   
}
