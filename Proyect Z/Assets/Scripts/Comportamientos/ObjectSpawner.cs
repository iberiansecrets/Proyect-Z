using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObjectSpawner : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerHealth playerHealth;
    public GameManager gameManager;

    [Header("Prefabs de objetos")]
    public GameObject medkitPrefab;
    public GameObject shotgunPrefab;
    public GameObject riflePrefab;
    public GameObject sniperPrefab;

    [Header("Área de aparición")]
    public Vector3 areaCenter = Vector3.zero;
    public Vector3 areaSize = new Vector3(30f, 0f, 30f);

    [Header("Parámetros")]
    public int umbralZombies = 12;       // Límite de zombies para "presión alta"
    public int maxObjetosActivos = 3;    // Límite total de objetos en el mapa

    [HideInInspector] public bool vidaGenerada = false;
    [HideInInspector] public bool armaGenerada = false;

    private readonly List<GameObject> objetosActivos = new();

    public bool VidaJugadorBaja()
    {
        if (playerHealth == null) return false;
        float ratio = playerHealth.GetVidaActual() / playerHealth.GetVidaMaxima();
        return ratio < 0.85f;
    }

    public bool MuchosZombies()
    {
        if (gameManager == null) return false;
        return gameManager.GetEnemigosRestantes() >= umbralZombies;
    }

    public bool PuedeSpawnear() => objetosActivos.Count < maxObjetosActivos;

    public void SpawnBotiquin()
    {
        if (medkitPrefab == null || !PuedeSpawnear()) return;

        Vector3 randomPos = areaCenter + new Vector3(
            Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
            0f,
            Random.Range(-areaSize.z / 2f, areaSize.z / 2f)
        );

        GameObject nuevo = Instantiate(medkitPrefab, randomPos, Quaternion.identity);
        objetosActivos.Add(nuevo);
        vidaGenerada = true;
        Debug.Log($"[ObjectSpawner] ha generado: {medkitPrefab}");

        // Elimina referencia cuando el objeto desaparece
        StartCoroutine(RemoveWhenDestroyed(nuevo));
    }

    public void SpawnEscopeta()
    {
        if (shotgunPrefab == null || !PuedeSpawnear()) return;

        Vector3 randomPos = areaCenter + new Vector3(
            Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
            0f,
            Random.Range(-areaSize.z / 2f, areaSize.z / 2f)
        );

        GameObject nuevo = Instantiate(shotgunPrefab, randomPos, Quaternion.identity);
        objetosActivos.Add(nuevo);
        armaGenerada = true;
        Debug.Log($"[ObjectSpawner] ha generado: {shotgunPrefab}");

        // Elimina referencia cuando el objeto desaparece
        StartCoroutine(RemoveWhenDestroyed(nuevo));
    }

    public void SpawnFusil()
    {
        if (riflePrefab == null || !PuedeSpawnear()) return;

        Vector3 randomPos = areaCenter + new Vector3(
            Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
            0f,
            Random.Range(-areaSize.z / 2f, areaSize.z / 2f)
        );

        GameObject nuevo = Instantiate(riflePrefab, randomPos, Quaternion.identity);
        objetosActivos.Add(nuevo);
        armaGenerada = true;
        Debug.Log($"[ObjectSpawner] ha generado: {riflePrefab}");

        // Elimina referencia cuando el objeto desaparece
        StartCoroutine(RemoveWhenDestroyed(nuevo));
    }

    public void SpawnFrancotirador()
    {
        if (sniperPrefab == null || !PuedeSpawnear()) return;

        Vector3 randomPos = areaCenter + new Vector3(
            Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
            0f,
            Random.Range(-areaSize.z / 2f, areaSize.z / 2f)
        );

        GameObject nuevo = Instantiate(sniperPrefab, randomPos, Quaternion.identity);
        objetosActivos.Add(nuevo);
        armaGenerada = true;
        Debug.Log($"[ObjectSpawner] ha generado: {sniperPrefab}");

        // Elimina referencia cuando el objeto desaparece
        StartCoroutine(RemoveWhenDestroyed(nuevo));
    }

    private IEnumerator RemoveWhenDestroyed(GameObject obj)
    {
        yield return new WaitUntil(() => obj == null);
        objetosActivos.Remove(obj);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.25f);
        Gizmos.DrawCube(areaCenter, areaSize);
    }

    public int numeroRandom()
    {
        int numero = Random.Range(0, 1);
        return numero;
    }

    public bool GetVidaGenerada()
    {
        return !vidaGenerada;
    }

    public bool GetArmaGenerada()
    {
        return !armaGenerada;
    }
}
