using UnityEngine;
using System.Collections;

public class ShotgunSpawner : MonoBehaviour
{
    public GameObject shotgunPrefab;
    public Vector3 areaCenter = Vector3.zero;  // centro del área de spawn
    public Vector3 areaSize = new Vector3(20f, 0f, 20f); // ancho-largo del área
    public float respawnDelay = 20f;

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

        // Calcula una posición aleatoria dentro del área
        Vector3 randomPos = areaCenter + new Vector3(
            Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
            0f,
            Random.Range(-areaSize.z / 2f, areaSize.z / 2f)
        );

        // Instancia el objeto
        currentPickup = Instantiate(shotgunPrefab, randomPos, Quaternion.identity);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 0, 1, 0.25f);
        Gizmos.DrawCube(areaCenter, areaSize);
    }
}