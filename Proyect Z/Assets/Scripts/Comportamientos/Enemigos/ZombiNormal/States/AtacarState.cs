using UnityEngine;
using BehaviourAPI.Core;
using BehaviourAPI.UnityToolkit;


public class AtacarState : IState
{
    private ZNormal zombi;
    private Transform jugador;

    private IState siguienteEstado;

    public AtacarState(ZNormal zombi)
    {
        this.zombi = zombi;
        this.jugador = zombi.jugador;
    }

    public void Enter()
    {
        Debug.Log("Estado: Atacando al Jugador");
    }
    /*public void FixedUpdate()
    {
        siguienteEstado?.FixedUpdate();
    }*/
    public void Update()
    {
        if (jugador == null)
            siguienteEstado = new BuscarState(zombi);

        //Calcular Distancia al jugador

        Vector3 direction = (jugador.position - zombi.rb.position).normalized;

        float distanciaAlJugador = Vector3.Distance(jugador.position, zombi.rb.position);
        
        if (distanciaAlJugador > zombi.rangoAtaque)
        {
            siguienteEstado = new PerseguirState(zombi);
            Exit();
        }
        
    }

    public void Exit()
    {
        Debug.Log("Saliendo de Perseguir");

        if (siguienteEstado != null)
        {
            zombi.fsm.ChangeState(siguienteEstado);
        }

    }
}

