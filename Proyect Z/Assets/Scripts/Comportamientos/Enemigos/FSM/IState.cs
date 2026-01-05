using UnityEngine;

public interface IState
{
    void Enter();      // Al entrar en el estado
    void Update();    // Lógica continua llamada desde Update()

    //void FixedUpdate(); // Lógica continua llamada desde FixedUpdate()
    void Exit();       // Al salir del estado
}
