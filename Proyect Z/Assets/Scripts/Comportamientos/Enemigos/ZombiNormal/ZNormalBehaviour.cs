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
        var buscarPJ = fsm.CreateState("Buscar", new BehaviourTreeAction(buscarBT));
        var perseguir = fsm.CreateState("Perseguir", new FunctionalAction(PerseguirPj));
        var atacar = fsm.CreateState("Atacar", new FunctionalAction(AtacarPj));

        // Percepciones
        var jugadorCerca = new DistancePerception(jugador, rangoPersecucion);
        var jugadorEnRangoAtaque = new DistancePerception(jugador, rangoAtaque);

        var jugadorEnVision = new AnglePerceptionCustom(rb.transform,jugador, 45f);

        var verJugador = new AndPerception(jugadorEnVision, jugadorCerca);

        // Transiciones
        fsm.CreateTransition("Jugador detectado", buscarPJ, perseguir, verJugador);
        fsm.CreateTransition("Jugador cerca", perseguir, atacar, jugadorEnRangoAtaque);
        fsm.CreateTransition("Jugador lejos", atacar, perseguir, statusFlags: StatusFlags.Failure);
        fsm.CreateTransition("Jugador perdido", perseguir, buscarPJ, statusFlags: StatusFlags.Failure);
        fsm.CreateTransition("Jugador muerto", atacar, buscarPJ, statusFlags: StatusFlags.Success);

        return fsm;
    }



    //BT
    private BehaviourTree CreateBuscarBT()
    {
        BehaviourTree bt = new BehaviourTree();

        Debug.Log("BUSCANDO");
                
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
            return Status.Success;

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
        if (zombiAnim != null && !zombiAnim.GetCurrentAnimatorStateInfo(0).IsName("Caminar"))
        {
            zombiAnim.CrossFade("Caminar", 0.2f); // Transición suave de 0.2 segundos
        }

        //Debug.Log("PERSECUCION");
        if (jugador == null)
        {
            return Status.Failure;
        }

        // 1. Sincroniza la posición del agente con el Rigidbody para que el NavMesh no se desfase
        agent.nextPosition = rb.position;

        // 2. Cálculo de dirección usando el NavMesh (evita que atraviese paredes)
        agent.SetDestination(jugador.position);
        Vector3 direction = (agent.steeringTarget - rb.position).normalized;
        direction.y = 0; // Evita que el zombi se incline hacia arriba/abajo

        // Movimiento del zombi hacia el jugador
        Vector3 newPosition = rb.position + direction * speed * Time.deltaTime;
        rb.MovePosition(newPosition);
        // Rotación hacia el jugador
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(targetRotation);
        }
        //Calcular Distancia al jugador
        distanciaAlJugador = Vector3.Distance(jugador.position, rb.position);
        if (distanciaAlJugador <= (rangoAtaque + 0.2f)) {
            zombiAnim.SetBool("Movimiento", false); // Parar animación de caminar
            //zombiAnim.SetBool("Ataque", true); // Iniciar animación
            //zombiAnim.Play("Mordisco");
            Debug.Log("ATACANDO");
            
            return Status.Success; // Cambiar al estado de atacar
            
        }
        if (distanciaAlJugador > rangoPersecucion)
        {
            Debug.Log("BUSCANDO");
            zombiAnim.Play("Idle");
            zombiAnim.SetBool("Movimiento", false); // Parar animación de caminar
            //zombiAnim.SetBool("Ataque", false); // Parar animación de ataque
            return Status.Failure; // Perder al jugador
        }
        
        return Status.Running; // Continuar persiguiendo
    }

    private Status AtacarPj() {
        //Debug.Log("ATAQUE");
        if (jugador == null || (jugadorVida !=null && jugadorVida.GetVidaActual() <= 0))
        {
            Debug.Log("BUSCANDO");
            zombiAnim.SetBool("Movimiento", false); // Parar animación de caminar
            zombiAnim.SetBool("Ataque", false); // Parar animación de ataque
            return Status.Success;
        }

        distanciaAlJugador = Vector3.Distance(jugador.position, rb.position);

        // Si el jugador se escapa del rango de ataque, fallamos para volver a perseguir
        if (distanciaAlJugador > rangoAtaque + 0.2f)
        {
            zombiAnim.SetBool("Ataque", false); // Parar animación
            //zombiAnim.SetBool("Movimiento", true); // Iniciar animación de caminar
            return Status.Failure;
        }

        // Mirar al jugador mientras ataca
        Vector3 dir = (jugador.position - rb.position).normalized;
        dir.y = 0;
        rb.MoveRotation(Quaternion.LookRotation(dir));

        // Ejecutamos animación de ataque
        AnimatorStateInfo stateInfo = zombiAnim.GetCurrentAnimatorStateInfo(0);

        if (!stateInfo.IsName("Mordisco") && !zombiAnim.IsInTransition(0))
        {
            zombiAnim.SetBool("Ataque", true);
        }


        return Status.Running;
    }
}


// Clases custom de ejecución del BT y percepciones
public class BehaviourTreeAction : Action {
    public BehaviourTree _bt;
    public BehaviourTreeAction(BehaviourTree bt) { 
        _bt = bt; 
    } 
    public override void Start() {
        _bt.Start();
    } 
    public override Status Update() {

        _bt.Update();
        return Status.Running;
    }
    public override void Stop() {
        Debug.Log("PERSIGUIENDO");
        _bt.Stop(); 
    } 
}

public class AnglePerceptionCustom : Perception
{
    Transform _self;
    Transform _target;
    float _maxAngle;

    public AnglePerceptionCustom(Transform self, Transform target, float maxAngle)
    {
        _self = self;
        _target = target;
        _maxAngle = maxAngle;
    }

    public override bool Check()
    {
        Vector3 dir = (_target.position - _self.position).normalized;
        float angle = Vector3.Angle(_self.forward, dir);
        return angle <= _maxAngle * 0.5f;
    }
}
