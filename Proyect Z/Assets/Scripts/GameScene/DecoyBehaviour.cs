using UnityEngine;
using System.Collections;

public class DecoyBehaviour : MonoBehaviour
{
    public int radioAtraccion = 15;   // Radio de atracción de enemigos
    public float duracion = 8f;       // Tiempo que dura el señuelo activo

    private Transform jugadorReal;

    void Start()
    {
        // Guardamos la referencia del jugador para devolverle el foco al destruirse
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            jugadorReal = playerObj.transform;
        }

        StartCoroutine(DecoyLife());
    }

    private IEnumerator DecoyLife()
    {
        float timer = 0f;

        while (timer < duracion)
        {
            AtraerZombies();
            timer += 1f;
            yield return new WaitForSeconds(1f);
        }

        RestaurarZombies();
        Destroy(gameObject);
    }

    private void AtraerZombies()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, radioAtraccion);
        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("Enemy"))
            {
                // Intenta atraer al Zombie Normal
                ZNormalBehaviour zombiNormal = collider.GetComponent<ZNormalBehaviour>();
                if (zombiNormal != null)
                {
                    zombiNormal.targetActual = transform;
                    continue; // Si ya es este, saltamos al siguiente enemigo
                }

                // Intenta atraer al Zombie Corredor
                ZombieFSMBehaviourRunner zombieCorredor = collider.GetComponent<ZombieFSMBehaviourRunner>();
                if (zombieCorredor != null)
                {
                    // Le cambiamos el target de su FSM hacia este señuelo
                    zombieCorredor.target = transform;
                }
            }
        }
    }

    private void RestaurarZombies()
    {
        if (jugadorReal == null) return;

        Collider[] colliders = Physics.OverlapSphere(transform.position, radioAtraccion);
        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("Enemy"))
            {
                // 1 - Restaurar Zombie Normal
                ZNormalBehaviour zombiNormal = collider.GetComponent<ZNormalBehaviour>();
                if (zombiNormal != null && zombiNormal.targetActual == transform)
                {
                    zombiNormal.targetActual = jugadorReal;
                    continue;
                }

                // 2 - Restaurar Zombie Corredor
                ZombieFSMBehaviourRunner zombieCorredor = collider.GetComponent<ZombieFSMBehaviourRunner>();
                if (zombieCorredor != null && zombieCorredor.target == transform)
                {
                    // Le devolvemos el target al jugador real
                    zombieCorredor.target = jugadorReal;
                }
            }
        }
    }

    // Dibuja el radio de acción en el editor de Unity al seleccionarlo
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radioAtraccion);
    }
}