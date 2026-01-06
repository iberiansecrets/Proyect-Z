using BehaviourAPI.BehaviourTrees;
using BehaviourAPI.Core;
using BehaviourAPI.Core.Actions;
using BehaviourAPI.Core.Perceptions;
using BehaviourAPI.UnityToolkit.GUIDesigner.Runtime;
using UnityEngine;
using BehaviourAPI.StateMachines;
using BehaviourAPI.UnityToolkit;
using System.Runtime.CompilerServices;

public class ZNormalBehaviour : BehaviourRunner
{
    [Header("Datos del Zombi")]
    private float tiempo = 0f;
    private float tiempoMax = 5f;
    private float distanciaAlJugador;
    private Vector3 destinoPatrulla;
    public float rangoPersecucion = 10f;
    public float rangoAtaque = 2f;
    public float rangoMovimiento = 10f;
    public float anguloVision = 45f;

    public float speed;

    //public float damage = 10f;


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

        base.Init();
    }


    //FSM
    protected override BehaviourGraph CreateGraph()
    {
        var fsm = new FSM();

        // Estados
        var buscar = CreateBuscarBT();
        var buscarPJ = fsm.CreateState("Buscar", new SubsystemAction(buscar));
        var perseguir = fsm.CreateState("Perseguir", new FunctionalAction(PerseguirPj));
        var atacar = fsm.CreateState("Atacar", new FunctionalAction(AtacarPj));

        // Percepciones
        var jugadorCerca = new DistancePerception(jugador, rangoPersecucion);
        var jugadorEnRangoAtaque = new DistancePerception(jugador, rangoAtaque);

        var jugadorEnVision = new AnglePerceptionCustom(rb.transform,jugador, 45f);
        //var lineaDeVision = new RayPerception(rb.transform, jugador, rangoPersecucion);

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

        // Elegir destino aleatorio
        Vector3 destino = transform.position + new Vector3(Random.Range(-rangoMovimiento, rangoMovimiento), 0, Random.Range(-rangoMovimiento, rangoMovimiento) );
        
        var moverAction = CrearAccionMover(destino);
        var esperarAction = new FunctionalAction(Esperar);

        //SimpleAction Elegir_Destino_action = new SimpleAction();
        LeafNode nodoEsperar = bt.CreateLeafNode("Esperar", esperarAction);

        LeafNode nodoMover = bt.CreateLeafNode("Mover a destino", moverAction);

        SequencerNode Patrullando = bt.CreateComposite<SequencerNode>(false, nodoMover, nodoEsperar);
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

    private Status MoverA(Vector3 destino)
    {
        Vector3 dir = (destino - rb.position).normalized; // Movimiento
        rb.MovePosition(rb.position + dir * speed * Time.deltaTime); // Rotación
        if (dir != Vector3.zero) {
            Quaternion rot = Quaternion.LookRotation(dir); rb.MoveRotation(rot);
        } //¿Llegó?
        if (Vector3.Distance(rb.position, destino) < 0.5f)
            return Status.Success;
        return Status.Running;
    }

    private FunctionalAction CrearAccionMover(Vector3 destino) {
        return new FunctionalAction(() => MoverA(destino));
    }

    private Status PerseguirPj()
    {
        if (jugador == null)
        {
            return Status.Failure;
        }
        // Dirección hacia el jugador
        Vector3 direction = (jugador.position - rb.position).normalized;
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
        }
        return Status.Running; // Continuar persiguiendo
    }

    /*private bool JugadorEnVision() { 
        Vector3 dirAlJugador = (jugador.position - rb.position).normalized;
        float angulo = Vector3.Angle(transform.forward, dirAlJugador); 
        return angulo <= anguloVision * 0.5f; 
    }*/

    private Status AtacarPj() {

        if (jugador == null)
        {
            return Status.Failure;
        }

        Debug.Log("Atacando al jugador");

        return Status.Running;
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
