using UnityEngine;
using System.Collections;

public class ShotgunSpawner : MonoBehaviour
{
    public GameObject shotgunPrefab;

    [Header("Área de aparición")]
    public Vector3 areaCenter = Vector3.zero;
    public Vector3 areaSize = new Vector3(20f, 0f, 20f);

    [Header("Parámetros")]
    public float respawnDelay = 20f;
    public float checkRadius = 1.5f; // radio para detectar obstáculos
    public int maxIntentos = 20;

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
        if (shotgunPrefab == null)
        {
            Debug.LogWarning("No hay prefab asignado al spawner.");
            return;
        }

        // Buscar posición válida
        if (TryGetSpawnPosition(out Vector3 spawnPos))
        {
            currentPickup = Instantiate(shotgunPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("No se encontró un sitio válido para generar el botiquín.");
        }
    }


    // Busca una posición libre de obstáculos. Devuelve TRUE si encuentra una posición válida.
    public bool TryGetSpawnPosition(out Vector3 position)
    {
        for (int i = 0; i < maxIntentos; i++)
        {
            Vector3 randomPos = areaCenter + new Vector3(
                Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
                0f,
                Random.Range(-areaSize.z / 2f, areaSize.z / 2f)
            );

            // Comprobar colisiones con obstáculos
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
        Gizmos.color = new Color(0, 1, 0, 0.25f);
        Gizmos.DrawCube(areaCenter, areaSize);
    }
}
