using BehaviourAPI.BehaviourTrees;
using BehaviourAPI.Core;
using BehaviourAPI.Core.Actions;
using BehaviourAPI.Core.Perceptions;
using BehaviourAPI.StateMachines;
using BehaviourAPI.UnityToolkit;
using BehaviourAPI.UnityToolkit.GUIDesigner.Runtime;
using System.Runtime.CompilerServices;
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

    public float speed;
    public float speedRotation;

    //Destino de movimiento aleatorio
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
        speedRotation = zombi.speedRotation; //Grados por segundo
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


    //FSM
    protected override BehaviourGraph CreateGraph()
    {
        var fsm = new FSM();

        // Estados
        var buscarBT = CreateBuscarBT();
        var buscarPJ = fsm.CreateState("Buscar", new BehaviourTreeAction(buscarBT,this));
        var perseguir = fsm.CreateState("Perseguir", new FunctionalAction(PerseguirPj));
        var atacar = fsm.CreateState("Atacar", new FunctionalAction(AtacarPj));

        // Percepciones
        var jugadorCerca = new DistancePerception(jugador, rangoPersecucion);
        var jugadorEnRangoAtaque = new DistancePerception(jugador, rangoAtaque);

        var jugadorEnVision = new AnglePerceptionCustom(rb.transform,jugador, 45f);

        var verJugador = new AndPerception(jugadorEnVision, jugadorCerca);

        // Transiciones
        fsm.CreateTransition("Jugador detectado", buscarPJ, perseguir, statusFlags: StatusFlags.Failure);
        fsm.CreateTransition("Jugador cerca", perseguir, atacar, statusFlags: StatusFlags.Success);
        fsm.CreateTransition("Jugador lejos", atacar, perseguir, statusFlags: StatusFlags.Failure);
        fsm.CreateTransition("Jugador perdido", perseguir, buscarPJ, statusFlags: StatusFlags.Failure);
        fsm.CreateTransition("Jugador muerto", atacar, buscarPJ, statusFlags: StatusFlags.Success);

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

        SequencerNode Patrullando = bt.CreateComposite<SequencerNode>(false, nodoElegirDestino,nodoMover, nodoEsperar);
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

        // Ver si ha completado el giro
        float angle = Quaternion.Angle(rb.rotation, targetRot);
        if (angle > 2f) // Si todavía no está alineado, NO avanzar. Tolerancia de 2 grados
            return Status.Running;

        //Movimiento hacia el destino
        rb.MovePosition(rb.transform.position + dir * speed * Time.deltaTime);

        // ¿Ya en destino?
        if (Vector3.Distance(rb.position, destino) < 0.3f)
        {
            //Debug.Log("Destino alcanzado");
            zombiAnim.SetBool("Movimiento", false);
            return Status.Success;
        }

        return Status.Running;
    }

    private FunctionalAction CrearAccionMover() {
        return new FunctionalAction(MoverA);
    }

    private Status ElegirDestino() {
        destino = transform.position + new Vector3(
            Random.Range(-rangoMovimiento, rangoMovimiento),
            0,
            Random.Range(-rangoMovimiento, rangoMovimiento)
        );                  

        //Debug.Log("NUEVO DESTINO ELEGIDO: " + destino);
        return Status.Success; 
    }

    private Status PerseguirPj()
    {
        //Actgualizar distancia al jugador
        distanciaAlJugador = Vector3.Distance(jugador.position, rb.position);

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

        if (distanciaAlJugador > rangoPersecucion || anguloEntreZombiYJugador > anguloVision)
        {
            Debug.Log("Jugador perdido. Paso a BUSCANDO");
            zombiAnim.SetBool("Movimiento", false); // Parar animación de caminar
            return Status.Failure;
        }

        // Activar animación de caminar
        zombiAnim.SetBool("Movimiento", true);

        // Sincroniza la posición del agente
        agent.nextPosition = rb.position;

        // Cálculo de dirección usando el NavMesh
        agent.SetDestination(jugador.position);
        Vector3 direction = (agent.steeringTarget - rb.position).normalized;
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
            //Debug.Log("Jugador fuera de rango de persecución");
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
        if (jugador == null || (jugadorVida !=null && jugadorVida.GetVidaActual() <= 0))
        {
            Debug.Log("No hay jugador. Paso a BUSCANDO");
            //zombiAnim.SetBool("Movimiento", false); // Parar animación de caminar
            zombiAnim.SetBool("Ataque", false); // Parar animación de ataque
            return Status.Success;
        }

        agent.nextPosition = rb.position; // Sincroniza la posición del agente con el Rigidbody

        distanciaAlJugador = Vector3.Distance(jugador.position, rb.position);

        // Si el jugador se escapa del rango de ataque, fallamos para volver a perseguir
        if (distanciaAlJugador > rangoAtaque)
        {
            Debug.Log("Jugador en rango de ataque. Paso a PERSIGUIENDO");
            zombiAnim.SetBool("Ataque", false); // Parar animación
            //zombiAnim.SetBool("Movimiento", true); // Iniciar animación de caminar
            return Status.Failure;
        }

        // Mirar al jugador mientras ataca
        Vector3 dir = (jugador.position - rb.position).normalized;
        dir.y = 0;
        rb.MoveRotation(Quaternion.LookRotation(dir));

        //Debug.Log("Enemigo colision con el jugador");
        PlayerHealth saludJugador = collision.gameObject.GetComponent<PlayerHealth>();
        if (saludJugador != null)
        {
            saludJugador.RecibirDaño(damage);
        }

        //Debug.Log("Movimiento: " + zombiAnim.GetBool("Movimiento"));
        //Debug.Log("Ataque: " + zombiAnim.GetBool("Ataque"));


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
