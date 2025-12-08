using UnityEngine;
using System.Collections.Generic;

public class SniperBulletController : MonoBehaviour
{
    [Header("Parámetros de bala")]
    public float distanciaMaxima = 60f;
    public float damage = 50f;
    public int maxPierceCount = 5;          // Cuántos enemigos puede atravesar como máximo

    private Vector3 puntoInicial;
    private int pierceCount = 0;
    private HashSet<GameObject> enemigosImpactados = new HashSet<GameObject>();

    void Start()
    {
        puntoInicial = transform.position;
    }

    void Update()
    {
        // Destruir la bala si supera su distancia máxima
        float distanciaRecorrida = Vector3.Distance(puntoInicial, transform.position);
        if (distanciaRecorrida >= distanciaMaxima)
        {
            Destroy(gameObject);
        }
    }

    // Este evento se activa cuando el collider (marcado como trigger) entra en contacto con otro collider
    private void OnTriggerEnter(Collider other)
    {
        // Solo daña a enemigos
        if (other.CompareTag("Enemy"))
        {
            GameObject enemigo = other.gameObject;

            // Evitar múltiples daños al mismo enemigo
            if (!enemigosImpactados.Contains(enemigo))
            {
                enemigosImpactados.Add(enemigo);

                EnemyHealth enemyHealth = enemigo.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    // Escalar el daño según la mejora del jugador
                    float dañoFinal = damage;

                    if (GameManager.Instance != null && GameManager.Instance.playerHealth != null)
                    {
                        dañoFinal *= GameManager.Instance.playerHealth.multiplicadorDaño;
                    }

                    enemyHealth.RecibirDaño((int)dañoFinal);
                }

                pierceCount++;

                // Si alcanzó el límite de enemigos atravesados, destruir la bala
                if (pierceCount >= maxPierceCount)
                {
                    Destroy(gameObject);
                }
            }
        }
        else if (!other.isTrigger)
        {
            // Si choca con una pared u obstáculo (no trigger), destruir la bala
            Destroy(gameObject);
        }
    }
}
