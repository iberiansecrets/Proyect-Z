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
using static UnityEngine.GraphicsBuffer;

public class ZNormalBehaviour : BehaviourRunner
{
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

    public Transform target;    // Objetivo actual si lo hay (jugador o señuelo)

    // Navegación con NavMesh
    private NavMeshAgent agent;
    private Transform decoyTarget;     // Referencia al señuelo actual (si hay)
    private bool followingDecoy = false;

    private bool isStunned = false;

    //Destino de movimiento aleatorio
    private Vector3 destino;

    //Parámetros de Animación
    private float ataqueStartTime = -1f;
    private float ataqueDuration = 0f;

    public float velocidad;
    [SerializeField] public Animator zombiAnim;


    [Header("Referencia al Zombi y al jugador")]
    public Rigidbody rb;
    public ZNormal zombi;
    public Transform jugador;

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
        
        // Agente NavMesh
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }

        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.speed = speed;
        agent.radius = 0.4f;
        agent.height = 2;
        agent.acceleration = 999;
        agent.angularSpeed = 720;


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
        fsm.CreateTransition("Jugador lejos", atacar, perseguir, jugadorCerca);
        fsm.CreateTransition("Jugador perdido", perseguir, buscarPJ, statusFlags: StatusFlags.Failure);
        fsm.CreateTransition("Jugador muerto", atacar, buscarPJ, statusFlags: StatusFlags.Failure);

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
        Debug.Log("Moviendose a: " + destino);
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

        Debug.Log("NUEVO DESTINO ELEGIDO: " + destino);
        return Status.Success; 
    }

    private Status PerseguirPj()
    {
        Debug.Log("PERSECUCION");
        if (jugador == null)
        {
            return Status.Failure;
        }

        // Dirección hacia el jugador
        Vector3 direction = (jugador.position - rb.position).normalized;
        
        // Actualiza el destino del NavMesh (jugador o señuelo)
        agent.SetDestination(rb.position);

        // Dirección usando NavMesh
        Vector3 NMdirection;

        // steeringTarget = el siguiente punto del camino
        Vector3 steering = agent.steeringTarget - transform.position;
        steering.y = 0f;

        if (steering.magnitude > 0.1f)
        {
            NMdirection = steering.normalized;
        }
        else
        {
            // Si el NavMesh está muy cerca del punto, usa tu método antiguo
            NMdirection = (rb.position - transform.position);
            NMdirection.y = 0f;
            NMdirection.Normalize();
        }

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
        if (distanciaAlJugador <= rangoAtaque)
        {
            return Status.Success; // Cambiar al estado de atacar
        } else if (distanciaAlJugador > rangoPersecucion)
        {
            return Status.Failure; // Perder al jugador
        }
        return Status.Running; // Continuar persiguiendo
    }

    public void SetDecoyTarget(Transform decoy)
    {
        decoyTarget = decoy;
        target = decoy;
        followingDecoy = true;
    }

    // Vuelve al jugador como objetivo
    public void ResetTarget()
    {
        followingDecoy = false;
        decoyTarget = null;
        target = null;
    }

    private Status AtacarPj() {

        if (jugador == null)
            return Status.Failure;

        if (ataqueStartTime < 0f)
        {
            zombiAnim.SetBool("Movimiento", false);
            zombiAnim.SetBool("Ataque", true); // Esperar un frame antes de leer el estado
            AnimatorStateInfo stateInfo = zombiAnim.GetCurrentAnimatorStateInfo(0); 
            ataqueDuration = stateInfo.length;
            ataqueStartTime = Time.time; return Status.Running;
        }
        // Comprobar si ya terminó la animación
        if (Time.time >= ataqueStartTime + ataqueDuration) 
            ataqueStartTime = -1f; // Reset para el próximo ataque

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
