using System;
using System.Collections.Generic;
using UnityEngine;
using BehaviourAPI.Core;
using BehaviourAPI.Core.Actions;
using BehaviourAPI.Core.Perceptions;
using BehaviourAPI.UnityToolkit;
using BehaviourAPI.BehaviourTrees;


// Use this attribute to include this action in a group: 
// [SelectionGroup("groupName")]
public class BuscarState : IState
{    
    private float tiempo = 0f;
    private float tiempoMax = 5f;
    private float distanciaAlJugador;

    private BehaviourTree tree; // NUEVO
    private ZNormal zombi;
    private Transform jugador;


    private IState siguienteEstado;
    public BuscarState(ZNormal zombi)
    {
        this.zombi = zombi;
        //jugador = zombi.jugador;
    }

    public void Enter()
    {
        //zombi.ElegirDestinoAleatorio();
        //tiempo = 0f;
        Debug.Log("ESTADO: Buscando al Jugador");

        //NUEVO
        tree = CrearArbol();
    }


    public void Update()
    {
        tree.Tick();

        if (VerAlJugador())
        {
            Debug.Log("¡Jugador visto! Cambiando a Perseguir");
            zombi.fsm.ChangeState(new PerseguirState(zombi));
        }
    }

    public void Exit()
    {
        Debug.Log("ESTADO: Saliendo de Buscar");

    }

    public class ElegirDestinoAction : SimpleAction
    {
        private ZNormal zombi;

        public ElegirDestinoAction(ZNormal zombi)
        {
            this.zombi = zombi;
        }

        protected override Status Update()
        {
            zombi.ElegirDestinoAleatorio();
            return Status.Success; // Siempre succeed
        }
    }

    // Acción para mover al destino hasta ver al jugador
    public class MoverAction : SimpleAction
    {
        private ZNormal zombi;

        public MoverAction(ZNormal zombi)
        {
            this.zombi = zombi;
        }

        protected override Status Update()
        {
            zombi.MoverHaciaDestino();
            return VerAlJugador() ? Status.Success : Status.Running;
        }
    }

    private BehaviourTree CrearArbol()
    {
        BehaviourTree tree = new BehaviourTree();

        // Crear nodos hoja con las acciones
        LeafNode elegir = tree.CreateLeafNode(new ElegirDestinoAction(zombi));
        LeafNode mover = tree.CreateLeafNode(new MoverAction(zombi));

        // Decorator LoopUntil para repetir mover hasta ver al jugador
        var moverLoop = tree.CreateDecorator<LoopUntilNode>(mover);
        moverLoop.TargetStatus = Status.Success; // Sale cuando mover devuelve Success

        // Secuencia: Elegir destino → MoverLoop
        var patrullar = tree.CreateComposite<SequencerNode>(false, elegir, moverLoop);

        // Decorator LoopUntil para repetir toda la secuencia indefinidamente
        var buscarJugador = tree.CreateDecorator<LoopUntilNode>(patrullar);
        buscarJugador.TargetStatus = Status.Success;

        // No se asigna tree.Root; BehaviourAPI infiere automáticamente
        return tree;
    }

    private bool VerAlJugador()
    {
        Vector3 dirAlJugador =
            (jugador.position - zombi.rb.position).normalized;

        float angulo =
            Vector3.Angle(zombi.transform.forward, dirAlJugador);

        if (angulo > zombi.anguloVision * 0.5f)
            return false;

        if (Physics.Raycast(
            zombi.rb.position + Vector3.up * 0.5f,
            dirAlJugador,
            out RaycastHit hit,
            zombi.rangoPersecucion))
        {
            return hit.transform == jugador;
        }

        return false;
    }
}
