using BehaviourAPI.BehaviourTrees;
using BehaviourAPI.Core;
using BehaviourAPI.Core.Actions;
using BehaviourAPI.Core.Perceptions;
using BehaviourAPI.StateMachines;
using BehaviourAPI.UnityToolkit;
using BehaviourAPI.UnityToolkit.GUIDesigner.Runtime;
using System.Runtime.CompilerServices;
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
    public float rangoDespertar = 4f; // Distancia a la que el zombie se levanta del suelo
    public bool empiezaTirado; // Decide si el zombie empieza en el estado Letargo o no

    public float speed;
    public float speedRotation;

    [Header("Ataque")]
    public float cadenciaAtaque = 1.5f; // Segundos entre cada zarpazo
    private float ultimoAtaqueTime = 0f;

    // Destino de movimiento aleatorio
    private Vector3 destino;
    private float temporizadorSeguridadMover = 0f; // Evita atascos temporales

    [Header("Objetivo Actual (Jugador o Señuelo)")]
    public Transform targetActual; // Para desviar la atención con el señuelo


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
        targetActual = jugador; // Al nacer, el objetivo por defecto es el jugador

        zDormido = GetComponentInChildren<TextMeshPro>(true);

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

        //Debug.Log("ESPERANDO");

        tiempo += Time.deltaTime;

        if (tiempo >= tiempoMax)
        {
            tiempo = 0f; // reset

            //Debug.Log("TIEMPO ESPERANDO TERMINADO");
            //zombiAnim.SetBool("Movimiento", true);
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

        //Debug.Log("MOVIENDOME");
        // Activar animación de caminar
        zombiAnim.SetBool("Movimiento", true);

        // Acumulamos el tiempo de movimiento
        temporizadorSeguridadMover += Time.deltaTime;

        // Si lleva más de 3.5 segundos intentando llegar al nuevo punto de roaming, cambia
        if(temporizadorSeguridadMover > 3.5f)
        {
            temporizadorSeguridadMover = 0f;
            zombiAnim.SetBool("Movimiento", false);
            return Status.Success;
        }

        // Si está chocando de frente contra un obstáculo y su velocidad física es prácticamente 0, cambia
        if(temporizadorSeguridadMover > 0.5f && rb.linearVelocity.sqrMagnitude < 0.02f)
        {
            temporizadorSeguridadMover = 0f;
            zombiAnim.SetBool("Movimiento", false);
            return Status.Success;
        }

        //Debug.Log("Movimiento: " + zombiAnim.GetBool("Movimiento"));
        //Debug.Log("Ataque: " + zombiAnim.GetBool("Ataque"));

        //Debug.Log("Moviendose a: " + destino);
        Vector3 dir = (destino - rb.position).normalized; // Dirección del movimiento

        // Rotación suave hacia el destino
        Quaternion targetRot = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
        Quaternion smoothRot = Quaternion.RotateTowards(
            rb.rotation,
            targetRot,
            speedRotation * Time.deltaTime
        );
        rb.MoveRotation(smoothRot); // Rotación suave

        //Movimiento hacia el destino
        rb.MovePosition(rb.transform.position + dir * speed * Time.deltaTime);

        // ¿Ya en destino?
        if (Vector3.Distance(rb.position, destino) < 0.4f)
        {
            //Debug.Log("Destino alcanzado");
            temporizadorSeguridadMover = 0f;
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
        // Generamos el punto aleatorio original
        Vector3 puntoAleatorio = transform.position + new Vector3(
            Random.Range(-rangoMovimiento, rangoMovimiento),
            0,
            Random.Range(-rangoMovimiento, rangoMovimiento)
        );

        // Preguntamos al NavMesh por el punto legal más cercano en un radio para no atravesar paredes
        NavMeshHit hit;
        if (NavMesh.SamplePosition(puntoAleatorio, out hit, rangoMovimiento, NavMesh.AllAreas))
        {
            destino = hit.position; // Guardamos el punto corregido dentro del coliseo
        }
        else
        {
            destino = transform.position; // Failsafe: si falla, se queda donde está
        }

        temporizadorSeguridadMover = 0f; // Reseteamos el reloj de seguridad
        //Debug.Log("NUEVO DESTINO ELEGIDO: " + destino);
        return Status.Success;
    }

    private Status PerseguirPj()
    {
        zDormido.enabled = false;

        // Si el agente estaba apagado (porque estaba tirado), lo encendemos y lo ponemos de pie
        if (agent != null && !agent.enabled)
        {
            // Rigidbody del padre de pie (Rotación X a 0)
            //rb.transform.rotation = Quaternion.Euler(0f, rb.transform.rotation.eulerAngles.y, 0f);

            
            
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

        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // Failsafe por si el señuelo se rompe en milisegundos
        if (targetActual == null) targetActual = jugador;

        // Calculamos distancia y dirección usando targetActual (Jugador o Señuelo)
        distanciaAlJugador = Vector3.Distance(targetActual.position, rb.position);
        Debug.Log($"[ZOMBIE NORMAL] La distancia al objetivo es de {distanciaAlJugador}");

        Vector3 direccionAlJugador = (targetActual.position - rb.position).normalized;
        float anguloEntreZombiYJugador = Vector3.Angle(transform.forward, direccionAlJugador);

        if (targetActual == null)
        {
            Debug.Log("Objetivo perdido. Paso a BUSCANDO");
            return Status.Failure;
        }

        if (distanciaAlJugador <= rangoAtaque)
        {
            zombiAnim.SetBool("Movimiento", false); // Parar animación de caminar
            Debug.Log("Objetivo en rango de ataque. Paso a ATACAR");
            return Status.Success;
        }

        bool estaAtascado = rb.linearVelocity.magnitude < 0.2f;

        // Si persigue al jugador real, vigilamos el ángulo de visión. Si va a por el señuelo, va ciego a por él.
        if (targetActual == jugador)
        {
            if (distanciaAlJugador > rangoPersecucion || (anguloEntreZombiYJugador > anguloVision && distanciaAlJugador > (rangoAtaque + 0.5f) && !estaAtascado))
            {
                Debug.Log("Jugador perdido de vista o fuera de rango. Paso a BUSCANDO");
                zombiAnim.SetBool("Movimiento", false);
                return Status.Failure;
            }
        }
        else // Si es el señuelo, solo se rinde si se destruye o se aleja demasiado
        {
            if (distanciaAlJugador > rangoPersecucion) // Failsafe de distancia del señuelo
            {
                targetActual = jugador;
                return Status.Failure;
            }
        }

        // Activar animación de caminar
        zombiAnim.SetBool("Movimiento", true);

        // Cálculo de dirección usando el NavMesh
        agent.SetDestination(targetActual.position);

        // CORRECCIÓN ROTACIÓN: Usamos el steeringTarget del NavMesh para calcular la dirección del giro real, evitando el balanceo raro
        Vector3 direccionGiro = (agent.steeringTarget - rb.position).normalized;
        direccionGiro.y = 0;

        // Mover hacia el objetivo
        rb.MovePosition(rb.position + direccionGiro * speed * Time.deltaTime);

        if (direccionGiro != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direccionGiro);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRotation, speedRotation * Time.deltaTime));
        }

        return Status.Running; // Continuar persiguiendo
    }

    private bool JugadorEnCono()
    {
        // Calcular distancia al jugador
        distanciaAlJugador = Vector3.Distance(jugador.position, rb.position);
        AnglePerceptionCustom vision = new AnglePerceptionCustom(rb.transform, jugador, anguloVision);

        if (distanciaAlJugador > rangoPersecucion)
        {
            //Debug.Log("Jugador fuera de rango de persecución");
            return false; // El jugador está demasiado lejos
        }

        // Verificar si el jugador está en el cono de visión
        bool enCono = vision.Check(); // Verifica si el jugador está dentro del ángulo de visión
        return enCono;
    }

    private Status AtacarPj()
    {
        // SOLUCIÓN ESCALADO EN ATAQUE
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        zombiAnim.SetBool("Ataque", true); // Asegurar que la animación de ataque esté activa

        if (targetActual == null || (targetActual == jugador && jugadorVida != null && jugadorVida.GetVidaActual() <= 0))
        {
            Debug.Log("No hay objetivo válido o jugador muerto. Paso a BUSCANDO");
            zombiAnim.SetBool("Ataque", false); // Parar animación de ataque
            return Status.Success;
        }

        distanciaAlJugador = Vector3.Distance(targetActual.position, rb.position);

        // Si el objetivo se escapa del rango de ataque, fallamos para volver a perseguir
        if (distanciaAlJugador > (rangoAtaque + 0.4f))
        {
            Debug.Log("Objetivo fuera de rango de ataque. Paso a PERSIGUIENDO");
            zombiAnim.SetBool("Ataque", false); // Parar animación
            return Status.Failure;
        }

        // Mirar al objetivo actual (Jugador o Señuelo) mientras ataca
        Vector3 dir = (targetActual.position - rb.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            rb.MoveRotation(Quaternion.LookRotation(dir));
        }

        if (Time.time >= ultimoAtaqueTime + cadenciaAtaque)
        {
            if (targetActual == jugador && jugadorVida != null)
            {
                jugadorVida.RecibirDaño(zombi.damage);
            }
            ultimoAtaqueTime = Time.time;
        }

        return Status.Running;
    }

    private Status EstarTirado()
    {
        zDormido.enabled = true;

        // Apagamos animaciones
        zombiAnim.SetBool("Movimiento", false);
        zombiAnim.SetBool("Ataque", false);

        // Apagamos el NavMeshAgent temporalmente
        if (agent != null && agent.enabled)
        {
            agent.enabled = false;
        }

        //Mostramos Z de dormido y modificamos tyransparencia

        
        

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