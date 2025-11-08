using UnityEngine;
using System.Collections;

public class SniperSpawner : MonoBehaviour
{
    public GameObject sniperPrefab;
    public Vector3 areaCenter = Vector3.zero;
    public Vector3 areaSize = new Vector3(20f, 0f, 20f);
    public float respawnDelay = 25f;

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
        if (sniperPrefab == null)
        {
            Debug.LogWarning("No hay prefab asignado al spawner del francotirador.");
            return;
        }

        Vector3 randomPos = areaCenter + new Vector3(
            Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
            0f,
            Random.Range(-areaSize.z / 2f, areaSize.z / 2f)
        );

        currentPickup = Instantiate(sniperPrefab, randomPos, Quaternion.identity);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.25f);
        Gizmos.DrawCube(areaCenter, areaSize);
    }
}
