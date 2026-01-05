using UnityEngine;
using BehaviourAPI.Core;
using BehaviourAPI.UnityToolkit;


public class PerseguirState : IState
{
    private ZNormal zombi;
    private Transform jugador;

    private IState siguienteEstado;

    public PerseguirState(ZNormal zombi)
    {
        this.zombi = zombi;
        this.jugador = zombi.jugador;
    }

    public void Enter()
    {
        Debug.Log("ESTADO: Persiguiendo al Jugador");
    }

    /*public void FixedUpdate()
    {
        if (jugador == null)
            siguienteEstado = new BuscarState(zombi);

        // Dirección hacia el jugador
        Vector3 direction = (jugador.position - zombi.rb.position).normalized;

        // Movimiento del zombi hacia el jugador
        Vector3 newPosition = zombi.rb.position + direction * zombi.speed * Time.deltaTime;
        zombi.rb.MovePosition(newPosition);

        // Rotación hacia el jugador
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            zombi.rb.MoveRotation(targetRotation);
        }
    }*/

    public void Update()
    {
        if (jugador == null)
        {
            siguienteEstado = new BuscarState(zombi);
            Exit();
        }

        // Dirección hacia el jugador
        Vector3 direction = (jugador.position - zombi.rb.position).normalized;

        //Calcular Distancia al jugador
        float distanciaAlJugador = Vector3.Distance(jugador.position, zombi.rb.position);

        if (distanciaAlJugador <= zombi.rangoAtaque)
        {
            siguienteEstado = new AtacarState(zombi);
            Exit();
        }else if (distanciaAlJugador > zombi.rangoPersecucion)
        {
            siguienteEstado = new BuscarState(zombi);
            Exit();
        }

        // Movimiento del zombi hacia el jugador
        Vector3 newPosition = zombi.rb.position + direction * zombi.speed * Time.deltaTime;
        zombi.rb.MovePosition(newPosition);

        // Rotación hacia el jugador
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            zombi.rb.MoveRotation(targetRotation);
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
