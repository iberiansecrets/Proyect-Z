using UnityEngine;
using System.Collections;

public class DecoySpawner : MonoBehaviour
{
    public GameObject decoyPrefab;

    [Header("Área de aparición")]
    public Vector3 areaCenter = Vector3.zero;
    public Vector3 areaSize = new Vector3(20f, 0f, 20f);

    [Header("Parámetros")]
    public float respawnDelay = 20f;
    public float checkRadius = 1f;   // radio para evitar obstáculos
    public int maxIntentos = 20;     // intentos máximos para buscar una posición válida

    private GameObject currentPickup;

    private void Start()
    {
        SpawnPickup();
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            if (currentPickup == null)
                SpawnPickup();

            yield return new WaitForSeconds(respawnDelay);
        }
    }

    private void SpawnPickup()
    {
        if (decoyPrefab == null)
        {
            Debug.LogWarning("No hay prefab asignado al spawner.");
            return;
        }

        // Buscar posición válida
        if (TryGetSpawnPosition(out Vector3 spawnPos))
        {
            currentPickup = Instantiate(decoyPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("No se encontró un sitio válido para generar el señuelo.");
        }
    }

    // Busca una posición libre de obstáculos. Devuelve TRUE si encuentra una posición válida.
    private bool TryGetSpawnPosition(out Vector3 position)
    {
        for (int i = 0; i < maxIntentos; i++)
        {
            Vector3 randomPos = areaCenter + new Vector3(
                Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
                0f,
                Random.Range(-areaSize.z / 2f, areaSize.z / 2f)
            );

            // Comprobar colisiones solo con obstáculos
            Collider[] colisiones = Physics.OverlapSphere(randomPos, checkRadius);

            bool valido = true;

            foreach (Collider col in colisiones)
            {
                if (col.CompareTag("Obstacle"))
                {
                    valido = false;
                    break;
                }
            }

            if (valido)
            {
                position = randomPos;
                return true;
            }
        }

        position = Vector3.zero;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 0, 1, 0.25f);
        Gizmos.DrawCube(areaCenter, areaSize);
    }
}
