using UnityEngine;
using System.Collections;

public class DecoyBehaviour : MonoBehaviour
{
    public int radioAtraccion = 15;   // Radio de atracción de enemigos
    public float duracion = 8f;       // Tiempo que dura el señuelo activo

    void Start()
    {
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
                EnemyController enemy = collider.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    enemy.SetDecoyTarget(transform);
                }
            }
        }
    }

    private void RestaurarZombies()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, radioAtraccion);
        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("Enemy"))
            {
                EnemyController enemy = collider.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    enemy.ResetTarget();
                }
            }
        }
    }
}
