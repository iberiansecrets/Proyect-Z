using UnityEngine;
using System.Collections.Generic;

public class PlayerOdorTrail : MonoBehaviour
{
    public static PlayerOdorTrail Instance; // Singleton para que el Colosal lo encuentre rápido

    public int maxPuntosRastro = 10;
    public float tiempoEntrePosicion = 1f;

    // Lista pública de las posiciones del rastro
    public List<Vector3> rastroPosiciones = new List<Vector3>();
    private float timer = 0f;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= tiempoEntrePosicion)
        {
            timer = 0f;
            AnadirPosicion();
        }
    }

    private void AnadirPosicion()
    {
        rastroPosiciones.Add(transform.position);

        // Si el rastro es muy largo, el olor antiguo se desvanece
        if (rastroPosiciones.Count > maxPuntosRastro)
        {
            rastroPosiciones.RemoveAt(0); // Borra el punto más viejo
        }
    }

    // Dibujamos el rastro en el editor para que veáis si funciona
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        for (int i = 0; i < rastroPosiciones.Count; i++)
        {
            Gizmos.DrawWireSphere(rastroPosiciones[i], 0.3f);
            if (i > 0) Gizmos.DrawLine(rastroPosiciones[i - 1], rastroPosiciones[i]);
        }
    }
}